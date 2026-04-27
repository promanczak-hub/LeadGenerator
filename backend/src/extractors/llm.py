from pydantic import BaseModel, Field
from typing import Optional
from google import genai
from src.core.config import config


class CompanyExtraction(BaseModel):
    company_name: str = Field(
        description=(
            "Pelna, oficjalna nazwa firmy WYKONAWCY / ZLECENIOBIORCY - "
            "czyli firmy, ktora REALIZUJE prace, WYGRALA przetarg, "
            "PODPISALA umowe lub DOSTARCZA produkt/usluge. "
            "NIE podawaj zamawiajacego, inwestora, gminy ani urzedu. "
            "Przyklad: 'Budimex S.A.', 'Strabag Sp. z o.o.', "
            "'Solaris Bus & Coach'. "
            "Jesli w tekscie NIE MA wykonawcy - zwroc pusty string."
        )
    )
    nip: Optional[str] = Field(
        default=None,
        description=(
            "Numer NIP firmy wykonawcy (10 cyfr, bez myslnikow). "
            "Wyciagnij z tekstu jesli jest dostepny. "
            "Jesli brak - zwroc null."
        ),
    )
    sanitized_title: str = Field(
        description=(
            "Krotki, profesjonalny opis CZEGO DOTYCZY zlecenie/przetarg. "
            "Przyklad: 'Budowa terminalu pasazerskiego w Modlinie', "
            "'Dostawa 50 autobusow elektrycznych', "
            "'Modernizacja drogi S7 odcinek Warszawa-Gdansk'. "
            "NIE uzywaj nawiasow kwadratowych ani tagów typu [PRZETARG]."
        )
    )
    summary: str = Field(
        description=(
            "Jedno zdanie opisujace co firma wykonawcy robi "
            "w kontekscie tego zlecenia. Opieraj sie TYLKO na tekscie."
        )
    )
    ai_score: int = Field(
        description=(
            "Ocena wartosci leada od 1 do 10. "
            "10 = duza firma budowlana/infrastrukturalna z konkretnym zleceniem. "
            "1 = news bez wartosci biznesowej, brak wykonawcy."
        )
    )


class ExtractionResult(BaseModel):
    companies: list[CompanyExtraction] = Field(
        description=(
            "Lista firm WYKONAWCOW znalezionych w tekscie. "
            "Kazdy element to osobna firma, ktora realizuje prace. "
            "Jesli w tekscie nie ma zadnego wykonawcy - zwroc PUSTA liste []."
        )
    )


class RawEntity(BaseModel):
    name: str = Field(
        description="Pełna nazwa podmiotu z tekstu (firmy, gminy, urzędu, inwestora)."
    )
    role: str = Field(
        description="Rola podmiotu w tekście (np. wykonawca, inwestor, zamawiający, organ dotujący)."
    )
    nip: Optional[str] = Field(default=None)
    context_sentence: str = Field(
        description="Zdanie z tekstu potwierdzające rolę podmiotu."
    )


class ExtractorResponse(BaseModel):
    entities: list[RawEntity] = Field(
        description="Lista wszystkich istotnych podmiotów wspomnianych w artykule (zarówno inwestorów jak i wykonawców)."
    )


class FilterDecision(BaseModel):
    entity_name: str
    is_b2b_lead: bool = Field(
        description="TRUE jeśli podmiot to firma komercyjna, która ZARABIA / OTRZYMUJE zlecenie / ZOSTAŁA WYBRANA. FALSE jeśli to instytucja publiczna, zamawiający, skarb państwa, gmina, inwestor płacący."
    )
    reasoning: str = Field(description="Krótkie uzasadnienie decyzji.")
    sanitized_title: str = Field(
        description="Tytuł zlecenia (tylko w przypadku is_b2b_lead=True). Inaczej puste."
    )
    summary: str = Field(
        description="Krótkie podsumowanie zlecenia (tylko w przypadku is_b2b_lead=True). Inaczej puste."
    )
    ai_score: int = Field(
        default=0, description="Ocena 1-10 (tylko w przypadku is_b2b_lead=True)."
    )


class FilterResponse(BaseModel):
    decisions: list[FilterDecision]


EXTRACTOR_PROMPT = """Jesteś analitykiem biznesowym. Twoim zadaniem jest znalezienie WSZYSTKICH podmiotów (firm, miast, urzędów, spółek) w artykule prasowym i zidentyfikowanie ich roli (np. Inwestor, Wykonawca, Podwykonawca, Zamawiający, Gmina). Zwróć wszystkie zidentyfikowane podmioty wraz z ich NIP i rolą."""

FILTER_PROMPT = """Otrzymujesz listę podmiotów i ról wyciągniętych z artykułu prasowego.
Twoim zadaniem jest zdecydować o Kwalifikacji Leada (is_b2b_lead).

KRYTERIA (is_b2b_lead=True):
- Podmiot to firma, która Wygrała przetarg, Została zatrudniona, Otrzyma zlecenie, Buduje obiekt. ZARABIA.

KRYTERIA ODRZUCENIA (is_b2b_lead=False):
- Gminy, Urzędy, Powiaty, Ministerstwa, GDDKiA, PKP, szpitale (ZAMAWIAJĄCY/KLIENCI).
- Inwestorzy, którzy PŁACĄ za inwestycję.

Jeśli is_b2b_lead=True, wypełnij `sanitized_title`, `summary` (1 zdanie) i `ai_score` (1-10)."""


async def extract_companies(text: str, raw_title: str = "") -> list[CompanyExtraction]:
    """Extract contractor/executor companies from article text using 2-stage AI process."""
    if not text or len(text.strip()) < 50:
        return []

    # --- STAGE 1: EXTRACT ALL ENTITIES ---
    ext_prompt = (
        f"Zidentyfikuj podmioty.\n\nTytuł: {raw_title}\n\nTekst:\n{text[:8000]}"
    )
    raw_entities = []

    if config.GEMINI_API_KEY:
        try:
            client = genai.Client(api_key=config.GEMINI_API_KEY)
            resp_ext = client.models.generate_content(
                model=config.GEMINI_MODEL_ID,
                contents=ext_prompt,
                config=genai.types.GenerateContentConfig(
                    response_mime_type="application/json",
                    response_schema=ExtractorResponse,
                    temperature=0.0,
                    system_instruction=EXTRACTOR_PROMPT,
                ),
            )
            if resp_ext.parsed and isinstance(resp_ext.parsed, ExtractorResponse):
                raw_entities = resp_ext.parsed.entities
        except Exception as e:
            print(f"Error w Etapie 1 (Gemini): {e}")

    if not raw_entities:
        return []

    # --- STAGE 2: FILTER & VERIFY ---
    entities_description = "\n".join(
        [
            f"- {e.name} (NIP: {e.nip}) | Rola: {e.role} | Kontekst: {e.context_sentence}"
            for e in raw_entities
        ]
    )

    from datetime import datetime
    current_date = datetime.now().strftime("%Y-%m-%d")
    
    filter_prompt = f"""Obecna data to {current_date}. Na podstawie podanej listy podmiotów oraz tytułu, przefiltruj je na B2B Leady. Odpowiedz listą decyzji.
UWAGA: Jeśli tekst wyraźnie opisuje wydarzenia, które miały miejsce w latach 2018-2025 (stare wiadomości), kategorycznie odrzuć podmiot (is_b2b_lead=False) z uzasadnieniem: "Zbyt stary artykuł".

Tytuł: {raw_title}
Podmioty:
{entities_description}"""

    decisions = []
    if config.GEMINI_API_KEY:
        try:
            client = genai.Client(api_key=config.GEMINI_API_KEY)
            resp_filter = client.models.generate_content(
                model=config.GEMINI_MODEL_ID,
                contents=filter_prompt,
                config=genai.types.GenerateContentConfig(
                    response_mime_type="application/json",
                    response_schema=FilterResponse,
                    temperature=0.0,
                    system_instruction=FILTER_PROMPT,
                ),
            )
            if resp_filter.parsed and isinstance(resp_filter.parsed, FilterResponse):
                decisions = resp_filter.parsed.decisions
        except Exception as e:
            print(f"Error w Etapie 2 (Gemini): {e}")

    # --- MAP TO OUTPUT ---
    final_companies = []

    for decision in decisions:
        if decision.is_b2b_lead:
            # find original entity to get nip if any
            nip = None
            for entity in raw_entities:
                if entity.name == decision.entity_name:
                    nip = entity.nip
                    break

            final_companies.append(
                CompanyExtraction(
                    company_name=decision.entity_name,
                    nip=nip,
                    sanitized_title=decision.sanitized_title or raw_title,
                    summary=decision.summary or decision.reasoning,
                    ai_score=decision.ai_score or 7,
                )
            )

    return final_companies

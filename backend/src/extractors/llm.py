from pydantic import BaseModel, Field
from typing import Optional
from google import genai
import json
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


SYSTEM_PROMPT = """Jestes ekspertem ds. generowania leadow B2B dla firmy handlowej.
Twoje zadanie: z artykulu prasowego lub opisu przetargu WYCIAGNIJ NAZWY FIRM WYKONAWCOW.

WYKONAWCA to firma, ktora:
- WYGRALA przetarg lub konkurs ofert
- PODPISALA umowe na realizacje
- DOSTARCZA produkt lub usluge (np. autobusy, sprzet, materialy)
- BUDUJE, MODERNIZUJE, REALIZUJE projekt

NIE INTERESUJA NAS:
- Zamawiajacy (gminy, urzedy, ministerstwa, PKP, GDDKiA - to KLIENCI, nie leady)
- Artykuly newsowe bez konkretnych firm wykonawczych
- Spekulacje, prognozy, plany bez podanej firmy realizujacej

ZASADY:
1. Jesli artykul mowi 'Budimex podpisal umowe na budowe terminalu' -> company_name='Budimex S.A.'
2. Jesli artykul mowi 'Miasto planuje budowe drogi' (bez wykonawcy) -> zwroc PUSTA liste
3. Zawsze podaj PELNA nazwe firmy z forma prawna (S.A., Sp. z o.o.) jesli znana
4. Jesli znajdziesz NIP w tekscie - wyciagnij go
5. ABSOLUTNIE NIE ZWRACAJ WARTOSCI typu 'Inwestor', 'Przyszły', 'Odrzucony', 'Kryptowaluty', 'Znana'. Jesli nie ma prawdziwej firmy uzywajacej formy prawnej lub jawnej nazwy komercyjnej wykonawcy, ZWROC PUSTA LISTE."""


async def extract_companies(
    text: str, raw_title: str = ""
) -> list[CompanyExtraction]:
    """Extract contractor/executor companies from article text using Gemini 2.5 Flash."""
    if not text or len(text.strip()) < 50:
        return []

    prompt = f"""ANALIZUJ PONIZSZY TEKST. Wyciagnij WSZYSTKIE firmy WYKONAWCOW (zleceniobiorcow).

Tytul artykulu: {raw_title}

Tresc:
{text[:8000]}

Zwroc ustrukturyzowany JSON zgodny ze schematem: {ExtractionResult.model_json_schema()}
Jesli nie ma zadnej firmy wykonawcy - zwroc {{"companies": []}}"""

    # --- GEMINI 2.5 FLASH (Primary) ---
    if config.GEMINI_API_KEY:
        try:
            client = genai.Client(api_key=config.GEMINI_API_KEY)
            response = client.models.generate_content(
                model=config.GEMINI_MODEL_ID,
                contents=prompt,
                config=genai.types.GenerateContentConfig(
                    response_mime_type="application/json",
                    response_schema=ExtractionResult,
                    temperature=0.1,
                    system_instruction=SYSTEM_PROMPT,
                ),
            )
            if response.parsed and isinstance(response.parsed, ExtractionResult):
                # Filter out empty company names
                valid = [
                    c for c in response.parsed.companies
                    if c.company_name and c.company_name.strip()
                ]
                return valid
        except Exception as e:
            print(f"Blad Gemini 2.5 Flash: {e}. Fallback to Anthropic...")

    # --- ANTHROPIC (Fallback) ---
    if config.ANTHROPIC_API_KEY:
        try:
            from anthropic import AsyncAnthropic

            anthropic_client = AsyncAnthropic(
                api_key=config.ANTHROPIC_API_KEY
            )
            response = await anthropic_client.messages.create(
                model="claude-3-5-sonnet-20240620",
                max_tokens=2000,
                system=SYSTEM_PROMPT,
                messages=[{"role": "user", "content": prompt}],
            )
            content = response.content[0].text
            json_start = content.find("{")
            json_end = content.rfind("}") + 1
            parsed = json.loads(content[json_start:json_end])
            results = [
                CompanyExtraction(**c)
                for c in parsed.get("companies", [])
            ]
            return [r for r in results if r.company_name.strip()]
        except Exception as e:
            print(f"Blad Anthropic (Fallback): {e}")

    return []

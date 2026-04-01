from pydantic import BaseModel, Field
from typing import Optional
from google import genai
from src.core.config import config


class EnrichmentResult(BaseModel):
    contact_email: Optional[str] = Field(
        description="Zaleziony adres e-mail (np. biuro@.., kontakt@..). Jeśli brak, zwróć null.",
        default=None,
    )
    contact_phone: Optional[str] = Field(
        description="Znaleziony numer telefonu firmy. Jeśli brak, zwróć null.",
        default=None,
    )
    nip: Optional[str] = Field(
        description="Numer NIP firmy, jeśli udało się znaleźć. Zwróć jako string bez myślników.",
        default=None,
    )
    industry: Optional[str] = Field(
        description="Zidentyfikowana krótka nazwa branży (np. 'Budownictwo', 'IT').",
        default=None,
    )


class LinkedinResult(BaseModel):
    ceo_name: Optional[str] = Field(
        description="Imię i nazwisko osoby decyzyjnej np. Prezes, Właściciel, Dyrektor. Jeśli nie masz pewności, zwróć null.",
        default=None,
    )
    linkedin_url: Optional[str] = Field(
        description="Link do profilu LinkedIn znalezionej osoby decyzyjnej. Jeśli nie ma, zwróć null.",
        default=None,
    )


async def extract_contact_info(text: str) -> Optional[EnrichmentResult]:
    """Wyciąga dane kontaktowe ze stront HTML klienta."""
    if not text or not config.GEMINI_API_KEY:
        return None

    client = genai.Client(api_key=config.GEMINI_API_KEY)

    prompt = f"""
    Z poniższego tekstu (który pochodzi ze strony WWW firmy) wyciągnij dane kontaktowe.
    Interesuje nas:
    - Główny adres e-mail (najlepiej ogólny lub do działu inwestycji)
    - Główny numer telefonu
    - Numer NIP (często pojawia się w stopce)
    - Krótka informacja o branży
    
    Zwróć TYLKO ustrukturyzowany JSON. Jeśli czegoś nie ma, użyj null.
    
    TEKST:
    {text[:5000]} # Ograniczenie wielkosci
    """

    try:
        response = await client.models.generate_content(
            model=config.GEMINI_MODEL,
            contents=prompt,
            config={
                "response_mime_type": "application/json",
                "response_schema": EnrichmentResult,
            },
        )
        parsed = response.parsed
        if isinstance(parsed, EnrichmentResult):
            return parsed
        return None
    except Exception as e:
        print(f"Błąd Gemini Enrichment: {e}")
        return None


async def extract_linkedin_info(search_results: str) -> Optional[LinkedinResult]:
    """Przetwarza wyniki wyszukiwania DuckDuckGo i znajduje link do konkretnej osoby."""
    if not search_results or not config.GEMINI_API_KEY:
        return None

    client = genai.Client(api_key=config.GEMINI_API_KEY)

    prompt = f"""
    Przeanalizuj poniższe wyniki wyszukiwania z Google/DuckDuckGo.
    Twoim celem jest zidentyfikowanie GŁÓWNEJ OSOBY DECYZYJNEJ (Prezes, CEO, Dyrektor Generalny, Właściciel) 
    dla wyszukiwanej firmy i znalezienie jej profilu LinkedIn w podanych linkach.
    
    WYNIKI WYSZUKIWANIA:
    {search_results}
    """

    try:
        response = await client.models.generate_content(
            model=config.GEMINI_MODEL,
            contents=prompt,
            config={
                "response_mime_type": "application/json",
                "response_schema": LinkedinResult,
            },
        )
        parsed = response.parsed
        if isinstance(parsed, LinkedinResult):
            return parsed
        return None
    except Exception as e:
        print(f"Błąd Gemini LinkedIn: {e}")
        return None

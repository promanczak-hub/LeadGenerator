from pydantic import BaseModel, Field
from typing import Optional
from google import genai
import anthropic
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
    if not text:
        return None

    prompt = f"""
    Z poniższego tekstu (który pochodzi ze strony WWW firmy) wyciągnij dane kontaktowe.
    Interesuje nas:
    - Główny adres e-mail (najlepiej ogólny lub do działu inwestycji)
    - Główny numer telefonu
    - Numer NIP (często pojawia się w stopce)
    - Krótka informacja o branży
    
    Zwróć TYLKO ustrukturyzowany JSON pasujący do schematu: {EnrichmentResult.model_json_schema()}. 
    Jeśli czegoś nie ma, użyj null.
    
    TEKST:
    {text[:8000]}
    """

    # --- ANTHROPIC PROVIDER ---
    if config.LLM_PROVIDER == "anthropic" and config.ANTHROPIC_API_KEY:
        try:
            client = anthropic.AsyncAnthropic(api_key=config.ANTHROPIC_API_KEY)
            response = await client.messages.create(
                model="claude-3-5-sonnet-20240620",
                max_tokens=1000,
                messages=[{"role": "user", "content": prompt}]
            )
            # Parse JSON from response
            import json
            content = response.content[0].text
            json_start = content.find('{')
            json_end = content.rfind('}') + 1
            return EnrichmentResult.model_validate_json(content[json_start:json_end])
        except Exception as e:
            print(f"Błąd Anthropic (Contact): {e}. Fallback to Gemini...")

    # --- GEMINI PROVIDER (Primary / Fallback) ---
    if config.GEMINI_API_KEY:
        client = genai.Client(api_key=config.GEMINI_API_KEY)
        try:
            response = await client.models.generate_content(
                model=config.GEMINI_MODEL_ID,
                contents=prompt,
                config={
                    "response_mime_type": "application/json",
                    "response_schema": EnrichmentResult,
                },
            )
            return response.parsed
        except Exception as e:
            print(f"Błąd Gemini (Contact): {e}")
    
    return None


async def extract_linkedin_info(search_results: str) -> Optional[LinkedinResult]:
    """Przetwarza wyniki wyszukiwania DuckDuckGo i znajduje link do konkretnej osoby."""
    if not search_results:
        return None

    prompt = f"""
    Przeanalizuj poniższe wyniki wyszukiwania z DuckDuckGo.
    Twoim celem jest zidentyfikowanie GŁÓWNEJ OSOBY DECYZYJNEJ (Prezes, CEO, Dyrektor Generalny, Właściciel) 
    dla wyszukiwanej firmy i znalezienie jej profilu LinkedIn w podanych linkach.
    
    Zwróć TYLKO ustrukturyzowany JSON pasujący do schematu: {LinkedinResult.model_json_schema()}
    
    WYNIKI WYSZUKIWANIA:
    {search_results}
    """

    # --- ANTHROPIC PROVIDER ---
    if config.LLM_PROVIDER == "anthropic" and config.ANTHROPIC_API_KEY:
        try:
            client = anthropic.AsyncAnthropic(api_key=config.ANTHROPIC_API_KEY)
            response = await client.messages.create(
                model="claude-3-5-sonnet-20240620",
                max_tokens=1000,
                messages=[{"role": "user", "content": prompt}]
            )
            import json
            content = response.content[0].text
            json_start = content.find('{')
            json_end = content.rfind('}') + 1
            return LinkedinResult.model_validate_json(content[json_start:json_end])
        except Exception as e:
            print(f"Błąd Anthropic (LinkedIn): {e}. Fallback to Gemini...")

    # --- GEMINI PROVIDER ---
    if config.GEMINI_API_KEY:
        client = genai.Client(api_key=config.GEMINI_API_KEY)
        try:
            response = await client.models.generate_content(
                model=config.GEMINI_MODEL_ID,
                contents=prompt,
                config={
                    "response_mime_type": "application/json",
                    "response_schema": LinkedinResult,
                },
            )
            return response.parsed
        except Exception as e:
            print(f"Błąd Gemini (LinkedIn): {e}")

    return None

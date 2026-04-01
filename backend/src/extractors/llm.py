from pydantic import BaseModel, Field
from typing import List
from google import genai
from src.core.config import config


class CompanyExtraction(BaseModel):
    company_name: str = Field(
        description="The formal, official name of the company extracted from the text. Must be a valid business entity. Do not extract regular nouns or article subjects."
    )
    summary: str = Field(
        description="A concise, 1-sentence comment describing what the company does, based ONLY on the provided text. Focus on their profession, e.g., 'Firma budowlana realizująca duże obiekty przemysłowe.'"
    )
    ai_score: int = Field(
        description="Score from 1 to 10 predicting the value of this lead. HIGHEST score (10) ONLY for companies performing physical work in INFRASTRUCTURE (budowa, drogi, mosty, kolej). Lower scores for IT, consulting, or suppliers."
    )


class ExtractionResult(BaseModel):
    companies: List[CompanyExtraction] = Field(
        description="List of companies found in the text that perform work, win tenders, or implement projects."
    )


if not config.GEMINI_API_KEY:
    raise ValueError("GEMINI_API_KEY must be set in the environment.")

client = genai.Client(api_key=config.GEMINI_API_KEY)


async def extract_companies(text: str) -> List[CompanyExtraction]:
    """
    Extracts company names, 1-sentence summaries, and score from a given text.
    """
    if not text or len(text.strip()) < 50:
        return []

    try:
        response = await client.models.generate_content(
            model=config.GEMINI_MODEL,
            contents=text,
            config=genai.types.GenerateContentConfig(
                response_mime_type="application/json",
                response_schema=ExtractionResult,
                temperature=0.1,
                system_instruction="""Jesteś ekspertem ds. analizy rynku i danych B2B w branży budowlanej i infrastrukturalnej. Twoim ZADANIEM jest ekstrakcja RZECZYWISTYCH FIRM (wykonawców) z podanego tekstu.

Rygorystyczne zasady filtracji:
1. Zwracaj TYLKO rzeczywiste podmioty gospodarcze (np. Budimex, Skanska, Strabag, firmy prywatne, spółki).
2. BEZWZGLĘDNIE ODRZUĆ: instytucje publiczne, urzędy (GDDKiA, PKP PLK, Urząd Miasta), programy publiczne oraz ogólne słowa (Wykonawca, Inwestor).
3. Każda nazwa musi być konkretną nazwą własną firmy. Jeśli nie masz pewności lub nazwa jest zbyt ogólna - odrzuć.

Zasady punktacji (ai_score 1-10):
- 10: Generalni wykonawcy dużych projektów INFRASTRUKTURALNYCH (autostrady, mosty, kolej, energetyka).
- 8-9: Firmy budowlane realizujące obiekty przemysłowe, magazynowe lub duże osiedla.
- 6-7: Specjalistyczni podwykonawcy (instalacje, fundamenty, konstrukcje stalowe) oraz producenci materiałów budowlanych.
- 4-5: Firmy IT, consultingowe, dostawcy biurowi lub projektanci.
- 1-3: Pozostałe branże luźno związane z zapytaniem.

Ocena MUSI być zróżnicowana. Jeśli firma jest tylko podwykonawcą lub dostawcą, nie dawaj jej 8-10.
Dla każdej znalezionej firmy napisz 1-zdaniowy, profesjonalny komentarz opisujący profil jej działalności na podstawie tekstu.""",
            ),
        )

        if isinstance(response.parsed, ExtractionResult):
            return response.parsed.companies
        else:
            print("Failed to parse the structured output as ExtractionResult.")
            return []

    except Exception as e:
        print(f"Error calling Gemini API: {e}")
        return []

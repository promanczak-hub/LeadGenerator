import os
import httpx
from pydantic import BaseModel, Field
from typing import Optional
from google import genai
from src.core.config import config

SERPAPI_KEY = os.getenv("SERPAPI_API_KEY")


class CompanyMetadata(BaseModel):
    website: Optional[str] = Field(None, description="Official company website URL")
    linkedin_url: Optional[str] = Field(
        None, description="Official company LinkedIn page URL"
    )
    reasoning: str = Field(
        ..., description="Short explanation of why these links were chosen"
    )


OSINT_PROMPT = """Analizujesz wyniki wyszukiwania Google dla firmy. 
Twoim zadaniem jest zidentyfikowanie:
1. Oficjalnej strony internetowej firmy (szukaj domen firmowych, unikaj portali typu panoramafirm, gowork, itp.).
2. Oficjalnego profilu firmy na LinkedIn.

Zwróć dane w formacie JSON."""


async def enrich_company_metadata(company_name: str) -> CompanyMetadata:
    """Enrich company data by searching Google via SerpApi and identifying links via LLM."""
    if not SERPAPI_KEY:
        return CompanyMetadata(
            website=None, linkedin_url=None, reasoning="SerpApi key missing"
        )

    query = f"{company_name} oficjalna strona internetowa linkedin"
    url = "https://serpapi.com/search"
    params = {
        "engine": "google",
        "q": query,
        "gl": "pl",
        "hl": "pl",
        "api_key": SERPAPI_KEY,
        "num": 5,  # Top 5 results are usually enough
    }

    try:
        async with httpx.AsyncClient(timeout=15.0) as client:
            response = await client.get(url, params=params)
            response.raise_for_status()
            data = response.json()

        organic_results = data.get("organic_results", [])
        if not organic_results:
            return CompanyMetadata(
                website=None, linkedin_url=None, reasoning="No search results found"
            )

        # Prepare snippets for LLM
        snippets = "\n".join(
            [
                f"- {res.get('title')} | URL: {res.get('link')} | Snippet: {res.get('snippet')}"
                for res in organic_results
            ]
        )

        prompt = f"Firma: {company_name}\n\nWyniki wyszukiwania:\n{snippets}"

        client_ai = genai.Client(api_key=config.GEMINI_API_KEY)
        resp = client_ai.models.generate_content(
            model=config.GEMINI_MODEL_ID,
            contents=prompt,
            config=genai.types.GenerateContentConfig(
                response_mime_type="application/json",
                response_schema=CompanyMetadata,
                temperature=0.0,
                system_instruction=OSINT_PROMPT,
            ),
        )

        if resp.parsed and isinstance(resp.parsed, CompanyMetadata):
            return resp.parsed

        return CompanyMetadata(
            website=None, linkedin_url=None, reasoning="Failed to parse LLM response"
        )

    except Exception as e:
        print(f"Error in OSINT enrichment for {company_name}: {e}")
        return CompanyMetadata(website=None, linkedin_url=None, reasoning=f"Error: {e}")

import pytest
import asyncio
from src.extractors.enrichment import validate_nip, enrich_company_data
from src.extractors.llm import extract_companies

def test_validate_nip():
    # Valid NIP (example from internet)
    assert validate_nip("774-00-01-454") == "7740001454"
    assert validate_nip("7740001454") == "7740001454"
    
    # Invalid NIP
    assert validate_nip("1234567890") is None
    assert validate_nip("ABC") is None
    assert validate_nip("") is None

@pytest.mark.asyncio
async def test_extract_companies_smoke():
    # Simple smoke test to ensure it doesn't crash and returns a list
    text = "Firma Budimex S.A. wygrała przetarg na budowę drogi ekspresowej S7."
    results = await extract_companies(text)
    assert isinstance(results, list)
    if results:
        assert results[0].company_name.lower().find("budimex") != -1

@pytest.mark.asyncio
async def test_enrich_company_data_smoke():
    # Smoke test for enrichment
    # Note: This makes real network calls if not mocked, but used here for verification
    linkedin, contact = await enrich_company_data("Budimex")
    assert linkedin is None or hasattr(linkedin, "linkedin_url")
    assert contact is None or hasattr(contact, "contact_email")

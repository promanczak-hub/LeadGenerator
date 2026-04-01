from unittest.mock import patch
from src.core.supabase import LeadInsert
from src.extractors.llm import CompanyExtraction


def test_lead_insert_model():
    # Boundary data checks
    lead = LeadInsert(
        source="Test Source",
        company_name="Test Company",
        tender_title="Test Tender",
        ai_score=10,
        ai_summary="Great firm",
        full_content="Long text here",
    )
    assert lead.company_name == "Test Company"
    assert lead.ai_score == 10
    assert lead.status == "new"


def test_company_extraction_model():
    # Empty/boundary tests
    ce = CompanyExtraction(
        company_name="Budimex", summary="Firma budowlana.", ai_score=10
    )
    assert ce.company_name == "Budimex"
    assert ce.ai_score == 10


@patch("src.extractors.llm.client")
def test_extract_companies_mocked(mock_client):
    # Setup mock to avoid hitting real Gemini API in CI
    from src.extractors.llm import extract_companies, ExtractionResult
    from unittest.mock import MagicMock

    # Mocking the parsed object in the response
    mock_parsed = ExtractionResult(
        companies=[
            CompanyExtraction(
                company_name="Mock Corp", summary="IT Solutions", ai_score=5
            )
        ]
    )

    mock_response = MagicMock()
    mock_response.parsed = mock_parsed

    mock_client.models.generate_content.return_value = mock_response

    res = extract_companies(
        "Some longer text about Mock Corp providing IT Solutions to ensure we have over 50 chars."
    )
    assert len(res) == 1
    assert res[0].company_name == "Mock Corp"
    assert res[0].ai_score == 5


def test_extract_companies_empty():
    from src.extractors.llm import extract_companies

    res = extract_companies("Too short")
    assert res == []

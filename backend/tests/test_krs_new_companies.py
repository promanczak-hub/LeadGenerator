import pytest
from unittest.mock import patch, MagicMock, AsyncMock
from src.scrapers.krs_new_companies import run_krs_scraper


@pytest.mark.asyncio
@patch("src.scrapers.krs_new_companies.config")
async def test_run_krs_scraper_no_api_key(mock_config, capsys):
    mock_config.REJESTR_IO_KEY = ""
    await run_krs_scraper()

    captured = capsys.readouterr()
    assert "Brak poprawnego klucza" in captured.out


@pytest.mark.asyncio
@patch("src.scrapers.krs_new_companies.config")
@patch("src.scrapers.krs_new_companies.httpx.Client.get")
@patch("src.scrapers.krs_new_companies.enrich_company_data")
@patch("src.scrapers.krs_new_companies.insert_lead", new_callable=AsyncMock)
async def test_run_krs_scraper_success(
    mock_insert_lead, mock_enrich, mock_get, mock_config, capsys
):
    mock_config.REJESTR_IO_KEY = "valid_key"
    mock_config.KRS_INDUSTRIES = ["41.20.Z"]

    # Mocking API response
    mock_response = MagicMock()
    mock_response.status_code = 200
    mock_response.json.return_value = {
        "items": [
            {
                "name": "Test Company Budownictwo Sp. z o.o.",
                "krs": "0000123456",
                "nip": "1234567890",
            }
        ]
    }
    mock_get.return_value = mock_response

    # Mocking enrichment
    mock_linkedin = MagicMock()
    mock_linkedin.ceo_name = "Jan Kowalski"
    mock_linkedin.linkedin_url = "https://linkedin.com/in/jan"
    mock_contact = MagicMock()
    mock_contact.contact_email = "biuro@test.pl"
    mock_contact.contact_phone = "123456789"
    mock_contact.industry = "Budownictwo"
    mock_contact.nip = "1234567890"

    mock_enrich.return_value = (mock_linkedin, mock_contact)
    mock_insert_lead.return_value = True

    await run_krs_scraper()

    captured = capsys.readouterr()
    assert "Dodano 1 nowych leadów" in captured.out

    # Verify insert_lead was called with correct data
    assert mock_insert_lead.call_count == 1
    lead_call_args = mock_insert_lead.call_args[0][0]
    assert lead_call_args.company_name == "Test Company Budownictwo Sp. z o.o."
    assert lead_call_args.source == "KRS/Nowe Spółki"
    assert "Jan Kowalski" in lead_call_args.ai_summary


@pytest.mark.asyncio
@patch("src.scrapers.krs_new_companies.config")
@patch("src.scrapers.krs_new_companies.httpx.Client.get")
async def test_run_krs_scraper_api_error(mock_get, mock_config, capsys):
    mock_config.REJESTR_IO_KEY = "valid_key"
    mock_config.KRS_INDUSTRIES = ["41.20.Z"]

    # Mocking API 401 response
    mock_response = MagicMock()
    mock_response.status_code = 401
    mock_get.return_value = mock_response

    await run_krs_scraper()

    captured = capsys.readouterr()
    assert "Nieprawidłowy klucz API Rejestr.io" in captured.out

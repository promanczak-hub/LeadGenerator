"""Unit tests for the Baza Konkurencyjności EU projects scraper."""

from __future__ import annotations

from unittest.mock import MagicMock, patch

from src.scrapers.baza_konkurencyjnosci import (
    _extract_chosen_offer_id,
    _find_company_by_offer_id,
    _build_lead,
)


# ---------------------------------------------------------------------------
# _extract_chosen_offer_id
# ---------------------------------------------------------------------------


def test_extract_chosen_offer_id_happy_path() -> None:
    settlement = {
        "order_nodes": [
            {
                "chosen_offer_variant": {"id": 1855},
                "no_offer_selected": False,
            }
        ]
    }
    assert _extract_chosen_offer_id(settlement) == 1855


def test_extract_chosen_offer_id_empty_nodes() -> None:
    assert _extract_chosen_offer_id({"order_nodes": []}) is None


def test_extract_chosen_offer_id_no_chosen() -> None:
    settlement = {
        "order_nodes": [{"chosen_offer_variant": None, "no_offer_selected": True}]
    }
    assert _extract_chosen_offer_id(settlement) is None


def test_extract_chosen_offer_id_missing_key() -> None:
    assert _extract_chosen_offer_id({}) is None


# ---------------------------------------------------------------------------
# _find_company_by_offer_id
# ---------------------------------------------------------------------------


def test_find_company_by_offer_id_found() -> None:
    offersets = [
        {
            "offers": [{"variants": [{"id": 1855}]}],
            "economic_subject": {"name": "Strabag Sp. z o.o."},
        }
    ]
    result = _find_company_by_offer_id(offersets, 1855)
    assert result == "Strabag Sp. z o.o."


def test_find_company_by_offer_id_not_found() -> None:
    offersets = [
        {
            "offers": [{"variants": [{"id": 999}]}],
            "economic_subject": {"name": "Some Company"},
        }
    ]
    assert _find_company_by_offer_id(offersets, 1855) is None


def test_find_company_by_offer_id_empty_list() -> None:
    assert _find_company_by_offer_id([], 1855) is None


def test_find_company_by_offer_id_empty_name() -> None:
    offersets = [
        {"offers": [{"variants": [{"id": 1855}]}], "economic_subject": {"name": ""}}
    ]
    assert _find_company_by_offer_id(offersets, 1855) is None


# ---------------------------------------------------------------------------
# _build_lead
# ---------------------------------------------------------------------------


@patch("src.scrapers.baza_konkurencyjnosci.enrich_company_data")
def test_build_lead_creates_valid_lead(mock_enrich: MagicMock) -> None:
    mock_enrich.return_value = (None, None)
    announcement = {"id": 42, "title": "Dostawa maszyn budowlanych"}
    lead = _build_lead("Komatsu Poland SA", announcement)

    assert lead is not None
    assert lead.company_name == "Komatsu Poland SA"
    assert lead.source == "Baza Konkurencyjności"
    assert lead.url is not None and "42" in lead.url


@patch("src.scrapers.baza_konkurencyjnosci.enrich_company_data")
def test_build_lead_skips_empty_name(mock_enrich: MagicMock) -> None:
    mock_enrich.return_value = (None, None)
    announcement = {"id": 1, "title": "Test"}
    assert _build_lead("", announcement) is None


@patch("src.scrapers.baza_konkurencyjnosci.enrich_company_data")
def test_build_lead_with_contact_info(mock_enrich: MagicMock) -> None:
    from src.extractors.enricher_llm import EnrichmentResult

    contact = EnrichmentResult(
        contact_email="kontakt@firma.pl",
        contact_phone="123456789",
        nip="1234567890",
        industry="Budownictwo",
    )
    mock_enrich.return_value = (None, contact)

    announcement = {"id": 99, "title": "Roboty ziemne"}
    lead = _build_lead("Skanska SA", announcement)

    assert lead is not None
    assert lead.contact_email == "kontakt@firma.pl"
    assert lead.nip == "1234567890"
    assert lead.industry == "Budownictwo"

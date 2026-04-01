"""Unit tests for the GUNB building permits scraper."""

from __future__ import annotations

from datetime import datetime, timedelta
from unittest.mock import MagicMock, patch

from src.scrapers.gunb import (
    _is_recent,
    _matches_keywords,
    _build_lead,
)


# ---------------------------------------------------------------------------
# _is_recent
# ---------------------------------------------------------------------------


def test_is_recent_returns_true_for_yesterday() -> None:
    yesterday = (datetime.now() - timedelta(days=1)).strftime("%Y-%m-%d")
    row = {"data_wydania_decyzji": yesterday}
    assert _is_recent(row, days=7) is True


def test_is_recent_returns_false_for_old_date() -> None:
    old_date = (datetime.now() - timedelta(days=30)).strftime("%Y-%m-%d")
    row = {"data_wydania_decyzji": old_date}
    assert _is_recent(row, days=7) is False


def test_is_recent_handles_empty_date() -> None:
    assert _is_recent({"data_wydania_decyzji": ""}, days=7) is False


def test_is_recent_handles_dot_format() -> None:
    yesterday = (datetime.now() - timedelta(days=1)).strftime("%d.%m.%Y")
    row = {"data_wydania_decyzji": yesterday}
    assert _is_recent(row, days=7) is True


# ---------------------------------------------------------------------------
# _matches_keywords
# ---------------------------------------------------------------------------


def test_matches_keywords_hits() -> None:
    row = {"nazwa_zam_budowlanego": "Budowa hali magazynowej przy ul. Przemysłowej"}
    assert _matches_keywords(row) is True


def test_matches_keywords_misses() -> None:
    row = {"nazwa_zam_budowlanego": "Remont mieszkania – zmiana lokalu użytkowego"}
    assert _matches_keywords(row) is False


def test_matches_keywords_empty() -> None:
    assert _matches_keywords({"nazwa_zam_budowlanego": ""}) is False


# ---------------------------------------------------------------------------
# _build_lead
# ---------------------------------------------------------------------------


@patch("src.scrapers.gunb.enrich_company_data")
@patch("src.scrapers.gunb.insert_lead")
def test_build_lead_valid_row(mock_insert: MagicMock, mock_enrich: MagicMock) -> None:
    mock_enrich.return_value = (None, None)

    row = {
        "nazwa_inwestor": "Budimex SA",
        "nazwa_zam_budowlanego": "Budowa hali produkcyjnej",
        "miasto": "Warszawa",
        "wojewodztwo": "Mazowieckie",
    }
    lead = _build_lead(row)

    assert lead is not None
    assert lead.company_name == "Budimex SA"
    assert lead.source == "GUNB Pozwolenia"
    assert lead.industry == "Budownictwo"


@patch("src.scrapers.gunb.enrich_company_data")
def test_build_lead_skips_empty_investor(mock_enrich: MagicMock) -> None:
    mock_enrich.return_value = (None, None)
    row = {"nazwa_inwestor": "", "nazwa_zam_budowlanego": "Budowa drogi"}
    assert _build_lead(row) is None

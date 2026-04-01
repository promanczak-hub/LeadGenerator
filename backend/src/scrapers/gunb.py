"""
GUNB (Główny Urząd Nadzoru Budowlanego) Scraper.

Downloads publicly available ZIP/CSV archives of building permits
(pozwolenia na budowę) and filters for high-value infrastructure
investments. Archives are updated nightly at:
https://wyszukiwarka.gunb.gov.pl/pliki_pobranie/wynik_{province}.zip
"""

import csv
import io
import zipfile
from datetime import datetime, timedelta

import httpx

from src.core.config import config
from src.core.supabase import LeadInsert, insert_lead
from src.extractors.enrichment import enrich_company_data

csv.field_size_limit(
    10 * 1024 * 1024
)  # 10 MB – handles large GUNB CSV fields on Windows

BASE_URL = "https://wyszukiwarka.gunb.gov.pl/pliki_pobranie/wynik_{province}.zip"

# CSV column names as published by GUNB (delimiter = '#')
CSV_DELIMITER = "#"
COL_INVESTOR = "nazwa_inwestor"
COL_DESCRIPTION = "nazwa_zam_budowlanego"
COL_DECISION_DATE = "data_wydania_decyzji"
COL_CITY = "miasto"
COL_VOIVODESHIP = "wojewodztwo"


def _download_csv_rows(province: str) -> list[dict[str, str]]:
    """Download and unzip the CSV for a given province, returning all rows."""
    url = BASE_URL.format(province=province)
    try:
        response = httpx.get(url, timeout=30.0, follow_redirects=True)
        response.raise_for_status()
    except Exception as exc:
        print(f"[GUNB] Error downloading {province}: {exc}")
        return []

    try:
        with zipfile.ZipFile(io.BytesIO(response.content)) as zf:
            csv_name = next((n for n in zf.namelist() if n.endswith(".csv")), None)
            if not csv_name:
                print(f"[GUNB] No CSV found in archive for {province}")
                return []
            with zf.open(csv_name) as csv_file:
                text = csv_file.read().decode("utf-8-sig", errors="replace")
                reader = csv.DictReader(io.StringIO(text), delimiter=CSV_DELIMITER)
                return list(reader)
    except Exception as exc:
        print(f"[GUNB] Error parsing ZIP for {province}: {exc}")
        return []


def _is_recent(row: dict[str, str], days: int) -> bool:
    """Return True if the decision date is within the last `days` days."""
    raw = (row.get(COL_DECISION_DATE) or "").strip()
    if not raw:
        return False
    # Date may include time: '2024-03-15 00:00:00'
    raw = raw.split(" ")[0]  # Take date portion only
    for fmt in ("%Y-%m-%d", "%d.%m.%Y", "%d-%m-%Y"):
        try:
            decision_date = datetime.strptime(raw, fmt)
            cutoff = datetime.now() - timedelta(days=days)
            return decision_date >= cutoff
        except ValueError:
            continue
    return False


def _matches_keywords(row: dict[str, str]) -> bool:
    """Return True if the investment description contains any target keyword."""
    description = (row.get(COL_DESCRIPTION) or "").lower()
    return any(kw.lower() in description for kw in config.GUNB_KEYWORDS)


def _build_lead(row: dict[str, str]) -> LeadInsert | None:
    """Create a LeadInsert from one CSV row after enrichment."""
    investor = row.get(COL_INVESTOR, "").strip()
    description = row.get(COL_DESCRIPTION, "").strip()
    location = (
        f"{row.get(COL_CITY, '').strip()}, {row.get(COL_VOIVODESHIP, '').strip()}"
    )

    if not investor or len(investor) < 3:
        return None

    print(f"  [GUNB] Enriching: {investor}")
    linkedin_info, contact_info = await enrich_company_data(investor)

    ai_summary_parts = [
        f"Pozwolenie na budowę: {description[:200]}",
        f"Lokalizacja: {location}",
    ]
    if linkedin_info and linkedin_info.ceo_name:
        ai_summary_parts.append(f"Osoba decyzyjna: {linkedin_info.ceo_name}")

    return LeadInsert(
        source="GUNB Pozwolenia",
        company_name=investor,
        tender_title=description[:255] if description else "Pozwolenie na budowę",
        url="https://wyszukiwarka.gunb.gov.pl/",
        ai_score=9,
        ai_summary=" | ".join(ai_summary_parts),
        contact_email=contact_info.contact_email if contact_info else None,
        contact_phone=contact_info.contact_phone if contact_info else None,
        nip=contact_info.nip if contact_info else None,
        industry="Budownictwo",
        full_content=f"Inwestor: {investor}\nOpis: {description}\nLokalizacja: {location}",
    )


async def run_gunb_scraper() -> None:
    """Main entry point: iterate provinces, filter recent + relevant rows."""
    print("=== Starting GUNB Building Permits Scraper ===")
    total_saved = 0
    max_per_province = config.GUNB_MAX_LEADS_PER_PROVINCE

    for province in config.GUNB_PROVINCES:
        province = province.strip()
        if not province:
            continue
        print(f"\n[GUNB] Processing province: {province}")

        rows = _download_csv_rows(province)
        print(f"  Downloaded {len(rows)} total rows.")

        matching = [
            r
            for r in rows
            if _is_recent(r, config.GUNB_DAYS_BACK) and _matches_keywords(r)
        ]
        print(f"  Matching (recent + keywords): {len(matching)} rows.")

        for row in matching[:max_per_province]:
            lead = _build_lead(row)
            if lead and await insert_lead(lead):
                print(f"  ✅ Saved: {lead.company_name}")
                total_saved += 1
            elif lead:
                print(f"  ❌ Failed to save or duplicate: {lead.company_name}")

    print(f"\n=== GUNB Finished. Total leads saved: {total_saved} ===")


if __name__ == "__main__":
    import asyncio

    asyncio.run(run_gunb_scraper())

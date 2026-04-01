"""
Baza Konkurencyjności (EU funds competition database) Scraper.

Uses the internal REST API discovered via browser network analysis.
Flow:
  1. Fetch page N of RESOLVED announcements.
  2. For each announcement: fetch its settlement to get the chosen offer ID.
  3. Fetch the offerset list to find the winning company details.
  4. Enrich and save as a lead.

Base URL: https://bazakonkurencyjnosci.funduszeeuropejskie.gov.pl/
"""

from __future__ import annotations

from curl_cffi import requests

from src.core.config import config
from src.core.supabase import LeadInsert, insert_lead
from src.extractors.enrichment import enrich_company_data

API_BASE = "https://bazakonkurencyjnosci.funduszeeuropejskie.gov.pl/api"
PAGE_LIMIT = 20

_HEADERS = {
    "Accept": "application/json, text/plain, */*",
    "Referer": "https://bazakonkurencyjnosci.funduszeeuropejskie.gov.pl/",
}


def _get_session() -> requests.Session:
    return requests.Session(impersonate="chrome120")


def _fetch_resolved_announcements(session: requests.Session, page: int) -> list[dict]:
    """Return page of resolved announcements from the BK search API."""
    url = f"{API_BASE}/announcements/search"
    params = {
        "status[]": "RESOLVED",
        "page": page,
        "limit": PAGE_LIMIT,
        "sort": "publicationDate",
        "sortDirection": "DESC",
    }
    try:
        resp = session.get(url, params=params, headers=_HEADERS, timeout=15.0)
        resp.raise_for_status()
        data = resp.json()
        # BK API uses 'advertisements' key (not 'announcements')
        return data.get("data", {}).get("advertisements", [])
    except Exception as exc:
        print(f"[BK] Error fetching page {page}: {exc}")
        return []


def _fetch_winner_company(
    session: requests.Session, announcement_id: int
) -> str | None:
    """
    Two-step fetch: settlement → chosen offer ID, then offerset list → company name.
    Returns the winning company name or None.
    """
    settlement_url = f"{API_BASE}/announcements/{announcement_id}/settlement"
    try:
        resp = session.get(settlement_url, headers=_HEADERS, timeout=10.0)
        resp.raise_for_status()
        settlement = resp.json().get("data", {}).get("settlement", {})
    except Exception as exc:
        print(f"  [BK] Settlement fetch error for {announcement_id}: {exc}")
        return None

    chosen_id = _extract_chosen_offer_id(settlement)
    if chosen_id is None:
        return None

    offerset_url = f"{API_BASE}/offerset/announcement/{announcement_id}/list"
    try:
        resp = session.get(offerset_url, headers=_HEADERS, timeout=10.0)
        resp.raise_for_status()
        offersets = resp.json().get("data", {}).get("offersets", [])
    except Exception as exc:
        print(f"  [BK] Offerset fetch error for {announcement_id}: {exc}")
        return None

    return _find_company_by_offer_id(offersets, chosen_id)


def _extract_chosen_offer_id(settlement: dict) -> int | None:
    """Dig into settlement JSON to find the chosen offer variant ID."""
    for node in settlement.get("order_nodes", []):
        chosen = node.get("chosen_offer_variant")
        if chosen and chosen.get("id"):
            return int(chosen["id"])
    return None


def _find_company_by_offer_id(
    offersets: list[dict], chosen_variant_id: int
) -> str | None:
    """
    Match the chosen offer VARIANT ID to an economic subject name.
    The chosen_offer_variant.id refers to offer.variants[].id, not offer.id.
    """
    for offerset in offersets:
        for offer in offerset.get("offers", []):
            variant_ids = [v.get("id") for v in offer.get("variants", [])]
            if chosen_variant_id in variant_ids:
                subject = offerset.get("economic_subject", {})
                return subject.get("name", "").strip() or None
    return None


def _build_lead(company_name: str, announcement: dict) -> LeadInsert | None:
    title = announcement.get("title", "Projekt UE – Baza Konkurencyjności")
    announcement_id = announcement.get("id", "")
    url = f"https://bazakonkurencyjnosci.funduszeeuropejskie.gov.pl/ogloszenia/{announcement_id}"

    if not company_name or len(company_name) < 3:
        return None

    print(f"  [BK] Enriching winner: {company_name}")
    linkedin_info, contact_info = await enrich_company_data(company_name)

    ai_summary_parts = [f"Wygrała postępowanie UE: {title[:200]}"]
    if linkedin_info and linkedin_info.ceo_name:
        ai_summary_parts.append(f"Osoba decyzyjna: {linkedin_info.ceo_name}")

    return LeadInsert(
        source="Baza Konkurencyjności",
        company_name=company_name,
        tender_title=title[:255],
        url=url,
        ai_score=8,
        ai_summary=" | ".join(ai_summary_parts),
        contact_email=contact_info.contact_email if contact_info else None,
        contact_phone=contact_info.contact_phone if contact_info else None,
        nip=contact_info.nip if contact_info else None,
        industry=contact_info.industry if contact_info else None,
        full_content=f"Firma: {company_name}\nOgłoszenie: {title}\nŹródło: {url}",
    )


async def run_bk_scraper() -> None:
    """Main entry point: fetch BK_PAGES pages of resolved announcements."""
    print("=== Starting Baza Konkurencyjności EU Projects Scraper ===")
    session = _get_session()
    total_saved = 0

    for page in range(1, config.BK_PAGES + 1):
        print(f"\n[BK] Fetching page {page}/{config.BK_PAGES}...")
        announcements = _fetch_resolved_announcements(session, page)

        if not announcements:
            print("  No announcements found, stopping.")
            break

        for ann in announcements:
            ann_id: int | None = ann.get("id")
            if ann_id is None:
                continue
            title = ann.get("title", "")[:60]
            print(f"  → Processing [{ann_id}]: {title}...")

            company_name = _fetch_winner_company(session, ann_id)
            if not company_name:
                print("    No winner found, skipping.")
                continue

            lead = _build_lead(company_name, ann)
            if lead and await insert_lead(lead):
                print(f"    ✅ Saved: {lead.company_name}")
                total_saved += 1
            elif lead:
                print(f"    ❌ Failed or duplicate: {lead.company_name}")

    print(f"\n=== BK Finished. Total leads saved: {total_saved} ===")


if __name__ == "__main__":
    import asyncio

    asyncio.run(run_bk_scraper())

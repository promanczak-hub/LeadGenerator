import os
import asyncio
import httpx
from typing import List

from src.core.profiles import get_profile
from src.core.supabase import LeadInsert, insert_lead, check_company_exists
from src.services.osint_service import enrich_company_metadata

SERPAPI_KEY = os.getenv("SERPAPI_API_KEY")


async def get_google_jobs_leads(query: str, limit: int = 5) -> List[LeadInsert]:
    """Fetch job listings from Google Jobs via SerpApi and convert them to Leads."""
    if not SERPAPI_KEY:
        print("SERPAPI_API_KEY missing.")
        return []

    url = "https://serpapi.com/search"
    params = {
        "engine": "google_jobs",
        "q": query,
        "gl": "pl",
        "hl": "pl",
        "api_key": SERPAPI_KEY,
    }

    try:
        async with httpx.AsyncClient(timeout=15.0) as client:
            response = await client.get(url, params=params)
            response.raise_for_status()
            data = response.json()

        jobs_results = data.get("jobs_results", [])
        leads = []
        for job in jobs_results[:limit]:
            company_name = job.get("company_name", "Unknown")
            job_title = job.get("title", "Unknown")
            location = job.get("location", "Polska")
            description = job.get("description", "")

            # Map to Lead model
            leads.append(
                LeadInsert(
                    source="JOBS - Google Search",
                    company_name=company_name,
                    tender_title=f"Rekrutacja: {job_title}",
                    job_title=job_title,
                    ai_summary=f"Firma rekrutuje na stanowisko {job_title} w lokalizacji {location}. To silny sygnał rozwoju/nowych projektów.",
                    full_content=description[:3000],
                    status="new",
                    ai_score=8,  # Jobs for specific roles are solid leads
                )
            )
        return leads
    except Exception as e:
        print(f"Error fetching Google Jobs for '{query}': {e}")
        return []


async def run_jobs_scraper(profile_name: str = "default"):
    """Main execution loop for Google Jobs scraper."""
    print(f"Starting Google Jobs Scraper - Profile: {profile_name.upper()}...")
    profile = get_profile(profile_name)
    keywords = profile.jobs_keywords

    for keyword in keywords:
        print(f"\nSearching Google Jobs for hiring signal: '{keyword}'")
        job_leads = await get_google_jobs_leads(keyword, limit=5)

        for lead in job_leads:
            if await check_company_exists(lead.company_name):
                print(
                    f"  -> Skipping duplicate: {lead.company_name} (found for keyword: {keyword})"
                )
                continue

            print(
                f"  Processing Hiring Lead: {lead.company_name} ({lead.job_title})..."
            )

            # Enrich with OSINT
            metadata = await enrich_company_metadata(lead.company_name)
            lead.website = metadata.website
            lead.linkedin_url = metadata.linkedin_url

            if metadata.website or metadata.linkedin_url:
                print(
                    f"    -> Enriched: WWW={lead.website}, LinkedIn={'Yes' if lead.linkedin_url else 'No'}"
                )

            # Save to Database
            success = await insert_lead(lead)
            if success:
                print(f"    -> Saved lead for {lead.company_name}")
            else:
                print(f"    -> Failed to save {lead.company_name} (duplicate?)")

    print("\n=== Google Jobs Scraper Completed ===")


if __name__ == "__main__":
    asyncio.run(run_jobs_scraper())

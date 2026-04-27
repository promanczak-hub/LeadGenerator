from curl_cffi import requests
from bs4 import BeautifulSoup
from src.core.supabase import LeadInsert, insert_lead, check_company_exists
from src.extractors.llm import extract_companies


def get_bzp_award_notices(limit: int = 5):
    """
    Fetches recent contract award notices from e-Zamówienia (Biuletyn Zamówień Publicznych).
    We use the public search HTML page.
    Optionally, we can filter by CPV or noticeType on the search page URL.
    """
    # Base search URL
    # https://ezamowienia.gov.pl/mp/bzp/search?NoticeType=WynikPostepowania
    url = "https://ezamowienia.gov.pl/mp/bzp/search"
    params = {"NoticeType": "WynikPostepowania"}

    try:
        session: requests.Session = requests.Session(impersonate="chrome120")
        response = session.get(url, params=params, timeout=15.0)
        response.raise_for_status()

        soup = BeautifulSoup(response.text, "html.parser")

        # Typically the results are in a list format
        items = soup.select(".list-container .list-item")

        results = []
        for item in items[:limit]:
            title_el = (
                item.select_one(".title")
                or item.select_one("h2")
                or item.select_one("a.details-link")
            )
            title = title_el.get_text(strip=True) if title_el else "Brak tytułu"

            link_el = item.select_one("a.details-link") or title_el
            link = ""
            if link_el and link_el.has_attr("href"):
                href_val = link_el["href"]
                href_str = href_val[0] if isinstance(href_val, list) else str(href_val)
                link = "https://ezamowienia.gov.pl" + href_str

            desc_el = item.select_one(".subtitle") or item.select_one(".description")
            desc = desc_el.get_text(strip=True) if desc_el else ""

            if link:
                results.append({"title": title, "link": link, "description": desc})
        return results
    except Exception as e:
        print(f"Error fetching BZP HTML search: {e}")
        return []


def scrape_bzp_notice(url: str) -> str:
    """Fetches the BZP notice form HTML"""
    try:
        session: requests.Session = requests.Session(impersonate="chrome120")
        response = session.get(url, timeout=15.0)
        response.raise_for_status()

        soup = BeautifulSoup(response.text, "html.parser")

        # Removing scripts and styles
        for element in soup(["script", "style", "nav", "footer"]):
            element.extract()

        return soup.get_text(separator=" ", strip=True)

    except Exception as e:
        print(f"Error fetching BZP notice details {url}: {e}")
        return ""


async def run_bzp_scraper(profile_name: str = "default"):
    print(
        f"Starting e-Zamówienia BZP Contract Awards Scraper - Profile: {profile_name.upper()}..."
    )

    notices = get_bzp_award_notices(limit=10)

    if not notices:
        print("No BZP notices found.")
        return

    for index, notice in enumerate(notices):
        print(
            f"[{index + 1}/{len(notices)}] Processing BZP Notice: {notice['title'][:60]}..."
        )

        text_content = scrape_bzp_notice(notice["objectId"])
        if len(text_content) < 100:
            print("  Notice content too short.")
            continue

        companies = await extract_companies(text_content)

        for company in companies:
            if await check_company_exists(company.company_name):
                print(f"  -> Skipping duplicate: {company.company_name}")
                continue

            print(f"  Found Winner: {company.company_name} - {company.summary}")

            lead = LeadInsert(
                source="BZP Wygrane",
                company_name=company.company_name,
                tender_title=notice["title"],
                url=notice["link"],
                ai_score=company.ai_score,
                ai_summary=company.summary,
                full_content=text_content[:1000],
            )
            await insert_lead(lead)


if __name__ == "__main__":
    import asyncio

    asyncio.run(run_bzp_scraper())

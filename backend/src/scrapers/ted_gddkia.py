from curl_cffi import requests
from bs4 import BeautifulSoup
from src.core.supabase import LeadInsert, insert_lead
from src.extractors.llm import extract_companies


def get_ted_award_notices(limit: int = 5):
    """
    Fetches recent contract award notices for Poland from TED via HTML scraping.
    """
    url = "https://ted.europa.eu/pl/search/result"

    # query for Contract Awards (ND=3) in Poland (CY=PL)
    params = {"q": "ND=[3] AND CY=[PL]", "sort": "PD DESC"}

    try:
        session: requests.Session = requests.Session(impersonate="chrome120")
        response = session.get(url, params=params, timeout=15.0)
        response.raise_for_status()

        soup = BeautifulSoup(response.text, "html.parser")
        notices = soup.select(".notice-item")

        results = []
        for item in notices[: limit * 3]:  # Fetch more for local filtering
            title_el = item.select_one(".notice-title a")
            desc_el = item.select_one(".notice-description")

            if not title_el:
                continue

            title = title_el.get_text(strip=True)
            href_val = title_el.get("href", "")
            href_str = href_val[0] if isinstance(href_val, list) else str(href_val)
            link = "https://ted.europa.eu" + href_str
            desc = desc_el.get_text(strip=True) if desc_el else ""

            # Simple content filtering - we look for GDDKiA or PKP in title/desc
            lower_text = (title + " " + desc).lower()
            if (
                "generalna dyrekcja dróg krajowych" in lower_text
                or "gddkia" in lower_text
                or "pkp" in lower_text
                or "kolej" in lower_text
            ):
                results.append({"title": title, "link": link, "description": desc})
        return results
    except Exception as e:
        print(f"Error fetching TED HTML search: {e}")
        return []


def scrape_ted_notice(url: str) -> str:
    """Fetches the notice HTML and extracts the main text content for LLM"""
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
        print(f"Error fetching TED notice {url}: {e}")
        return ""


async def run_ted_scraper():
    print("Starting TED Contract Awards Scraper (GDDKiA/PKP)...")

    # We fetch a larger batch because we filter locally
    notices = get_ted_award_notices(limit=50)

    if not notices:
        print("No relevant TED notices found.")
        return

    for index, notice in enumerate(notices):
        print(
            f"[{index + 1}/{len(notices)}] Processing TED Notice: {notice['title']}..."
        )

        text_content = scrape_ted_notice(notice["link"])
        if len(text_content) < 100:
            continue

        companies = extract_companies(text_content)

        for company in companies:
            print(f"  Found Winner: {company.company_name} - {company.summary}")

            lead = LeadInsert(
                source="TED GDDKiA Wygrane",
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

    asyncio.run(run_ted_scraper())

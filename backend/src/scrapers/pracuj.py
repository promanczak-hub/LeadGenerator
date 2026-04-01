from urllib.parse import quote
from curl_cffi import requests
from bs4 import BeautifulSoup
from src.core.config import config
from src.core.supabase import LeadInsert, insert_lead
from src.extractors.llm import extract_companies


async def scrape_pracuj(query: str, limit: int = 5):
    print(f"Starting Pracuj.pl Scraping for query: {query}")

    # curl_cffi impersonates a real browser TLS fingerprint
    session: requests.Session = requests.Session(impersonate="chrome120")

    try:
        url = f"https://www.pracuj.pl/praca/{quote(query)};kw"
        response = session.get(url, timeout=15.0)

        soup = BeautifulSoup(response.text, "html.parser")

        # Searching for offer links in the HTML
        offer_links = set()
        for a_tag in soup.find_all("a", href=True):
            href_attr = a_tag["href"]
            href = href_attr[0] if isinstance(href_attr, list) else str(href_attr)
            if href.startswith("https://www.pracuj.pl/praca/") and "oferta" in href:
                # Need to clean up search tracking params if they exist to avoid huge URLs
                clean_href = href.split("?")[0] if "?" in href else href
                offer_links.add(clean_href)

        links = list(offer_links)[:limit]
        print(f"Found {len(links)} offer links. Limiting to {limit}.")

        for index, link in enumerate(links):
            print(f"[{index + 1}/{len(links)}] Scraping: {link}")
            try:
                offer_res = session.get(link, timeout=15.0)
                offer_soup = BeautifulSoup(offer_res.text, "html.parser")

                # We extract the main container or body, avoiding script/style content
                for element in offer_soup(["script", "style", "nav", "footer"]):
                    element.extract()

                text_content = offer_soup.get_text(separator=" ", strip=True)
                title = (
                    str(offer_soup.title.string)
                    if offer_soup.title and offer_soup.title.string
                    else "Brak tytułu"
                )

                if (
                    len(text_content) > 100
                    and "Cloudflare" not in title
                    and "Just a moment" not in title
                ):
                    companies = extract_companies(text_content)
                    for company in companies:
                        print(
                            f"  Found Company: {company.company_name} - {company.summary}"
                        )

                        lead = LeadInsert(
                            source="pracuj.pl",
                            company_name=company.company_name,
                            tender_title=title,
                            url=str(link),
                            ai_score=company.ai_score,
                            ai_summary=company.summary,
                            full_content=text_content[
                                :1000
                            ],  # Limiting to 1000 chars for DB
                        )
                        await insert_lead(lead)
                else:
                    print(
                        f"  Warning: Skipped link. Content too short or Cloudflare block '{title}'"
                    )

            except Exception as ex:
                print(f"  Error processing offer {link}: {ex}")

    except Exception as e:
        print(f"Error scraping pracuj.pl for '{query}': {e}")


async def run_pracuj_scraper():
    keywords = config.PRACUJ_KEYWORDS
    for query in keywords:
        query = query.strip()
        if not query:
            continue
        await scrape_pracuj(query, limit=3)


if __name__ == "__main__":
    import asyncio

    asyncio.run(run_pracuj_scraper())

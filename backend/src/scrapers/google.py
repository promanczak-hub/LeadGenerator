import urllib.parse
from bs4 import BeautifulSoup
import httpx
from src.core.config import config
from src.core.supabase import LeadInsert, insert_lead
from src.extractors.llm import extract_companies


def get_google_news_links(query: str, limit: int = 5) -> list[dict]:
    url = f"https://news.google.com/rss/search?q={urllib.parse.quote(query)}&hl=pl&gl=PL&ceid=PL:pl"
    try:
        response = httpx.get(url, timeout=10.0)
        response.raise_for_status()
        soup = BeautifulSoup(response.content, features="xml")
        items = soup.find_all("item")

        results = []
        for item in items[:limit]:
            results.append(
                {
                    "title": item.title.text if item.title else "",
                    "link": item.link.text if item.link else "",
                    "pubDate": item.pubDate.text if item.pubDate else "",
                }
            )
        return results
    except Exception as e:
        print(f"Error fetching Google News for '{query}': {e}")
        return []


def fetch_article_text(url: str) -> str:
    try:
        # Use a standard user agent
        headers = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"}
        response = httpx.get(url, headers=headers, timeout=15.0, follow_redirects=True)
        response.raise_for_status()
        soup = BeautifulSoup(response.text, "html.parser")

        # Extract text from paragraphs
        paragraphs = soup.find_all("p")
        text = "\n".join(
            [
                p.get_text(strip=True)
                for p in paragraphs
                if len(p.get_text(strip=True)) > 20
            ]
        )
        return text
    except Exception as e:
        print(f"Error fetching article text from {url}: {e}")
        return ""


async def run_google_scraper():
    print("Starting Google Search Scraper...")
    keywords = config.GOOGLE_KEYWORDS

    for query in keywords:
        query = query.strip()
        if not query:
            continue

        print(f"Searching Google News for: {query}")
        articles = get_google_news_links(query, limit=5)

        for article in articles:
            print(f"Processing article: {article['title']}")
            text = fetch_article_text(article["link"])

            if len(text) < 100:
                print("Article too short or could not be parsed.")
                continue

            companies = extract_companies(text)
            for company in companies:
                print(f"Found Company: {company.company_name} - {company.summary}")

                # Check if it was already inserted or insert new
                lead = LeadInsert(
                    source="Google Search",
                    company_name=company.company_name,
                    tender_title=article["title"],
                    url=article["link"],
                    ai_score=company.ai_score,
                    ai_summary=company.summary,
                    full_content=text[:1000],  # Limiting to 1000 chars for DB
                )
                success = await insert_lead(lead)
                if success:
                    print(f"-> Saved {company.company_name} to Supabase")
                else:
                    print(
                        f"-> Failed to save {company.company_name} or likely duplicate"
                    )


if __name__ == "__main__":
    import asyncio

    asyncio.run(run_google_scraper())

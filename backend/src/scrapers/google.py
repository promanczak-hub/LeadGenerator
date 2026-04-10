"""Google News scraper with full article text extraction.

Fetches RSS from Google News, resolves redirect URLs to actual articles,
scrapes full article text, and extracts CONTRACTOR companies via Gemini 2.5 Flash.
"""
import urllib.parse
import asyncio

import httpx
from bs4 import BeautifulSoup

from src.core.config import config
from src.core.supabase import LeadInsert, insert_lead
from src.extractors.llm import extract_companies

# Timeout and limits
HTTP_TIMEOUT = 15.0
MAX_ARTICLE_CHARS = 5000
MIN_ARTICLE_LENGTH = 150


async def get_google_news_links(
    query: str, limit: int = 5
) -> list[dict]:
    """Fetch RSS feed from Google News and return article metadata."""
    url = (
        f"https://news.google.com/rss/search?"
        f"q={urllib.parse.quote(query)}&hl=pl&gl=PL&ceid=PL:pl"
    )
    try:
        async with httpx.AsyncClient(timeout=HTTP_TIMEOUT) as client:
            response = await client.get(url)
            response.raise_for_status()

        soup = BeautifulSoup(response.content, features="xml")
        items = soup.find_all("item")

        results = []
        for item in items[:limit]:
            title = item.title.text if item.title else ""
            link = item.link.text if item.link else ""
            pub_date = item.pubDate.text if item.pubDate else ""
            results.append(
                {"title": title, "link": link, "pubDate": pub_date}
            )
        return results
    except Exception as e:
        print(f"Error fetching Google News for '{query}': {e}")
        return []


async def resolve_google_redirect(google_url: str) -> str:
    """Decode a Google News RSS URL to the actual article URL.

    Google News RSS returns protobuf-encoded URLs like:
    https://news.google.com/rss/articles/CBMi...
    Uses the googlenewsdecoder library to decode them.
    """
    if not google_url or "news.google.com" not in google_url:
        return google_url

    try:
        from googlenewsdecoder import gnewsdecoder

        decoded = gnewsdecoder(google_url)
        if decoded.get("status") and decoded.get("decoded_url"):
            return decoded["decoded_url"]
        return google_url
    except Exception as e:
        print(f"  Could not decode Google News URL: {e}")
        return google_url


async def fetch_article_text(url: str) -> str:
    """Fetch full article text from a URL.

    Scrapes the page, extracts paragraph text, and returns
    clean text content suitable for AI analysis.
    """
    if not url:
        return ""

    try:
        headers = {
            "User-Agent": (
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                "AppleWebKit/537.36 (KHTML, like Gecko) "
                "Chrome/120.0.0.0 Safari/537.36"
            ),
            "Accept": "text/html,application/xhtml+xml",
            "Accept-Language": "pl-PL,pl;q=0.9,en;q=0.8",
        }
        async with httpx.AsyncClient(
            follow_redirects=True,
            timeout=HTTP_TIMEOUT,
        ) as client:
            response = await client.get(url, headers=headers)
            response.raise_for_status()

        soup = BeautifulSoup(response.text, "html.parser")

        # Remove noise elements
        for tag in soup(
            ["script", "style", "nav", "footer", "header", "aside", "form"]
        ):
            tag.extract()

        # Extract meaningful paragraphs
        paragraphs = soup.find_all("p")
        text_parts = []
        for p in paragraphs:
            txt = p.get_text(strip=True)
            if len(txt) > 30:
                text_parts.append(txt)

        article_text = "\n".join(text_parts)

        # If paragraphs are too short, try article/main tags
        if len(article_text) < MIN_ARTICLE_LENGTH:
            for container in soup.find_all(
                ["article", "main", "div"],
                class_=lambda c: c and any(
                    kw in str(c).lower()
                    for kw in ["article", "content", "body", "text"]
                ),
            ):
                fallback = container.get_text(separator="\n", strip=True)
                if len(fallback) > len(article_text):
                    article_text = fallback

        return article_text[:MAX_ARTICLE_CHARS]

    except Exception as e:
        print(f"  Error fetching article from {url[:60]}: {e}")
        return ""


async def run_google_scraper():
    """Main Google News scraper pipeline."""
    print("Starting Google News Scraper (Full Article Mode)...")
    keywords = config.GOOGLE_KEYWORDS

    for query in keywords:
        query = query.strip()
        if not query:
            continue

        print(f"\nSearching Google News for: '{query}'")
        articles = await get_google_news_links(query, limit=5)

        for article in articles:
            raw_title = article["title"]
            google_url = article["link"]
            print(f"  Processing: {raw_title[:60]}...")

            # Step 1: Resolve Google redirect to actual article URL
            real_url = await resolve_google_redirect(google_url)
            if real_url != google_url:
                print(f"  Resolved URL: {real_url[:60]}...")

            # Step 2: Fetch full article text
            text = await fetch_article_text(real_url)
            if len(text) < MIN_ARTICLE_LENGTH:
                print(
                    f"  Article too short ({len(text)} chars), skipping."
                )
                continue

            print(f"  Fetched {len(text)} chars of article text.")

            # Step 3: Extract contractors via Gemini 2.5 Flash
            companies = await extract_companies(
                text, raw_title=raw_title
            )

            if not companies:
                print("  No contractor found in article. Skipping.")
                continue

            for company in companies:
                print(
                    f"  CONTRACTOR: {company.company_name}"
                    f" | Score: {company.ai_score}"
                )

                lead = LeadInsert(
                    source="NEWS - Google Search",
                    company_name=company.company_name,
                    tender_title=company.sanitized_title,
                    url=real_url,
                    ai_score=company.ai_score,
                    ai_summary=company.summary,
                    full_content=text[:MAX_ARTICLE_CHARS],
                    nip=company.nip,
                    status="processed",
                )
                success = await insert_lead(lead)
                if success:
                    nip_str = (
                        f" (NIP: {company.nip})" if company.nip else ""
                    )
                    print(
                        f"  -> Saved: {company.company_name}"
                        f"{nip_str}"
                    )
                else:
                    print(
                        f"  -> Failed to save {company.company_name}"
                        " (duplicate?)"
                    )

    print("\n=== Google News Scraper Completed ===")


if __name__ == "__main__":
    asyncio.run(run_google_scraper())

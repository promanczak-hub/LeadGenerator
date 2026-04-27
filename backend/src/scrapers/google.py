"""Google News scraper with full article text extraction and API Fallback Strategy.

Uses Tavily -> Brave -> SerpApi -> DuckDuckGo -> Google RSS in a cascade.
Filters out strictly old articles using timestamp parsing.
"""

import urllib.parse
import asyncio
import os
from datetime import datetime, timezone
from email.utils import parsedate_to_datetime

import httpx
from bs4 import BeautifulSoup
from duckduckgo_search import DDGS
from tavily import TavilyClient

from src.core.profiles import get_profile
from src.core.supabase import LeadInsert, insert_lead, check_company_exists
from src.extractors.llm import extract_companies
from src.services.osint_service import enrich_company_metadata

HTTP_TIMEOUT = 15.0
MAX_ARTICLE_CHARS = 5000
MIN_ARTICLE_LENGTH = 150
MAX_NEWS_AGE_DAYS = 14

SERPAPI_KEY = os.getenv("SERPAPI_API_KEY")
TAVILY_KEY = os.getenv("TAVILY_API_KEY")
BRAVE_KEY = os.getenv("BRAVE_API_KEY")


def parse_news_date(date_str: str) -> datetime | None:
    if not date_str:
        return None
    try:
        # ISO formats
        d_str = date_str.replace("Z", "+00:00")
        return datetime.fromisoformat(d_str)
    except ValueError:
        pass
    try:
        # RFC 2822
        return parsedate_to_datetime(date_str)
    except Exception:
        pass
    return None


def is_too_old(pub_date: str, max_days: int = MAX_NEWS_AGE_DAYS) -> bool:
    dt = parse_news_date(pub_date)
    if not dt:
        return False
    now = datetime.now(timezone.utc)
    if dt.tzinfo is None:
        dt = dt.replace(tzinfo=timezone.utc)
    return (now - dt).days > max_days


async def get_tavily_news(query: str, limit: int = 5) -> list[dict]:
    if not TAVILY_KEY:
        raise ValueError("No TAVILY_API_KEY")
    client = TavilyClient(api_key=TAVILY_KEY)
    
    response = await asyncio.to_thread(
        client.search,
        query=query,
        search_depth="advanced",
        topic="news",
        days=MAX_NEWS_AGE_DAYS,
        max_results=limit
    )
    
    results = []
    for item in response.get("results", []):
        results.append({
            "title": item.get("title", ""),
            "link": item.get("url", ""),
            "pubDate": item.get("published_date", ""),
            "source_engine": "Tavily"
        })
    return results


async def get_brave_news(query: str, limit: int = 5) -> list[dict]:
    if not BRAVE_KEY:
        raise ValueError("No BRAVE_API_KEY")
    
    url = "https://api.search.brave.com/res/v1/news/search"
    headers = {
        "Accept": "application/json",
        "X-Subscription-Token": BRAVE_KEY
    }
    # past month is 'pm', past week 'pw'
    params = {"q": query, "freshness": "pm", "count": limit, "search_lang": "pl"}
    
    async with httpx.AsyncClient(timeout=HTTP_TIMEOUT) as client:
        response = await client.get(url, headers=headers, params=params)
        response.raise_for_status()
        data = response.json()
        
    results = []
    for item in data.get("results", []):
        results.append({
            "title": item.get("title", ""),
            "link": item.get("url", ""),
            "pubDate": item.get("age", ""), 
            "source_engine": "Brave"
        })
    return results


async def get_serpapi_news_links(query: str, limit: int = 5) -> list[dict]:
    if not SERPAPI_KEY:
        raise ValueError("No SERPAPI_API_KEY")

    url = "https://serpapi.com/search"
    params = {
        "engine": "google_news",
        "q": query,
        "gl": "pl",
        "hl": "pl",
        "tbs": "qdr:w,sbd:1", # past week + sort by date
        "api_key": SERPAPI_KEY,
    }

    async with httpx.AsyncClient(timeout=HTTP_TIMEOUT) as client:
        response = await client.get(url, params=params)
        response.raise_for_status()
        data = response.json()

    results = []
    for item in data.get("news_results", [])[:limit]:
        results.append({
            "title": item.get("title", ""),
            "link": item.get("link", ""),
            "pubDate": item.get("date", ""),
            "source_engine": "SerpApi"
        })
    return results


async def get_duckduckgo_news(query: str, limit: int = 5) -> list[dict]:
    def fetch():
        with DDGS() as ddgs:
            return list(ddgs.news(query, timelimit="m", max_results=limit))
            
    try:
        items = await asyncio.to_thread(fetch)
    except Exception as e:
        raise ValueError(f"DDG failed: {e}")

    results = []
    for item in items:
        results.append({
            "title": item.get("title", ""),
            "link": item.get("url", ""),
            "pubDate": item.get("date", ""),
            "source_engine": "DuckDuckGo"
        })
    return results


async def get_google_rss_news_links(query: str, limit: int = 5) -> list[dict]:
    url = (
        f"https://news.google.com/rss/search?"
        f"q={urllib.parse.quote(query + f' when:{MAX_NEWS_AGE_DAYS}d')}&hl=pl&gl=PL&ceid=PL:pl"
    )
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
        results.append({
            "title": title, 
            "link": link, 
            "pubDate": pub_date, 
            "source_engine": "Google RSS"
        })
    return results


async def resolve_google_redirect(google_url: str) -> str:
    if not google_url or "news.google.com" not in google_url:
        return google_url
    try:
        from googlenewsdecoder import gnewsdecoder  # type: ignore
        decoded = gnewsdecoder(google_url)
        if decoded.get("status") and decoded.get("decoded_url"):
            return decoded["decoded_url"]
        return google_url
    except Exception as e:
        return google_url


async def fetch_article_text(url: str) -> str:
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
        }
        async with httpx.AsyncClient(follow_redirects=True, timeout=HTTP_TIMEOUT) as client:
            response = await client.get(url, headers=headers)
            response.raise_for_status()

        soup = BeautifulSoup(response.text, "html.parser")
        for tag in soup(["script", "style", "nav", "footer", "header", "aside", "form"]):
            tag.extract()

        paragraphs = soup.find_all("p")
        text_parts = [p.get_text(strip=True) for p in paragraphs if len(p.get_text(strip=True)) > 30]
        article_text = "\n".join(text_parts)

        if len(article_text) < MIN_ARTICLE_LENGTH:
            for container in soup.find_all(["article", "main", "div"], class_=lambda c: c and any(kw in str(c).lower() for kw in ["article", "content", "body", "text"])):
                fallback = container.get_text(separator="\n", strip=True)
                if len(fallback) > len(article_text):
                    article_text = fallback

        return article_text[:MAX_ARTICLE_CHARS]
    except Exception as e:
        print(f"  Error fetching article from {url[:60]}: {e}")
        return ""


async def search_with_fallback(query: str, limit: int = 5) -> list[dict]:
    """Cascade API search: Tavily -> Brave -> SerpApi -> DuckDuckGo -> Google RSS"""
    print(f"  [Search] Attempting Tavily...")
    try:
        return await get_tavily_news(query, limit)
    except Exception as e:
        print(f"  -> Tavily failed/skipped: {e}")
        
    print(f"  [Search] Attempting Brave...")
    try:
        return await get_brave_news(query, limit)
    except Exception as e:
        print(f"  -> Brave failed/skipped: {e}")

    print(f"  [Search] Attempting SerpApi...")
    try:
        return await get_serpapi_news_links(query, limit)
    except Exception as e:
        print(f"  -> SerpApi failed/skipped: {e}")

    print(f"  [Search] Attempting DuckDuckGo...")
    try:
        return await get_duckduckgo_news(query, limit)
    except Exception as e:
        print(f"  -> DuckDuckGo failed/skipped: {e}")

    print(f"  [Search] Attempting Google RSS...")
    try:
        return await get_google_rss_news_links(query, limit)
    except Exception as e:
        print(f"  -> Google RSS failed: {e}")
        
    return []


async def run_google_scraper(profile_name: str = "default"):
    print(f"Starting News Scraper Cascade (Max Age: {MAX_NEWS_AGE_DAYS} days) - Profile: {profile_name.upper()}...")
    profile = get_profile(profile_name)
    keywords = profile.google_keywords + profile.chamber_keywords

    for query in keywords:
        query = query.strip()
        if not query:
            continue

        print(f"\nQuerying: '{query}'")
        articles = await search_with_fallback(query, limit=5)

        for article in articles:
            raw_title = article["title"]
            google_url = article["link"]
            pub_date = article["pubDate"]
            engine = article["source_engine"]

            print(f"  Processing [{engine}]: {raw_title[:60]}...")
            
            # 1. Date Check in Python
            if is_too_old(pub_date, max_days=MAX_NEWS_AGE_DAYS):
                print(f"  -> ODRZUCONO: Artykuł posiada historyczną datę publikacji ({pub_date}).")
                continue

            real_url = await resolve_google_redirect(google_url)
            
            # 2. Fetch full article text
            text_content = await fetch_article_text(real_url)
            if len(text_content) < MIN_ARTICLE_LENGTH:
                print(f"  -> Skipped: Article too short.")
                continue

            # 3. LLM Extraction
            companies = await extract_companies(text_content, raw_title=raw_title)
            if not companies:
                continue

            for company in companies:
                if await check_company_exists(company.company_name):
                    print(f"  -> Skipping duplicate: {company.company_name}")
                    continue

                print(f"  CONTRACTOR: {company.company_name} | Score: {company.ai_score}")

                lead = LeadInsert(
                    source=f"NEWS - {engine}",
                    company_name=company.company_name,
                    tender_title=company.sanitized_title,
                    url=real_url,
                    ai_score=company.ai_score,
                    ai_summary=company.summary,
                    full_content=text_content[:MAX_ARTICLE_CHARS],
                    nip=company.nip,
                    status="processed",
                )

                metadata = await enrich_company_metadata(company.company_name)
                lead.website = metadata.website
                lead.linkedin_url = metadata.linkedin_url

                success = await insert_lead(lead)
                if success:
                    print(f"  -> Saved: {company.company_name}")

    print("\n=== News Scraper Cascade Completed ===")


if __name__ == "__main__":
    asyncio.run(run_google_scraper())

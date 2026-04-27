import re
import aiohttp
from bs4 import BeautifulSoup
from duckduckgo_search import DDGS
from typing import Optional, Dict, Any
import logging

logger = logging.getLogger(__name__)


class EnrichmentService:
    def __init__(self):
        self.headers = {
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        }

    async def find_company_website(self, company_name: str) -> Optional[str]:
        """Search for the official company website URL."""
        try:
            with DDGS() as ddgs:
                # Limit to first few results to find the most probable official site
                results = ddgs.text(f"{company_name} oficjalna strona", max_results=3)
                for r in results:
                    url = r.get("href", "")
                    # Basic filters to avoid social media/directories if possible
                    if any(
                        x in url
                        for x in [
                            "facebook.com",
                            "linkedin.com",
                            "instagram.com",
                            "twitter.com",
                            "krs-pobierz.pl",
                            "rejestr.io",
                        ]
                    ):
                        continue
                    return url
            return None
        except Exception as e:
            logger.error(f"Error finding website for {company_name}: {e}")
            return None

    async def extract_contacts(self, url: str) -> Dict[str, Optional[str]]:
        """Scrape the given URL for email and phone numbers."""
        if not url:
            return {"email": None, "phone": None}

        try:
            async with aiohttp.ClientSession(headers=self.headers) as session:
                timeout = aiohttp.ClientTimeout(total=10)
                async with session.get(url, timeout=timeout) as response:
                    if response.status != 200:
                        return {"email": None, "phone": None}

                    html = await response.text()
                    soup = BeautifulSoup(html, "html.parser")
                    text = soup.get_text()

                    # Regex patterns
                    email_pattern = r"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"
                    phone_pattern = r"(?:\+48|0048)?[\s-]?\d{3}[\s-]?\d{3}[\s-]?\d{3}"

                    emails = re.findall(email_pattern, html)
                    phones = re.findall(phone_pattern, text)

                    return {
                        "email": emails[0].lower() if emails else None,
                        "phone": phones[0].replace(" ", "").replace("-", "")
                        if phones
                        else None,
                    }
        except Exception as e:
            logger.error(f"Error scraping {url}: {e}")
            return {"email": None, "phone": None}

    async def enrich_lead(self, company_name: str) -> Dict[str, Any]:
        """Main entry point to research a company."""
        url = await self.find_company_website(company_name)
        contacts = (
            await self.extract_contacts(url) if url else {"email": None, "phone": None}
        )

        return {
            "website": url,
            "email": contacts.get("email"),
            "phone": contacts.get("phone"),
        }


enrichment_service = EnrichmentService()

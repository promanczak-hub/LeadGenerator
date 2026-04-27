"""Pracuj.pl scraper – extracts the EMPLOYER (hiring company) from job listings.

For a Lead Generator, job postings are valuable because they reveal companies
that are actively hiring (growing/executing projects). The company_name
should be the EMPLOYER, not the job title.
"""

from urllib.parse import quote

from bs4 import BeautifulSoup
from curl_cffi import requests

from src.core.profiles import get_profile
from src.core.supabase import LeadInsert, insert_lead, check_company_exists


def extract_employer_from_html(soup: BeautifulSoup) -> str | None:
    """Extract the employer/company name from Pracuj.pl offer page.

    Pracuj.pl uses specific HTML structures to display the company name.
    We try multiple selectors to find it reliably.
    """
    # Method 1: Look for employer name in structured data
    for tag in soup.find_all("a", {"data-test": "text-employerName"}):
        name = tag.get_text(strip=True).replace("O firmie", "").strip()
        if name and len(name) > 2:
            return name

    # Method 2: Look for company name in header section
    for tag in soup.find_all("h2", {"data-test": "text-employerName"}):
        name = tag.get_text(strip=True).replace("O firmie", "").strip()
        if name and len(name) > 2:
            return name

    # Method 3: JSON-LD structured data
    for script in soup.find_all("script", type="application/ld+json"):
        import json

        try:
            data = json.loads(script.string or "")
            if isinstance(data, dict):
                org = data.get("hiringOrganization", {})
                if isinstance(org, dict):
                    name = org.get("name", "")
                    if name and len(name) > 2:
                        return name
        except (json.JSONDecodeError, TypeError):
            continue

    # Method 4: Meta tag og:site_name or company-related meta
    for meta in soup.find_all("meta", {"property": "og:title"}):
        content = meta.get("content", "")
        if " - " in content:
            # Often format: "Job Title - Company Name - Pracuj.pl"
            parts = content.split(" - ")
            if len(parts) >= 2:
                candidate = parts[-2].strip()
                if (
                    candidate
                    and "pracuj" not in candidate.lower()
                    and len(candidate) > 2
                ):
                    return candidate

    # Method 5: general text search for company pattern
    for div in soup.find_all(
        "div",
        class_=lambda c: c and "employer" in str(c).lower(),
    ):
        name = div.get_text(strip=True)
        if name and len(name) > 2 and len(name) < 100:
            return name

    return None


async def scrape_pracuj(query: str, limit: int = 5):
    """Scrape Pracuj.pl for job listings, extracting employer names."""
    print(f"Pracuj.pl: Searching for '{query}'")

    session: requests.Session = requests.Session(impersonate="chrome120")

    try:
        url = f"https://www.pracuj.pl/praca/{quote(query)};kw"
        response = session.get(url, timeout=15.0)
        soup = BeautifulSoup(response.text, "html.parser")

        # Find offer links
        offer_links: set[str] = set()
        for a_tag in soup.find_all("a", href=True):
            href_attr = a_tag["href"]
            href = href_attr[0] if isinstance(href_attr, list) else str(href_attr)
            if href.startswith("https://www.pracuj.pl/praca/") and "oferta" in href:
                clean_href = href.split("?")[0] if "?" in href else href
                offer_links.add(clean_href)

        links = list(offer_links)[:limit]
        print(f"  Found {len(links)} offers.")

        for idx, link in enumerate(links):
            print(f"  [{idx + 1}/{len(links)}] {link[:60]}...")
            try:
                offer_res = session.get(link, timeout=15.0)
                offer_soup = BeautifulSoup(offer_res.text, "html.parser")

                # Remove noise
                for tag in offer_soup(["script", "style", "nav", "footer"]):
                    tag.extract()

                title = (
                    str(offer_soup.title.string)
                    if offer_soup.title and offer_soup.title.string
                    else "Brak tytułu"
                )

                # Skip Cloudflare blocks
                if "Cloudflare" in title or "Just a moment" in title:
                    print("    Cloudflare block, skipping.")
                    continue

                # Extract the EMPLOYER name
                employer = extract_employer_from_html(offer_soup)
                text_content = offer_soup.get_text(separator=" ", strip=True)

                if not employer and len(text_content) > 100:
                    # Fallback: use title parsing
                    # Pracuj titles often: "Stanowisko - Firma - Pracuj.pl"
                    parts = title.replace(" | ", " - ").split(" - ")
                    for part in reversed(parts):
                        part = part.strip()
                        if (
                            part
                            and "pracuj" not in part.lower()
                            and len(part) > 3
                            and len(part) < 80
                        ):
                            employer = part
                            break

                if not employer:
                    print("    No employer found, skipping.")
                    continue

                if await check_company_exists(employer):
                    print(f"    -> Skipping duplicate: {employer}")
                    continue

                # Clean job title (remove company and site name)
                job_title = title.split(" - ")[0].strip()
                if not job_title:
                    job_title = title

                print(f"    FIRMA: {employer}")
                print(f"    Stanowisko: {job_title}")

                lead = LeadInsert(
                    source="pracuj.pl",
                    company_name=employer,
                    tender_title=job_title,
                    url=str(link),
                    ai_score=6,
                    ai_summary=f"Firma {employer} szuka: {job_title}",
                    full_content=text_content[:5000],
                    status="processed",
                )
                await insert_lead(lead)

            except Exception as ex:
                print(f"    Error: {ex}")

    except Exception as e:
        print(f"Error scraping pracuj.pl for '{query}': {e}")


async def run_pracuj_scraper(profile_name: str = "default"):
    """Run the Pracuj.pl scraper for all configured keywords."""
    print(f"Starting Pracuj.pl Scraper - Profile: {profile_name.upper()}...")
    profile = get_profile(profile_name)
    keywords = profile.pracuj_keywords
    for query in keywords:
        query = query.strip()
        if not query:
            continue
        await scrape_pracuj(query, limit=3)


if __name__ == "__main__":
    import asyncio

    asyncio.run(run_pracuj_scraper())

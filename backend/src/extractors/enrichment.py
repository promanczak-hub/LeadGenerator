import asyncio
import re
from curl_cffi import requests
from bs4 import BeautifulSoup
from src.extractors.enricher_llm import (
    extract_linkedin_info,
    extract_contact_info,
    EnrichmentResult,
    LinkedinResult,
)

from duckduckgo_search import DDGS


def validate_nip(nip: str | None) -> str | None:
    """
    Czyści i waliduje polski numer NIP.
    Zwraca 10-cyfrowy string lub None jeśli niepoprawny.
    """
    if not nip:
        return None
    
    # Usuwamy wszystko co nie jest cyfrą
    digits = re.sub(r"\D", "", nip)
    
    if len(digits) != 10:
        return None
        
    # Wagi do walidacji sumy kontrolnej NIP
    weights = [6, 5, 7, 2, 3, 4, 5, 6, 7]
    try:
        check_sum = sum(int(digits[i]) * weights[i] for i in range(9))
        control_digit = check_sum % 11
        
        if control_digit == int(digits[9]):
            return digits
    except (ValueError, IndexError):
        pass
        
    return None


async def search_duckduckgo_html(
    query: str, max_results: int = 5, time_range: str | None = None
) -> list[dict]:
    """Helper method to execute a search on DuckDuckGo using the ddgs package (async)."""
    def _do_search():
        results = []
        try:
            with DDGS() as ddgs:
                for r in ddgs.text(query, timelimit=time_range, max_results=max_results):
                    results.append(
                        {
                            "title": r.get("title", ""),
                            "href": r.get("href", ""),
                            "body": r.get("body", ""),
                        }
                    )
        except Exception as e:
            print(f"Error in DDG search: {e}")
        return results

    return await asyncio.to_thread(_do_search)


async def search_linkedin_for_ceo(company_name: str) -> LinkedinResult | None:
    """Wyszukuje CEO/Prezesa firmy na LinkedIn używając DuckDuckGo (async)."""
    print(f"[Enrichment] Wyszukiwanie osoby decyzyjnej LinkedIn dla: {company_name}")
    try:
        query = f"{company_name} LinkedIn (CEO OR Prezes OR Dyrektor OR Founder)"
        search_res = await search_duckduckgo_html(query, max_results=3)

        if not search_res:
            print("  Brak wyników wyszukiwania.")
            return None

        results_str = ""
        for r in search_res:
            results_str += (
                f"Title: {r['title']}\nURL: {r['href']}\nBody: {r['body']}\n\n"
            )

        print("  Przekazywanie wyników do analizy Gemini...")
        return await extract_linkedin_info(results_str)
    except Exception as e:
        print(f"  Błąd podczas wyszukiwania LinkedIn: {e}")
        return None


async def find_company_website(company_name: str) -> str | None:
    """Znajduje oficjalną stronę firmy (async)."""
    try:
        query = f"{company_name} oficjalna strona kontakt"
        search_res = await search_duckduckgo_html(query, max_results=3)
        for r in search_res:
            href = r["href"]
            # Ignorujemy popularne portale z danymi (krs, linkedin itp.)
            if not any(
                x in href
                for x in [
                    "facebook.com",
                    "linkedin.com",
                    "krs-",
                    "rejestr.io",
                    "aleo.com",
                    "gowork.pl",
                ]
            ):
                return href
        return None
    except Exception as e:
        print(f"  Błąd wyszukiwania strony firmy: {e}")
        return None


async def deep_scrape_company_website(company_name: str, url: str) -> EnrichmentResult | None:
    """Pobiera zawartość strony i wyciąga dane kontaktowe (async)."""
    print(f"[Enrichment] Skanowanie strony domowej: {url}")
    try:
        async with requests.AsyncSession(impersonate="chrome120") as session:
            response = await session.get(url, timeout=15.0, verify=False)
            soup = BeautifulSoup(response.text, "html.parser")

            # Filtrujemy niepotrzebne tagi
            for element in soup(["script", "style", "nav"]):
                element.extract()

            text_content = soup.get_text(separator=" ", strip=True)
            cut_text = text_content[:10000]

            print("  Analiza na obecność danych kontaktowych...")
            res = await extract_contact_info(cut_text)
            
            # Walidacja NIP jeśli został wyciągnięty
            if res and res.nip:
                valid_nip = validate_nip(res.nip)
                if not valid_nip:
                    print(f"  NIP {res.nip} nie przeszedł walidacji - odrzucam.")
                    res.nip = None
                else:
                    res.nip = valid_nip
                    
            return res
    except Exception as e:
        print(f"  Błąd scrapowania oficjalnej strony: {e}")
        return None


async def enrich_company_data(
    company_name: str,
) -> tuple[LinkedinResult | None, EnrichmentResult | None]:
    """Główna funkcja orkiestrująca wzbogacanie leada (async)."""
    # Uruchamiamy LinkedIn i szukanie strony równolegle
    linkedin_task = asyncio.create_task(search_linkedin_for_ceo(company_name))
    
    website_url = await find_company_website(company_name)
    contact_info = None
    
    if website_url:
        contact_info = await deep_scrape_company_website(company_name, website_url)
    
    linkedin_info = await linkedin_task
    return linkedin_info, contact_info

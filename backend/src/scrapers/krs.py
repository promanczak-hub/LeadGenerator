from src.extractors.enrichment import search_duckduckgo_html, enrich_company_data
from src.core.supabase import LeadInsert, insert_lead
import re


async def scrape_krs():
    print(
        "Starting KRS Scraping via DuckDuckGo Search for 'oddział zagranicznego przedsiębiorcy'"
    )

    try:
        query = '"oddział zagranicznego przedsiębiorcy" site:aleo.com'
        print(f"Szukanie bez site: {query}")

        found_companies = []
        # Otwieramy szeroko bez limitu czasu, bo DuckDuckGo rzadko indeksuje nowe podstrony aleo w czasie rzeczywistym
        results = search_duckduckgo_html(query, max_results=10)

        for r in results:
            title = r.get("title", "")
            href = r.get("href", "")
            body = r.get("body", "")

            print(f"[DEBUG] Found result: {title} | {href}")

            # Wyczyść końcówki typu krs-pobierz itp.
            company_name = (
                title.split(" - ")[0]
                .split(",")[0]
                .replace("NIP", "")
                .replace("KRS", "")
                .strip()
            )

            # Próba wyciagnięcia NIPu z tekstu jesli jest
            nip_match = re.search(r"NIP:?\s*([\d\-]{10,13})", body + title)
            nip = nip_match.group(1).replace("-", "") if nip_match else None

            # Odsiewamy śmieci - akceptujemy jeśli zawiera słowo oddział lub jest po prostu dłuższą nazwą firmy
            if (
                company_name
                and len(company_name) > 3
                and "Wpis oddziału" not in company_name
            ):
                found_companies.append({"name": company_name, "url": href, "nip": nip})

        if not found_companies:
            print("Nie znaleziono żadnych firm.")
            return

        print(
            f"Znaleziono {len(found_companies)} potencjalnych firm. Analizuję pierwsze 3...\n"
        )

        for item in found_companies[:3]:
            company_name = item["name"]
            detail_link = item["url"]
            nip = item["nip"]

            print(f"-> Znalazłem: {company_name}")
            print(f"   URL:       {detail_link}")
            if nip:
                print(f"   NIP:       {nip}")

            print("   --- Wzbogacanie danych (Enrichment) ---")
            linkedin_info, contact_info = await enrich_company_data(company_name)

            ai_summary_parts = [
                "Zarejestrowany oddział zagranicznego przedsiębiorcy (KRS)."
            ]
            if linkedin_info and linkedin_info.ceo_name:
                ai_summary_parts.append(
                    f"Osoba decyzyjna: {linkedin_info.ceo_name} {f'({linkedin_info.linkedin_url})' if linkedin_info.linkedin_url else ''}."
                )

            contact_email = contact_info.contact_email if contact_info else None
            contact_phone = contact_info.contact_phone if contact_info else None
            industry = contact_info.industry if contact_info else None
            nip_final = nip or (contact_info.nip if contact_info else None)

            lead = LeadInsert(
                source="KRS/WebScrape",
                company_name=company_name,
                tender_title="Oddział zagranicznego przedsiębiorcy w Polsce",
                url=detail_link,
                ai_score=8,
                ai_summary=" ".join(ai_summary_parts),
                contact_email=contact_email,
                contact_phone=contact_phone,
                nip=nip_final,
                industry=industry,
                full_content=f"Nazwa: {company_name}\nNIP: {nip}\nŹródło: {detail_link}",
            )

            if await insert_lead(lead):
                print("✅ Zapisano pomyślnie w bazie.\n")
            else:
                print("❌ Błąd zapisu.\n")

    except Exception as e:
        print(f"Error scraping KRS via DDGS: {e}")


if __name__ == "__main__":
    import asyncio

    asyncio.run(scrape_krs())

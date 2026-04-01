from datetime import datetime, timedelta
import httpx

from src.core.config import config
from src.core.supabase import LeadInsert, insert_lead
from src.extractors.enrichment import enrich_company_data


async def run_krs_scraper() -> None:
    """
    Scraper for Rejestr.io API v2
    Fetches newly registered companies from the National Court Register (KRS)
    based on predefined PKD codes (e.g., construction).
    Requires a valid REJESTR_IO_KEY in .env.
    """
    api_key = config.REJESTR_IO_KEY
    if not api_key or api_key == "twoj_klucz_api_z_rejestr_io":
        print("Brak poprawnego klucza REJESTR_IO_KEY w .env. Pomijam scraper KRS.")
        return

    # Check companies registered yesterday (usually KRS is published with a 1-day delay)
    target_date = (datetime.now() - timedelta(days=1)).strftime("%Y-%m-%d")
    print(f"Rozpoczynam pobieranie nowych spółek z KRS dla daty: {target_date}")

    base_url = "https://rejestr.io/api/v2/org"
    headers = {"Authorization": api_key}

    total_found = 0

    with httpx.Client(timeout=30.0) as client:
        for pkd in config.KRS_INDUSTRIES:
            pkd = pkd.strip()
            if not pkd:
                continue

            print(f"Szukam PKD: {pkd} ...")
            params = {
                "pkd": pkd,
                "data_pierwszego_wpisu_do_krs": f"gte:{target_date}",
            }

            try:
                response = client.get(base_url, headers=headers, params=params)
                if response.status_code == 401:
                    print("Błąd 401: Nieprawidłowy klucz API Rejestr.io!")
                    return
                elif response.status_code != 200:
                    print(
                        f"Błąd API {response.status_code} dla PKD {pkd}: {response.text}"
                    )
                    continue

                data = response.json()
                items = data.get("items", [])

                print(f" -> Znaleziono {len(items)} firm w PKD {pkd}")

                for item in items:
                    name = item.get("name", "")
                    krs = item.get("krs", "")
                    nip = item.get("nip", "")

                    if not name or not krs:
                        continue

                    # Zapisujemy tylko jeśli firma wygląda sensownie
                    # Rejestr.io dla wpisów z KRS zwraca zazwyczaj spółki prawa handlowego
                    company_url = f"https://rejestr.io/krs/{krs}"
                    print(f"   Przetwarzam: {name} (KRS: {krs})")

                    # Wzbogacanie danych na LinkedIn / DuckDuckGo
                    linkedin_info, contact_info = await enrich_company_data(name)

                    ai_summary_parts = [
                        f"Nowo zarejestrowana spółka. Główny PKD: {pkd}."
                    ]
                    if linkedin_info and linkedin_info.ceo_name:
                        ai_summary_parts.append(
                            f"Prezes/Zarząd: {linkedin_info.ceo_name}"
                        )
                        if linkedin_info.linkedin_url:
                            ai_summary_parts.append(f"({linkedin_info.linkedin_url})")

                    contact_email = contact_info.contact_email if contact_info else None
                    contact_phone = contact_info.contact_phone if contact_info else None
                    industry = contact_info.industry if contact_info else pkd
                    nip_final = nip or (contact_info.nip if contact_info else None)

                    lead = LeadInsert(
                        source="KRS/Nowe Spółki",
                        company_name=name,
                        tender_title="Nowa rejestracja w KRS",
                        url=company_url,
                        ai_score=9,  # Nowe spółki B2B dostają z góry wysoki wynik
                        ai_summary=" ".join(ai_summary_parts),
                        contact_email=contact_email,
                        contact_phone=contact_phone,
                        nip=nip_final,
                        industry=industry,
                        full_content=f"Nowo zarejestrowany podmiot.\nNazwa: {name}\nKRS: {krs}\nNIP: {nip}\nPKD: {pkd}",
                    )

                    if await insert_lead(lead):
                        print("    ✅ Zapisano pomyślnie w bazie.")
                        total_found += 1
                    else:
                        print("    ❌ Błąd zapisu lub duplikat.")

            except Exception as e:
                print(f"Błąd podczas szukania PKD {pkd}: {e}")

    print(f"Zakończono pobieranie z KRS. Dodano {total_found} nowych leadów.")

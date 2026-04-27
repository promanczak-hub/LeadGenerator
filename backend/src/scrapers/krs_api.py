import asyncio
from datetime import datetime, timedelta
from typing import List, Optional, Dict, Any
from curl_cffi import requests

from src.core.supabase import LeadInsert, insert_lead


class KRSClient:
    """
    Client for interacting with the official KRS Open API (api-krs.ms.gov.pl).
    """

    BASE_URL = "https://api-krs.ms.gov.pl/api/krs"

    def __init__(self, impersonate: str = "chrome120") -> None:
        self.session: requests.Session = requests.Session(impersonate=impersonate)  # type: ignore[arg-type]

    async def get_daily_bulletin(self, date: str) -> List[str]:
        """
        Fetches the bulletin for a specific date (YYYY-MM-DD).
        Returns a list of KRS numbers that were updated.
        """
        url = f"{self.BASE_URL}/Biuletyn/{date}"
        try:
            # We wrap sync call in loop.run_in_executor if needed,
            # but curl_cffi doesn't have native async in this version's toolset.
            # Using loop.run_in_executor for non-blocking.
            loop = asyncio.get_event_loop()
            response = await loop.run_in_executor(
                None, lambda: self.session.get(url, timeout=15.0)
            )

            if response.status_code == 404:
                print(f"No bulletin found for date {date}")
                return []

            response.raise_for_status()
            data = response.json()
            return data if isinstance(data, list) else []
        except Exception as e:
            print(f"Error fetching KRS bulletin for {date}: {e}")
            return []

    async def fetch_krs_details(
        self, krs: str, register: str = "P"
    ) -> Optional[Dict[str, Any]]:
        """
        Fetches current actual extract (OdpisAktualny) for a given KRS.
        register: 'P' (Entrepreneurs), 'S' (Associations/Foundations)
        """
        url = f"{self.BASE_URL}/OdpisAktualny/{krs}"
        params = {"rejestr": register, "format": "json"}
        try:
            loop = asyncio.get_event_loop()
            response = await loop.run_in_executor(
                None, lambda: self.session.get(url, params=params, timeout=10.0)
            )

            if response.status_code == 404:
                return None

            response.raise_for_status()
            return response.json()
        except Exception as e:
            print(f"Error fetching details for KRS {krs} ({register}): {e}")
            return None

    def transform_to_lead(self, data: Dict[str, Any]) -> Optional[LeadInsert]:
        """
        Transforms KRS API response data into a LeadInsert model.
        """
        try:
            odpis = data.get("odpis", {})
            naglowek = odpis.get("naglowekA", {})
            dane_content = odpis.get("dane", {})

            # Entities can have different structures, but dzial1/danePodmiotu is common
            dzial1 = dane_content.get("dzial1", {})
            dane_podmiotu = dzial1.get("danePodmiotu", {})

            if not dane_podmiotu:
                print(
                    f"Warning: Missing danePodmiotu in KRS {naglowek.get('numerKRS')}"
                )
                return None

            krs = naglowek.get("numerKRS")
            full_name = dane_podmiotu.get("nazwa")

            # Fallback for name if missing in danePodmiotu
            if not full_name:
                full_name = dane_podmiotu.get("nazwaSkrocona") or f"Podmiot KRS {krs}"

            identyfikatory = dane_podmiotu.get("identyfikatory", {})
            nip = identyfikatory.get("nip")

            # Extracting address
            siedziba = dane_podmiotu.get("siedzibaIAdres", {}).get("adres", {})
            city = siedziba.get("miejscowosc")
            street = siedziba.get("ulica")
            house_no = siedziba.get("numerDomu")
            address_str = f"{street} {house_no}, {city}" if street else city

            # Extracting forma prawna (Type of Company)
            forma_prawna_raw = dane_podmiotu.get("formaPrawna")
            forma_prawna = (
                str(forma_prawna_raw).upper() if forma_prawna_raw else "KRS / NOWY WPIS"
            )

            # Extracting purpose/PKD
            dzial3 = dane_content.get("dzial3", {})
            pkd_info = dzial3.get("przedmiotDzialalnosci", {}) or dzial1.get(
                "przedmiotDzialalnosci", {}
            )

            pkd_list = pkd_info.get("przedmiotPrzewazajacejDzialalnosci", [])
            industry = "Nieokreślony"
            if pkd_list:
                pkd_item = pkd_list[0]
                opis = pkd_item.get("opis", "")

                # Assemble PKD Code e.g. "41.20.Z"
                kod_dzial = pkd_item.get("kodDzial", "")
                kod_klasa = pkd_item.get("kodKlasa", "")
                kod_podklasa = pkd_item.get("kodPodklasa", "")

                parts = [p for p in [kod_dzial, kod_klasa, kod_podklasa] if p]
                if len(parts) == 3:
                    pkd_code = f"{kod_dzial}.{kod_klasa}.{kod_podklasa}"
                elif parts:
                    pkd_code = ".".join(parts)
                else:
                    pkd_code = ""

                industry = f"{pkd_code} | {opis}" if pkd_code else opis

            # Truncate potentially long fields
            safe_industry = industry[:95] + "..." if len(industry) > 97 else industry
            safe_company = (
                full_name[:245] + "..." if len(full_name) > 248 else full_name
            )
            safe_forma_prawna = (
                forma_prawna[:95] + "..." if len(forma_prawna) > 97 else forma_prawna
            )

            return LeadInsert(
                source="KRS Monitor",
                company_name=safe_company,
                tender_title=safe_forma_prawna,
                url=f"https://ekrs.ms.gov.pl/web/wyszukiwarka-krs/strona-glowna?p_p_id=SearchKRS_WAR_SearchKRSportlet&krs={krs}",
                ai_score=50,
                ai_summary=f"KRS: {krs} | Automatyczny import z KRS. Branża: {industry}. Lokalizacja: {address_str}",
                full_content=str(data)[:2000],
                nip=nip,
                industry=safe_industry,
            )
        except Exception as e:
            print(
                f"Transformation error for KRS {krs if 'krs' in locals() else 'unknown'}: {e}"
            )
            return None


async def run_krs_sync(date: Optional[str] = None):
    """
    Main orchestrator for KRS synchronization.
    If date is None, scales for 'yesterday'.
    """
    if date is None:
        yesterday = datetime.now() - timedelta(days=1)
        date = yesterday.strftime("%Y-%m-%d")

    print(f"Starting KRS Sync for date: {date}...")
    client = KRSClient()
    krs_numbers = await client.get_daily_bulletin(date)

    if not krs_numbers:
        print(f"No records found for {date}.")
        return

    print(f"Found {len(krs_numbers)} entries to process.")

    processed_count = 0
    for krs in krs_numbers:
        # We try 'P' register (Entrepreneurs) first as it's most common for leads
        details = await client.fetch_krs_details(krs, register="P")
        if not details:
            # Try 'S' register (Associations/Foundations)
            details = await client.fetch_krs_details(krs, register="S")

        if details:
            lead = client.transform_to_lead(details)
            if lead:
                success = await insert_lead(lead)
                if success:
                    processed_count += 1
                    if processed_count % 10 == 0:
                        print(f"  Processed {processed_count} leads...")

    print(
        f"KRS Sync completed. Imported {processed_count} leads from {len(krs_numbers)} entries."
    )


if __name__ == "__main__":
    asyncio.run(run_krs_sync())

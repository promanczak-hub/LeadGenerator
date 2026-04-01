import os
import sys
from postgrest import APIResponse

# Dodanie ścieżki do backendu
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from src.core.supabase import supabase


async def clean_google_leads():
    # Pobierz wszystkie leady z Google
    print("Pobieram leady ze źródła 'Google Search'...")
    response: APIResponse = (
        await supabase.table("leads")
        .select("*")
        .eq("source", "Google Search")
        .execute()
    )

    leads = response.data
    print(f"Znaleziono {len(leads)} leadów łącznie.")

    # Słownik potencjalnie błędnych jednosłowowych haseł (nie-firmy)
    bad_keywords = {
        "Jedno",
        "Pijany",
        "Skarszewianie",
        "InPost",
        "Startują",
        "Smart",
        "Podpisanie",
        "Śmiertelny",
    }

    to_delete = []

    for lead in leads:
        company_name = lead.get("company_name", "")
        # Jeżeli company_name jest puste, ma jedno słowo, lub zawiera słowo z listy
        words = company_name.split()
        if len(words) == 1 and company_name in bad_keywords:
            to_delete.append(lead)
        elif len(words) == 1 and company_name not in [
            "Budimex",
            "Skanska",
            "Strabag",
            "Mirbud",
            "Porr",
            "Erbud",
            "Unibep",
        ]:
            # Podejrzane jednosłowowe firmy, usuwamy jeśli są krótkie, ale wypiszemy je
            if len(company_name) < 15 and company_name[0].isupper():
                to_delete.append(lead)
        elif "Kłopoty Chińczyków" in company_name or "kierowca" in company_name.lower():
            to_delete.append(lead)

    if not to_delete:
        print("Nie znaleziono leadów do usunięcia.")
        return

    print(f"Zidentyfikowano {len(to_delete)} błędnych lub podejrzanych rekordów:")
    for lead_row in to_delete:
        print(
            f" - {lead_row.get('company_name')} (ID: {lead_row.get('id')}) z artykułu: {lead_row.get('tender_title')[:30]}..."
        )

    # Usuwamy z bazy
    print("\nRozpoczynam usuwanie...")
    for lead_row in to_delete:
        await supabase.table("leads").delete().eq("id", lead_row.get("id")).execute()
        print(f"Usunięto rekord ID {lead_row.get('id')}")

    print("Gotowe. Wyczyściłem bazę danych z podejrzanych leadów od Google News.")


if __name__ == "__main__":
    import asyncio

    asyncio.run(clean_google_leads())

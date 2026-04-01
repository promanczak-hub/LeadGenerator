import asyncio
import json
from src.scrapers.krs_api import KRSClient


async def test_krs_api():
    client = KRSClient()
    
    # Test 1: Fetching bulletin for a known recent date
    # Government data usually lags by a day or so, or is same-day
    test_date = "2026-03-29" 
    print(f"Testing Bulletin for {test_date}...")
    krs_numbers = await client.get_daily_bulletin(test_date)
    
    if krs_numbers:
        print(f"  Success! Found {len(krs_numbers)} entries.")
        # Test 2: Fetching details for the first one
        sample_krs = krs_numbers[0]
        print(f"Testing Details for KRS {sample_krs}...")
        
        details = await client.fetch_krs_details(sample_krs)
        if not details:
            # Try S register
            details = await client.fetch_krs_details(sample_krs, register="S")
            
        if details:
            print("  Success! Got JSON details.")
            # Test 3: Transformation
            lead = client.transform_to_lead(details)
            if lead:
                print("  Success! Transformed to LeadInsert.")
                print(f"  Company: {lead.company_name}")
                print(f"  NIP: {lead.nip}")
                print(f"  KRS: {lead.krs}")
                print(f"  Industry: {lead.industry}")
                print(f"  Summary: {lead.ai_summary}")
            else:
                print("  Failed to transform to LeadInsert.")
        else:
            print("  Failed to get details.")
    else:
        print("  No bulletin found for this date. (It might be empty or too early)")


if __name__ == "__main__":
    asyncio.run(test_krs_api())

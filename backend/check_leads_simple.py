
import asyncio
import sys
from pathlib import Path
sys.path.append(str(Path.cwd()))
from src.core.supabase import supabase

async def check_leads():
    try:
        res = await supabase.table('leads').select('id, source, company_name, tender_title, url, updated_at').eq('source', 'EZ').limit(3).execute()
        for r in res.data:
            print(f"ID: {r['id']}\nUpdated At: {r['updated_at']}\nSource: {r['source']}\nCompany: {r['company_name']}\nTitle: {r['tender_title']}\nURL: {r['url']}\n---")
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    asyncio.run(check_leads())

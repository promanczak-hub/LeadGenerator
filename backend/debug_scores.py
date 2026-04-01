import asyncio
from src.core.supabase import supabase

async def main():
    res = await supabase.table("leads").select("ai_score, company_name").order("inserted_at", desc=True).limit(20).execute()
    print("Recent AI Scores:")
    for row in res.data:
        print(f"{row.get('company_name')}: {row.get('ai_score')}")

if __name__ == "__main__":
    asyncio.run(main())

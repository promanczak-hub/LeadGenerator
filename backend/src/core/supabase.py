from supabase._async.client import AsyncClient
from pydantic import BaseModel, Field
from typing import Optional

from src.core.config import config

if not config.SUPABASE_URL or not config.SUPABASE_KEY:
    raise ValueError(
        "SUPABASE_URL and SUPABASE_KEY must be set in environment variables."
    )

supabase: AsyncClient = AsyncClient(config.SUPABASE_URL, config.SUPABASE_KEY)


class LeadInsert(BaseModel):
    source: str
    company_name: str
    tender_title: str
    url: Optional[str] = None
    ai_score: int = Field(default=10)
    ai_summary: Optional[str] = None
    full_content: Optional[str] = None
    status: str = Field(default="new")
    contact_email: Optional[str] = None
    contact_phone: Optional[str] = None
    nip: Optional[str] = None
    industry: Optional[str] = None


async def insert_lead(lead: LeadInsert) -> bool:
    try:
        response = (
            await supabase.table("leads")
            .insert(lead.model_dump(exclude_none=True))
            .execute()
        )
        return len(response.data) > 0
    except Exception as e:
        print(f"Error inserting lead {lead.company_name}: {e}")
        return False

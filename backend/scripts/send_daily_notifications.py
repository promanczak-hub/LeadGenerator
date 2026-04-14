import sys
from pathlib import Path

# Fix python path for script execution
sys.path.append(str(Path(__file__).parent.parent))

import logging
from typing import Any, cast
from datetime import datetime
from src.core.supabase import supabase
from src.services.email_service import GmailNotifier

logging.basicConfig(
    level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s"
)
logger = logging.getLogger(__name__)


from datetime import datetime, timedelta

async def fetch_unsent_leads() -> list[dict[str, Any]]:
    """Fetch leads from Supabase where email_sent is false and inserted in the last 24h."""
    try:
        yesterday = (datetime.now() - timedelta(days=1)).isoformat()
        response = (
            await supabase.table("leads")
            .select("*")
            .eq("email_sent", False)
            .gte("inserted_at", yesterday)
            .execute()
        )
        return cast(list[dict[str, Any]], response.data)
    except Exception as e:
        logger.error(f"PostgREST Error while fetching leads: {e}")
        return []


async def mark_leads_as_sent(lead_ids: list[str]) -> bool:
    """Mark leads as email_sent=true and update notified_at."""
    if not lead_ids:
        return True

    try:
        current_time = datetime.now().isoformat()
        # Because supabase-py doesn't currently support an easy `in_` update
        # for multiple IDs in a single query reliably, we'll update them one by one or in a loop.
        # Alternatively, using `in_` filter:
        response = (
            await supabase.table("leads")
            .update({"email_sent": True, "notified_at": current_time})
            .in_("id", lead_ids)
            .execute()
        )
        return len(response.data) > 0
    except Exception as e:
        logger.error(f"Error marking leads as sent: {e}")
        return False


async def main():
    logger.info("=== Starting Daily Lead Notifications ===")

    # 1. Fetch leads
    leads = await fetch_unsent_leads()

    if not leads:
        logger.info("No new leads to send today.")
        return

    logger.info(f"Found {len(leads)} unsent leads. Preparing email...")

    # 2. Send email
    notifier = GmailNotifier()
    success = notifier.send_daily_report(leads)

    # 3. Mark as sent
    if success:
        lead_ids = [str(lead["id"]) for lead in leads if "id" in lead]
        marked = await mark_leads_as_sent(lead_ids)
        if marked:
            logger.info("Successfully updated database records.")
        else:
            logger.warning("Email sent, but failed to update database records.")
    else:
        logger.error("Email sending failed. Database records were NOT updated.")

    logger.info("=== Notification Process Finished ===")


if __name__ == "__main__":
    import asyncio

    asyncio.run(main())

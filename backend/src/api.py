import os
from fastapi import FastAPI, BackgroundTasks
from src.scrapers.google import run_google_scraper
from src.scrapers.pracuj import run_pracuj_scraper
from src.scrapers.krs_new_companies import run_krs_scraper
from src.scrapers.gunb import run_gunb_scraper
from src.scrapers.baza_konkurencyjnosci import run_bk_scraper

app = FastAPI(title="LeadGenerator API")


@app.get("/")
async def root():
    return {"status": "ok", "service": "leadgen-backend"}


@app.post("/run-scrapers")
async def trigger_scrapers(background_tasks: BackgroundTasks):
    """
    Endpoint to trigger all scrapers in the background.
    Useful for Cloud Scheduler (CRON).
    """
    background_tasks.add_task(run_all_scrapers)
    return {"message": "Scrapers started in background"}


@app.post("/send-notifications")
async def trigger_notifications(background_tasks: BackgroundTasks):
    """
    Endpoint to trigger daily lead notifications.
    Useful for Cloud Scheduler (CRON).
    """
    from scripts.send_daily_notifications import main as send_emails

    # We use a wrapper to handle asyncio structure properly if needed
    # but FastAPI BackgroundTasks can accept async directly if main is async.
    background_tasks.add_task(send_emails)
    return {"message": "Notifications started in background"}


async def run_all_scrapers():
    print("=== SCRAPER RUN STARTED VIA API ===")
    await run_google_scraper()
    await run_pracuj_scraper()
    await run_krs_scraper()
    await run_gunb_scraper()
    await run_bk_scraper()
    print("=== SCRAPER RUN COMPLETED ===")


if __name__ == "__main__":
    import uvicorn

    port = int(os.environ.get("PORT", 8080))
    uvicorn.run(app, host="0.0.0.0", port=port)

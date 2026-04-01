import sys
import asyncio
from src.scrapers.google import run_google_scraper
from src.scrapers.pracuj import run_pracuj_scraper
from src.scrapers.krs_new_companies import run_krs_scraper
from src.scrapers.gunb import run_gunb_scraper
from src.scrapers.baza_konkurencyjnosci import run_bk_scraper


async def main():
    print("=== Starting LeadGenerator Backend Scrapers ===")

    args = sys.argv[1:]

    # Check if we should run notifications instead of scrapers
    if "email" in args:
        print("Running Email Notifications Script...")
        from scripts.send_daily_notifications import main as send_emails

        # Check if send_emails is a coroutine (it was refactored to async)
        if asyncio.iscoroutinefunction(send_emails):
            await send_emails()
        else:
            send_emails()
        sys.exit(0)

    if not args:
        print("Running all scrapers...")
        await run_google_scraper()
        await run_pracuj_scraper()
        await run_krs_scraper()
        await run_gunb_scraper()
        await run_bk_scraper()
    elif "google" in args:
        print("Running only Google Scraper...")
        await run_google_scraper()
    elif "pracuj" in args:
        print("Running only Pracuj.pl Scraper...")
        await run_pracuj_scraper()
    elif "krs" in args:
        print("Running only KRS Scraper...")
        await run_krs_scraper()
    elif "gunb" in args:
        print("Running only GUNB Building Permits Scraper...")
        await run_gunb_scraper()
    elif "bk" in args:
        print("Running only Baza Konkurencyjności Scraper...")
        await run_bk_scraper()
    else:
        print(
            f"Unknown arguments: {args}. Use 'google', 'pracuj', 'krs', 'gunb', 'bk' or 'email'."
        )

    print("=== Scraping Completed ===")


if __name__ == "__main__":
    asyncio.run(main())

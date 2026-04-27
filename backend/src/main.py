import sys
import asyncio
from src.scrapers.google import run_google_scraper
from src.scrapers.pracuj import run_pracuj_scraper
from src.scrapers.krs_new_companies import run_krs_scraper
from src.scrapers.gunb import run_gunb_scraper
from src.scrapers.baza_konkurencyjnosci import run_bk_scraper
from src.scrapers.ezamowienia_bzp import run_bzp_scraper
from src.scrapers.ted_gddkia import run_ted_scraper
from src.scrapers.google_jobs import run_jobs_scraper


async def main():
    print("=== Starting LeadGenerator Backend Scrapers ===")

    args = sys.argv[1:]
    profile_name = "default"

    if "--profile" in args:
        idx = args.index("--profile")
        if idx + 1 < len(args):
            profile_name = args[idx + 1]
            args.pop(idx + 1)
            args.pop(idx)

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

    print(f"Using profile: {profile_name.upper()}")

    if not args:
        print("Running all scrapers...")
        await run_google_scraper(profile_name)
        await run_jobs_scraper(profile_name)
        await run_pracuj_scraper(profile_name)
        await run_krs_scraper(profile_name)
        await run_gunb_scraper(profile_name)
        await run_bk_scraper(profile_name)
        await run_bzp_scraper(profile_name)
        await run_ted_scraper(profile_name)
    elif "google" in args or "jobs" in args:
        print("Running Google Search scrapers (News + Jobs)...")
        await run_google_scraper(profile_name)
        await run_jobs_scraper(profile_name)
    elif "pracuj" in args:
        print("Running only Pracuj.pl Scraper...")
        await run_pracuj_scraper(profile_name)
    elif "krs" in args:
        print("Running only KRS Scraper...")
        await run_krs_scraper(profile_name)
    elif "gunb" in args:
        print("Running only GUNB Building Permits Scraper...")
        await run_gunb_scraper(profile_name)
    elif "bk" in args:
        print("Running only Baza Konkurencyjności Scraper...")
        await run_bk_scraper(profile_name)
    elif "ez" in args:
        print("Running only e-Zamówienia BZP Scraper...")
        await run_bzp_scraper(profile_name)
    elif "ted" in args:
        print("Running only TED GDDKiA Scraper...")
        await run_ted_scraper(profile_name)
    else:
        print(
            f"Unknown arguments: {args}. Use 'google', 'jobs', 'pracuj', 'krs', 'gunb', 'bk', 'ez', 'ted' or 'email'."
        )

    print("=== Scraping Completed ===")


if __name__ == "__main__":
    asyncio.run(main())

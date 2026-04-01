import os
from pathlib import Path
from dotenv import load_dotenv

env_path = Path(__file__).parent.parent.parent / ".env"
load_dotenv(dotenv_path=env_path)


class Config:
    SUPABASE_URL = os.getenv("SUPABASE_URL", "")
    SUPABASE_KEY = os.getenv("SUPABASE_KEY", "")
    GEMINI_API_KEY = os.getenv("GEMINI_API_KEY", "")
    GEMINI_MODEL = os.getenv("GEMINI_MODEL", "gemini-2.0-flash-exp")

    # Email settings
    GMAIL_SENDER = os.getenv("GMAIL_SENDER", "")
    GMAIL_APP_PASSWORD = os.getenv("GMAIL_APP_PASSWORD", "")
    NOTIFICATION_EMAILS = os.getenv("NOTIFICATION_EMAILS", "").split(",")

    # Default search keywords if not overridden
    PRACUJ_KEYWORDS = os.getenv(
        "PRACUJ_KEYWORDS", "rozbudowa platformy,przetarg,wdrożenie systemu"
    ).split(",")
    GOOGLE_KEYWORDS = os.getenv(
        "GOOGLE_KEYWORDS",
        "konsorcjum,przetarg,wybuduje,wygrała przetarg wdrożenie,podpisała umowę na realizację,GDDKiA przetarg,GDDKiA podpisanie umowy,GDDKiA najkorzystniejsza oferta",
    ).split(",")

    # GUNB – Building permits scraper
    GUNB_PROVINCES: list[str] = os.getenv(
        "GUNB_PROVINCES",
        "mazowieckie,slaskie,wielkopolskie,dolnoslaskie,malopolskie",
    ).split(",")
    GUNB_KEYWORDS: list[str] = os.getenv(
        "GUNB_KEYWORDS",
        "hala,magazyn,droga,most,wiadukt,tunel,linia kolejowa,budynek przemysłowy,zakład,centrum logistyczne",
    ).split(",")
    GUNB_DAYS_BACK: int = int(os.getenv("GUNB_DAYS_BACK", "7"))
    GUNB_MAX_LEADS_PER_PROVINCE: int = int(
        os.getenv("GUNB_MAX_LEADS_PER_PROVINCE", "10")
    )

    # Baza Konkurencyjności
    BK_PAGES: int = int(os.getenv("BK_PAGES", "3"))

    # KRS / Rejestr.io
    REJESTR_IO_KEY = os.getenv("REJESTR_IO_KEY", "")
    KRS_INDUSTRIES: list[str] = os.getenv(
        "KRS_INDUSTRIES",
        "41.20.Z,42.11.Z,42.12.Z,42.13.Z,42.21.Z,42.22.Z,42.91.Z,42.99.Z,43.11.Z,43.12.Z".strip(),
    ).split(",")

    # Tavily
    TAVILY_API_KEY = os.getenv("TAVILY_API_KEY", "")


config = Config()

import os
from pathlib import Path
from dotenv import load_dotenv

# Try to load from various potential locations
load_dotenv(dotenv_path=Path(__file__).parent.parent.parent / ".env")
load_dotenv(dotenv_path=Path(__file__).parent.parent / ".env")  # backend root
load_dotenv()  # current dir


class Config:
    SUPABASE_URL = os.getenv("SUPABASE_URL", "")
    SUPABASE_KEY = os.getenv("SUPABASE_KEY", "")
    GEMINI_API_KEY = os.getenv("GEMINI_API_KEY", "")
    GEMINI_MODEL_ID: str = os.getenv("GEMINI_MODEL_ID", "gemini-2.0-flash")
    ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")
    LLM_PROVIDER = os.getenv("LLM_PROVIDER", "gemini")

    # Email settings
    GMAIL_SENDER = os.getenv("GMAIL_SENDER", "")
    GMAIL_APP_PASSWORD = os.getenv("GMAIL_APP_PASSWORD", "")
    NOTIFICATION_EMAILS = os.getenv("NOTIFICATION_EMAILS", "").split(",")
    CC_EMAILS = [
        email.strip()
        for email in os.getenv("CC_EMAILS", "").split(",")
        if email.strip()
    ]

    # Default search keywords if not overridden
    PRACUJ_KEYWORDS = os.getenv(
        "PRACUJ_KEYWORDS", "rozbudowa platformy,przetarg,wdrożenie systemu"
    ).split(",")
    GOOGLE_KEYWORDS = os.getenv(
        "GOOGLE_KEYWORDS",
        "konsorcjum,przetarg,wybuduje,wygrała przetarg wdrożenie,podpisała umowę na realizację,GDDKiA przetarg,GDDKiA podpisanie umowy,GDDKiA najkorzystniejsza oferta,PSE przetarg,PSE podpisanie umowy,Polskie Sieci Elektroenergetyczne",
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


config = Config()

from pydantic import BaseModel
from typing import List


class ScrapeProfile(BaseModel):
    name: str
    pracuj_keywords: List[str]
    google_keywords: List[str]
    gunb_keywords: List[str]
    krs_industries: List[str]
    bzp_keywords: List[str] = []
    ted_keywords: List[str] = []
    jobs_keywords: List[str] = []  # New: for hiring signals
    chamber_keywords: List[str] = []  # New: for bilateral chambers


# Domyślny profil dotychczasowy (Budownictwo / IT / Przetargi)
PROFILE_DEFAULT = ScrapeProfile(
    name="default",
    pracuj_keywords=["rozbudowa platformy", "przetarg", "wdrożenie systemu"],
    google_keywords=[
        "konsorcjum",
        "przetarg",
        "wybuduje",
        "wygrała przetarg wdrożenie",
        "podpisała umowę na realizację",
        "GDDKiA przetarg",
        "GDDKiA podpisanie umowy",
        "GDDKiA najkorzystniejsza oferta",
        "PSE przetarg",
        "PSE podpisanie umowy",
        "Polskie Sieci Elektroenergetyczne",
    ],
    gunb_keywords=[
        "hala",
        "magazyn",
        "droga",
        "most",
        "wiadukt",
        "tunel",
        "linia kolejowa",
        "budynek przemysłowy",
        "zakład",
        "centrum logistyczne",
    ],
    krs_industries=[
        "41.20.Z",
        "42.11.Z",
        "42.12.Z",
        "42.13.Z",
        "42.21.Z",
        "42.22.Z",
        "42.91.Z",
        "42.99.Z",
        "43.11.Z",
        "43.12.Z",
    ],
    bzp_keywords=[],
    ted_keywords=["generalna dyrekcja dróg krajowych", "gddkia", "pkp", "kolej"],
    jobs_keywords=[
        "kierownik budowy",
        "specjalista ds. przetargów",
        "inżynier kontraktu",
    ],
    chamber_keywords=[
        '"Polsko-Niemiecka Izba Przemysłowo-Handlowa" (inwestycja OR kontrakt OR planuje)',
        '"Amerykańska Izba Handlowa" OR "AmCham" (inwestycja OR nowa siedziba)',
        '"Brytyjsko-Polska Izba Handlowa" (inwestycja OR kontrakt)',
        '"Francusko-Polska Izba Gospodarcza" (firma OR projekt OR umowa)',
        '"Włoska Izba Handlowo-Przemysłowa" (inwestycja OR firma)',
        '"Hiszpańsko-Polska Izba Gospodarcza" (inwestycja OR kontrakt)',
        '"Skandynawsko-Polska Izba Gospodarcza" OR "SPCC" (inwestycja OR firma)',
        '"Niderlandzko-Polska Izba Gospodarcza" (inwestycja OR rozbudowa)',
        '"Belgijska Izba Gospodarcza" (firma OR projekt)',
        '"Szwajcarska Izba Gospodarcza" (inwestycja OR kontrakt)',
    ],
)

PROFILE_IT = ScrapeProfile(
    name="it",
    pracuj_keywords=[
        "nowy projekt IT",
        "wdrożenie chmury",
        "transformacja cyfrowa",
        "AI",
        "machine learning",
        "data center",
    ],
    google_keywords=[
        "wygrała przetarg IT",
        "umowa na wdrożenie systemu",
        "dostawa sprzętu IT",
        "cyberbezpieczeństwo przetarg",
        "cyfryzacja umowa",
    ],
    gunb_keywords=[
        "serwerownia",
        "data center",
        "centrum danych",
        "hub technologiczny",
    ],
    krs_industries=["62.01.Z", "62.02.Z", "62.03.Z", "62.09.Z", "63.11.Z", "63.12.Z"],
    bzp_keywords=[
        "systemów informatycznych",
        "licencji",
        "sprzętu komputerowego",
        "oprogramowania",
    ],
    ted_keywords=["software", "hardware", "IT services"],
    jobs_keywords=[
        "cloud architect",
        "devops engineer",
        "system administrator",
        "analityk biznesowy",
    ],
)

PROFILE_LOGISTICS = ScrapeProfile(
    name="logistics",
    pracuj_keywords=[
        "centrum dystrybucyjne",
        "spedycja",
        "kierowca C+E",
        "nowy magazyn",
        "flota",
    ],
    google_keywords=[
        "nowe centrum logistyczne",
        "rozbudowa magazynu",
        "umowa na dostawę floty",
        "wygrała przetarg transport",
    ],
    gunb_keywords=[
        "centrum logistyczne",
        "magazyn",
        "hala magazynowa",
        "terminal",
        "sortownia",
    ],
    krs_industries=["49.41.Z", "52.10.B", "52.29.A", "52.29.B"],
    bzp_keywords=["usługi transportowe", "dostawa pojazdów", "spedytor"],
    ted_keywords=["transport logistics", "freight forwarding"],
    jobs_keywords=["kierownik magazynu", "spedytor międzynarodowy", "kierowca C+E"],
)

PROFILE_RAC = ScrapeProfile(
    name="rac",
    pracuj_keywords=[
        "budujemy zespół sprzedaży",
        "rekrutujemy handlowców",
        "rozwój rynku",
        "wejście na nowe regiony",
    ],
    google_keywords=[
        '"firma budowlana" AND "buduje flotę"',
        '"generalny wykonawca" AND "flota"',
        '"firma remontowa" AND "rozwój"',
        '"instalacje elektryczne" AND "serwis terenowy"',
        '"realizacja projektów w całej Polsce" AND "ogłasza"',
        '"serwis mobilny" AND "poszukuje"',
        '"serwis terenowy" AND "inwestycja"',
        '"serwis techniczny" AND "nowy"',
        '"realizacja projektów czasowych"',
        '"elastyczne zasoby"',
        '"szybka realizacja zleceń"',
        '"działalność na terenie całej Polski" AND "firma"',
        '"organizacja eventów" AND "realizacja"',
        '"obsługa wydarzeń"',
        '"produkcja eventowa"',
        '"produkcja filmowa" AND "plan zdjęciowy"',
        '"obsługa planów zdjęciowych"',
    ],
    gunb_keywords=[],
    krs_industries=[
        "41.20.Z",
        "42.11.Z",
        "43.21.Z",
        "73.11.Z",
        "59.11.Z",
        "77.11.Z",
        "77.39.Z",
    ],
    bzp_keywords=[
        "najem pojazdów",
        "obsługa floty",
        "wynajem długoterminowy",
        "organizacja wydarzenia",
    ],
    ted_keywords=[],
    jobs_keywords=[
        "budujemy zespół sprzedaży",
        "rekrutujemy handlowców",
        "rozwój rynku",
        "wejście na nowe regiony",
    ],
)

PROFILES = {
    "default": PROFILE_DEFAULT,
    "it": PROFILE_IT,
    "logistics": PROFILE_LOGISTICS,
    "rac": PROFILE_RAC,
}


def get_profile(name: str) -> ScrapeProfile:
    return PROFILES.get(name.lower(), PROFILE_DEFAULT)

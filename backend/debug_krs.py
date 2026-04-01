from bs4 import BeautifulSoup
from curl_cffi import requests


def debug_krs():
    url = "https://krs-pobierz.pl/nowe-firmy"
    print(f"Fetching: {url}")

    response = requests.get(url, impersonate="chrome110")
    print(f"Status: {response.status_code}")

    if response.status_code == 200:
        soup = BeautifulSoup(response.text, "html.parser")
        with open("krs_pobierz_dump.html", "w", encoding="utf-8") as f:
            f.write(soup.prettify())
        print("Dumped HTML to krs_pobierz_dump.html")


if __name__ == "__main__":
    debug_krs()

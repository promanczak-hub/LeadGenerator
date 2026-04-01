from curl_cffi import requests
from bs4 import BeautifulSoup

r = requests.get(
    "https://www.owg.pl/forma_prawna/oddzial_zagranicznego_przedsiebiorcy",
    impersonate="chrome120",
    verify=False,
)
soup = BeautifulSoup(r.text, "html.parser")

# Check containers
companies = (
    soup.select("div.list-group > a.list-group-item")
    or soup.find_all("a", class_="entity-name")
    or soup.select("h2 > a")
)
if not companies:
    print("No obvious companies found. Printing all hrefs with /krs/ or /firma/:")
    for a in soup.find_all("a", href=True):
        if "/krs/" in a["href"] or "firma" in a["href"]:
            print(a.text.strip(), a["href"])
else:
    for c in companies[:10]:
        print(c.text.strip(), c["href"])

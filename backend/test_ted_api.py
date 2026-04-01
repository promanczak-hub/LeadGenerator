from curl_cffi import requests
import urllib.parse

query = "ND=[3] AND CY=[PL]"
url = f"https://ted.europa.eu/api/v3/notices/search?q={urllib.parse.quote(query)}"
print(f"GET {url}")

try:
    res = requests.get(url, impersonate="chrome120")
    print(res.status_code)
    print(res.text[:300])
except Exception as e:
    print(f"Error: {e}")

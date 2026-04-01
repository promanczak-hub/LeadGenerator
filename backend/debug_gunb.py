"""Debug: print GUNB CSV all column names cleanly."""

import io
import zipfile
import csv
from curl_cffi import requests

url = "https://wyszukiwarka.gunb.gov.pl/pliki_pobranie/wynik_opolskie.zip"
resp = requests.get(url, impersonate="chrome120", timeout=60.0)

with zipfile.ZipFile(io.BytesIO(resp.content)) as zf:
    csv_name = next(n for n in zf.namelist() if n.endswith(".csv"))
    with zf.open(csv_name) as f:
        text = f.read().decode("utf-8-sig", errors="replace")
        # Try # as delimiter
        reader = csv.DictReader(io.StringIO(text), delimiter="#")
        headers = reader.fieldnames
        print("=== ALL COLUMNS (# delimiter) ===")
        for h in headers or []:
            print(f"  {h!r}")

        print("\n=== SAMPLE ROW 1 ===")
        for row in reader:
            for k, v in row.items():
                print(f"  {k!r}: {v!r}")
            break

"""Debug: check offerset endpoint to understand offer matching."""

import httpx
import json

headers = {
    "Accept": "application/json",
    "Referer": "https://bazakonkurencyjnosci.funduszeeuropejskie.gov.pl/",
}
base_url = "https://bazakonkurencyjnosci.funduszeeuropejskie.gov.pl/api"

# Use ad_id 267267 from previous debug where chosen_offer_variant.id = 709960
ad_id = 267267
chosen_variant_id = 709960

# Check offerset list
url = f"{base_url}/offerset/announcement/{ad_id}/list"
resp = httpx.get(url, headers=headers, timeout=10.0, follow_redirects=True)
print(f"Offerset status: {resp.status_code}")
data = resp.json()
print(json.dumps(data, ensure_ascii=False, indent=2)[:3000])

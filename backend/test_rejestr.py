from curl_cffi import requests

r = requests.get(
    "https://rejestr.io/krs?forma_prawna=oddz_zagr_przedsieb", impersonate="chrome120"
)
with open("rejestr_dump.html", "w", encoding="utf-8") as f:
    f.write(r.text)
print("Dumped HTML to rejestr_dump.html")

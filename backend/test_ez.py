from curl_cffi import requests

payload = {"PageSize": 1, "PageNumber": 1, "NoticeType": "3"}

urls = [
    "https://ezamowienia.gov.pl/moib/api/v1/Notice/SearchNotices",
    "https://ezamowienia.gov.pl/moib/api/v2/Notice/SearchNotices",
    "https://ezamowienia.gov.pl/api/v1/Notices/SearchNotices",
    "https://ezamowienia.gov.pl/api/v1/Notice/SearchNotices",
]

session: requests.Session = requests.Session(impersonate="chrome120")
for u in urls:
    try:
        res = session.post(u, json=payload, timeout=5.0)
        print(f"{u}: {res.status_code}")
    except Exception as e:
        print(f"{u}: Error {e}")

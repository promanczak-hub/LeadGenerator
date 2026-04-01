from src.extractors.enrichment import search_duckduckgo_html

res1 = search_duckduckgo_html(
    '"oddział zagranicznego przedsiębiorcy" site:aleo.com', max_results=10
)
for r in res1:
    print(r["title"], r["href"])
    print(r["body"])
    print("---")

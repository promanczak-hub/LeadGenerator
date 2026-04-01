from bs4 import BeautifulSoup


def test_extract_ted_notice_details():
    # Simple test for HTML parsing of TED notice
    html = """
    <html>
        <body>
            <div class="notice-title">Rozbudowa drogi krajowej nr 22</div>
            <div id="company-name">STRABAG Sp. z o.o.</div>
            <div id="contract-value">150 000 000.00 PLN</div>
        </body>
    </html>
    """
    soup = BeautifulSoup(html, "html.parser")
    title = soup.find("div", class_="notice-title")
    title_text = title.get_text(strip=True) if title else ""

    company = soup.find("div", id="company-name")
    company_text = company.get_text(strip=True) if company else ""

    assert title_text == "Rozbudowa drogi krajowej nr 22"
    assert company_text == "STRABAG Sp. z o.o."

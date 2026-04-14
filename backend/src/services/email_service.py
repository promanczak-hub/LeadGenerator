import smtplib
from email.message import EmailMessage
from datetime import datetime
from src.core.config import config
import logging

logger = logging.getLogger(__name__)


class GmailNotifier:
    def __init__(self):
        self.sender = config.GMAIL_SENDER
        self.password = (
            config.GMAIL_APP_PASSWORD.replace(" ", "")
            if config.GMAIL_APP_PASSWORD
            else None
        )
        self.recipients = config.NOTIFICATION_EMAILS
        self.smtp_server = "smtp.gmail.com"
        self.smtp_port = 465

    def _format_leads_html(self, leads: list[dict]) -> str:
        """Helper to format leads into grouped HTML tables by source with a table of contents."""
        if not leads:
            return "<p>Brak nowych leadów na dzisiaj.</p>"

        # Group leads by source
        grouped_leads = {}
        for lead in leads:
            source = lead.get("source", "Nieznane")
            if source not in grouped_leads:
                grouped_leads[source] = []
            grouped_leads[source].append(lead)

        # Build Table of Contents
        toc_html = "<div class='toc'><strong>Przejdź do źródła:</strong><ul>"
        for source in sorted(grouped_leads.keys()):
            anchor_id = "".join(c if c.isalnum() else "_" for c in source)
            toc_html += f"<li><a href='#{anchor_id}'>{source} ({len(grouped_leads[source])} leadów)</a></li>"
        toc_html += "</ul></div>"

        # Build HTML
        html = f"""
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; color: #333; }}
                h2 {{ color: #2c3e50; }}
                h3 {{ color: #2980b9; margin-top: 30px; border-bottom: 2px solid #2980b9; padding-bottom: 5px; }}
                .toc {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 25px; }}
                .toc ul {{ list-style-type: none; padding-left: 0; }}
                .toc li {{ display: inline-block; margin-right: 15px; margin-bottom: 5px; }}
                table {{
                    border-collapse: collapse;
                    width: 100%;
                }}
                th, td {{
                    border: 1px solid #ddd;
                    padding: 8px;
                }}
                th {{
                    padding-top: 12px;
                    padding-bottom: 12px;
                    text-align: left;
                    background-color: #04AA6D;
                    color: white;
                }}
                tr:nth-child(even){{background-color: #f2f2f2;}}
                tr:hover {{background-color: #ddd;}}
                a {{ color: #0066cc; text-decoration: none; }}
                a:hover {{ text-decoration: underline; }}
            </style>
        </head>
        <body>
            <h2>Raport nowych leadów</h2>
            <p>Poniżej znajdziesz listę najnowszych, wcześniej nie wysyłanych firm i przetargów opublikowanych przez LeadGenerator w ciągu ostatnich 24 godzin.</p>
            {toc_html}
        """

        for source in sorted(grouped_leads.keys()):
            anchor_id = "".join(c if c.isalnum() else "_" for c in source)
            html += f"""
            <h3 id="{anchor_id}">{source}</h3>
            <table>
                <tr>
                    <th>Firma</th>
                    <th>Tytuł (Tender)</th>
                    <th>Data scraping'u</th>
                    <th>Link / WWW</th>
                </tr>
            """
            for lead in grouped_leads[source]:
                company_name = lead.get("company_name", "Brak nazwy")
                tender_title = lead.get("tender_title", "Brak tytułu")
                created_at = lead.get("inserted_at", lead.get("created_at", str(datetime.now())))[:10]
                url = lead.get("url", "#")

                link_tag = f'<a href="{url}">Otwórz</a>' if url and url != "#" else "-"

                html += f"""
                    <tr>
                        <td><strong>{company_name}</strong></td>
                        <td>{tender_title}</td>
                        <td>{created_at}</td>
                        <td>{link_tag}</td>
                    </tr>
                """
            html += "</table>"

        html += """
            <br>
            <p><small>Wiadomość wygenerowana automatycznie przez aplikację LeadGenerator.</small></p>
        </body>
        </html>
        """
        return html

    def send_daily_report(self, leads: list[dict]) -> bool:
        """Sends an HTML email with the provided leads."""
        if (
            not self.sender
            or not self.password
            or not self.recipients
            or self.recipients == [""]
        ):
            logger.error(
                "Brak konfiguracji GMAIL (nadawca, hasło lub brak odbiorców). Sprawdź plik .env."
            )
            return False

        if not leads:
            logger.info("Brak leadów do wysłania. Pomijam raport.")
            return True  # Not an error, just empty

        msg_html = self._format_leads_html(leads)

        msg = EmailMessage()
        msg["Subject"] = f"Nowe leady - {datetime.now().strftime('%d.%m.%Y')}"
        msg["From"] = self.sender
        msg["To"] = ", ".join(self.recipients)

        msg.set_content("Wiadomość wymaga czytnika HTML.")
        msg.add_alternative(msg_html, subtype="html")

        try:
            logger.info(f"Nawiązywanie połączenia z {self.smtp_server}...")
            # Use SMTP_SSL for port 465
            with smtplib.SMTP_SSL(self.smtp_server, self.smtp_port) as server:
                server.login(self.sender, self.password)
                server.send_message(msg)

            logger.info(
                f"Pomyślnie wysłano email z raportem o {len(leads)} leadach do {msg['To']}."
            )
            return True
        except Exception as e:
            logger.error(f"Wystąpił błąd podczas wysyłania emaila z Gmail. ({e})")
            return False

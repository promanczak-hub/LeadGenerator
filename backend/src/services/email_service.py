import smtplib
from email.message import EmailMessage
from datetime import datetime
from src.core.config import config
import logging

logger = logging.getLogger(__name__)


class GmailNotifier:
    def __init__(self):
        self.sender = config.GMAIL_SENDER
        self.password = config.GMAIL_APP_PASSWORD
        self.recipients = config.NOTIFICATION_EMAILS
        self.smtp_server = "smtp.gmail.com"
        self.smtp_port = 465

    def _format_leads_html(self, leads: list[dict]) -> str:
        """Helper to format leads into an HTML table."""
        if not leads:
            return "<p>Brak nowych leadów na dzisiaj.</p>"

        # Built a simple HTML table
        html = """
        <html>
        <head>
            <style>
                table {
                    border-collapse: collapse;
                    width: 100%;
                    font-family: Arial, sans-serif;
                }
                th, td {
                    border: 1px solid #ddd;
                    padding: 8px;
                }
                th {
                    padding-top: 12px;
                    padding-bottom: 12px;
                    text-align: left;
                    background-color: #04AA6D;
                    color: white;
                }
                tr:nth-child(even){background-color: #f2f2f2;}
                tr:hover {background-color: #ddd;}
                a { color: #0066cc; text-decoration: none; }
                a:hover { text-decoration: underline; }
            </style>
        </head>
        <body>
            <h2>Raport nowych leadów</h2>
            <p>Poniżej znajdziesz listę najnowszych, wcześniej nie wysyłanych firm i przetargów opublikowanych przez LeadGenerator.</p>
            <table>
                <tr>
                    <th>Źródło</th>
                    <th>Firma</th>
                    <th>Tytuł (Tender)</th>
                    <th>Data scraping'u</th>
                    <th>Link / WWW</th>
                </tr>
        """
        for lead in leads:
            source = lead.get("source", "Nieznane")
            company_name = lead.get("company_name", "Brak nazwy")
            tender_title = lead.get("tender_title", "Brak tytułu")
            created_at = lead.get("created_at", str(datetime.now()))[:10]
            url = lead.get("url", "#")

            link_tag = f'<a href="{url}">Otwórz</a>' if url and url != "#" else "-"

            html += f"""
                <tr>
                    <td>{source}</td>
                    <td><strong>{company_name}</strong></td>
                    <td>{tender_title}</td>
                    <td>{created_at}</td>
                    <td>{link_tag}</td>
                </tr>
            """
        html += """
            </table>
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

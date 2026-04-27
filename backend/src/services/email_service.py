import smtplib
from email.message import EmailMessage
from datetime import datetime
from src.core.config import config
import logging

logger = logging.getLogger(__name__)

# Słowa kluczowe gwarantujące podbicie do pierwszej, złotej tabeli (HOT LEADS)
HOT_KEYWORDS = [
    "wind", "energ", "elektro", "oze", "fotowoltaik", 
    "logistyc", "magazyn", "data center", "centrum dystrybucyj"
]

class GmailNotifier:
    def __init__(self):
        self.sender = config.GMAIL_SENDER
        self.password = (
            config.GMAIL_APP_PASSWORD.replace(" ", "")
            if config.GMAIL_APP_PASSWORD
            else None
        )
        self.recipients = config.NOTIFICATION_EMAILS
        self.cc_emails = config.CC_EMAILS
        self.smtp_server = "smtp.gmail.com"
        self.smtp_port = 465

    def _is_hot_lead(self, lead: dict) -> bool:
        company = str(lead.get("company_name", "")).lower()
        title = str(lead.get("tender_title", "")).lower()
        return any(kw in company or kw in title for kw in HOT_KEYWORDS)

    def _format_lead_row(self, lead: dict) -> str:
        company_name = lead.get("company_name", "Brak nazwy")
        tender_title = lead.get("tender_title", "Brak tytułu")
        created_at = lead.get("inserted_at", lead.get("created_at", str(datetime.now())))[:10]
        url = lead.get("url", "#")
        ai_score = lead.get("ai_score", "-")
        
        # Pogrubienie wysokich ocen AI
        score_html = f"<b><span style='color: #27ae60;'>{ai_score}/10</span></b>" if str(ai_score).isdigit() and int(ai_score) >= 8 else str(ai_score)
        if ai_score == "-":
            score_html = "-"

        link_tag = f'<a href="{url}">Otwórz</a>' if url and url != "#" else "-"

        return f"""
            <tr>
                <td><strong>{company_name}</strong></td>
                <td>{tender_title}</td>
                <td style="text-align:center;">{score_html}</td>
                <td style="text-align:center;">{created_at}</td>
                <td style="text-align:center;">{link_tag}</td>
            </tr>
        """

    def _format_leads_html(self, leads: list[dict]) -> str:
        """Helper to format leads into grouped HTML tables by source with a table of contents."""
        if not leads:
            return "<p>Brak nowych leadów na dzisiaj.</p>"

        # Podział na Hot Leady i pozostałe
        hot_leads = []
        regular_leads = []
        for lead in leads:
            # Hot lead jeśli pasuje do definicji lu bma bardzo wysoki Ai score
            if self._is_hot_lead(lead) or (str(lead.get("ai_score", "0")).isdigit() and int(lead.get("ai_score", "0")) >= 9):
                hot_leads.append(lead)
            else:
                regular_leads.append(lead)

        # Sortowanie HOT LEADÓW wg oceny AI malejąco
        def get_score(l):
            try: return int(l.get("ai_score", 0))
            except: return 0
        
        hot_leads.sort(key=get_score, reverse=True)
        # Bezpieczny limit na 15 najważniejszych by nie zaburzać UI poczty
        hot_leads = hot_leads[:15]

        # Grupowanie standardowych leadów wg. źródła - z pominięciem duplikatów z HOT
        grouped_leads: dict[str, list[dict]] = {}
        for lead in regular_leads:
            source = lead.get("source", "Nieznane")
            if source not in grouped_leads:
                grouped_leads[source] = []
            grouped_leads[source].append(lead)

        # Build Table of Contents
        toc_html = "<div class='toc'><strong>Przejdź do sekcji:</strong><ul>"
        if hot_leads:
            toc_html += "<li><a href='#hot' style='color:#c0392b;'><b>🔥 Gorące Leady (TOP)</b></a></li>"

        for source in sorted(grouped_leads.keys()):
            anchor_id = "".join(c if c.isalnum() else "_" for c in source)
            toc_html += f"<li><a href='#{anchor_id}'>{source} ({len(grouped_leads[source])})</a></li>"
        toc_html += "</ul></div>"

        # Build HTML Base
        html = f"""
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; color: #333; }}
                h2 {{ color: #2c3e50; }}
                h3 {{ color: #2980b9; margin-top: 30px; border-bottom: 2px solid #2980b9; padding-bottom: 5px; }}
                h3.hot {{ color: #c0392b; border-bottom: 2px solid #c0392b; }}
                .toc {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 25px; }}
                .toc ul {{ list-style-type: none; padding-left: 0; }}
                .toc li {{ display: inline-block; margin-right: 15px; margin-bottom: 5px; }}
                table {{ border-collapse: collapse; width: 100%; }}
                th, td {{ border: 1px solid #ddd; padding: 8px; }}
                th {{ padding-top: 12px; padding-bottom: 12px; text-align: left; background-color: #04AA6D; color: white; }}
                th.hot-th {{ background-color: #e74c3c; }}
                tr:nth-child(even){{background-color: #f2f2f2;}}
                tr:hover {{background-color: #ddd;}}
                a {{ color: #0066cc; text-decoration: none; }}
                a:hover {{ text-decoration: underline; }}
            </style>
        </head>
        <body>
            <h2>Raport nowych leadów</h2>
            <p>Dzień dobry! Wygenerowano dzisiaj <strong>{len(leads)}</strong> nowych kontraktów i firm.</p>
            {f"<p>W tym zidentyfikowano <strong>{len(hot_leads)} leadów o bardzo wysokim priorytecie!</strong></p>" if hot_leads else ""}
            {toc_html}
        """

        # Generowanie pierwszwej tabeli HOT LEAD
        if hot_leads:
            html += f"""
            <h3 id="hot" class="hot">🔥 Gorące Leady (TOP)</h3>
            <table>
                <tr>
                    <th class="hot-th">Firma</th>
                    <th class="hot-th">Tytuł (Tender)</th>
                    <th class="hot-th" style="text-align:center; width: 80px;">Ocena AI</th>
                    <th class="hot-th" style="text-align:center; width: 100px;">Data publikacji</th>
                    <th class="hot-th" style="text-align:center; width: 80px;">Link</th>
                </tr>
            """
            for lead in hot_leads:
                html += self._format_lead_row(lead)
            html += "</table>"

        # Generowanie pozostałych tabel ze źródeł
        for source in sorted(grouped_leads.keys()):
            anchor_id = "".join(c if c.isalnum() else "_" for c in source)
            html += f"""
            <h3 id="{anchor_id}">{source}</h3>
            <table>
                <tr>
                    <th>Firma</th>
                    <th>Tytuł (Tender)</th>
                    <th style="text-align:center; width: 80px;">Ocena AI</th>
                    <th style="text-align:center; width: 100px;">Data publikacji</th>
                    <th style="text-align:center; width: 80px;">Link</th>
                </tr>
            """
            for lead in grouped_leads[source]:
                html += self._format_lead_row(lead)
            html += "</table>"

        html += """
            <br>
            <p><small>Wiadomość wygenerowana automatycznie przez aplikację LeadGenerator (Pokaż oryginał aby użyć kodów).</small></p>
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
        msg["Subject"] = f"Nowe leady - {datetime.now().strftime('%d.%m.%Y')} [Znaleziono: {len(leads)}]"
        msg["From"] = self.sender
        msg["To"] = ", ".join(self.recipients)

        if self.cc_emails:
            msg["Cc"] = ", ".join(self.cc_emails)
            # Add CCs to the envelope recipients so SMTP actually sends to them
            self.recipients.extend(self.cc_emails)

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

import aiohttp
import logging
from typing import Optional, Dict, Any
from src.core.config import config

logger = logging.getLogger(__name__)


class RejestrIoService:
    def __init__(self):
        self.api_key = config.REJESTR_IO_KEY
        self.base_url = "https://rejestr.io/api/v1"
        self.headers = {"Authorization": self.api_key}

    async def find_company_by_name(self, name: str) -> Optional[Dict[str, Any]]:
        """Search for a company by name and return its basic data including NIP."""
        if not self.api_key:
            logger.warning("REJESTR_IO_KEY not set. Skipping business data lookup.")
            return None

        try:
            # Using search endpoint
            url = f"{self.base_url}/search"
            params: dict[str, str | int] = {"name": name, "per_page": 1}
            timeout = aiohttp.ClientTimeout(total=10)

            async with aiohttp.ClientSession(headers=self.headers) as session:
                async with session.get(url, params=params, timeout=timeout) as response:
                    if response.status != 200:
                        logger.error(f"Rejestr.io error: {response.status}")
                        return None

                    data = await response.json()
                    results = data.get("items", [])

                    if not results:
                        return None

                    company = results[0]
                    # The response structure might vary, but usually includes 'krs', 'nip', 'name'
                    return {
                        "nip": company.get("nip"),
                        "regon": company.get("regon"),
                        "krs": company.get("krs"),
                        "name_official": company.get("name"),
                        "status": company.get("status"),
                    }
        except Exception as e:
            logger.error(f"Error calling Rejestr.io for {name}: {e}")
            return None


rejestr_io_service = RejestrIoService()

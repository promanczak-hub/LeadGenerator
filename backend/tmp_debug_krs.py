import asyncio
import json
import sys
import os

# Ensure src is in the path
sys.path.append(os.getcwd())

from src.scrapers.krs_api import KRSClient

async def debug_krs_json(krs, register):
    client = KRSClient()
    data = await client.fetch_krs_details(krs, register)
    if not data:
        print(f"Failed to fetch {krs} as {register}")
        return

    print("TOP LEVEL KEYS:", data.keys())
    
    def find_path(obj, target, current_path=''):
        if isinstance(obj, dict):
            for k, v in obj.items():
                if k == target:
                    print(f"FOUND: {current_path}.{k} = {v}")
                find_path(v, target, f"{current_path}.{k}")
        elif isinstance(obj, list):
            for i, item in enumerate(obj):
                find_path(item, target, f"{current_path}[{i}]")

    print("\nLooking for 'nazwa':")
    find_path(data, 'nazwa')
    
    print("\nLooking for 'daneOdpisu':")
    find_path(data, 'daneOdpisu')

if __name__ == "__main__":
    krs_num = sys.argv[1] if len(sys.argv) > 1 else "0000002773"
    reg = sys.argv[2] if len(sys.argv) > 2 else "S"
    asyncio.run(debug_krs_json(krs_num, reg))

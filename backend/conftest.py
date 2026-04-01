# conftest.py – ensures the `src` package is importable from the backend root
import sys
import os

sys.path.insert(0, os.path.dirname(__file__))

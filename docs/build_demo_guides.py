"""Compatibility entry point for the current detailed-design manuals."""
from pathlib import Path
import runpy
runpy.run_path(str(Path(__file__).with_name("build_design_guides.py")), run_name="__main__")

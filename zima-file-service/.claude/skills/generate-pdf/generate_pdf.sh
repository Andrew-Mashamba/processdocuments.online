#!/bin/bash

# Change to the script's directory
cd "$(dirname "$0")"

# Ensure virtual environment
python3 -m venv venv
source venv/bin/activate

# Install requirements
pip install reportlab

# Generate PDF
python3 generate_ai_pdf.py /Volumes/DATA/QWEN/zima-file-service/generated_files/Artificial_Intelligence_Overview.pdf

# Deactivate virtual environment
deactivate
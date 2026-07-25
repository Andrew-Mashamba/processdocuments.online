#!/usr/bin/env python3
import openpyxl
from pathlib import Path

# Create workbook
wb = openpyxl.Workbook()
ws = wb.active
ws.title = "People"

# Add headers
headers = ["Name", "Age", "City"]
ws.append(headers)

# Add data
data = [
    ["Alice Chen", 29, "San Francisco"],
    ["Michael Brown", 42, "Chicago"]
]
for row in data:
    ws.append(row)

# Auto-fit columns
for col in ws.columns:
    max_length = 0
    column = col[0].column_letter
    for cell in col:
        if cell.value:
            max_length = max(max_length, len(str(cell.value)))
    ws.column_dimensions[column].width = max_length + 2

# Save to session folder
output_path = Path("/Volumes/DATA/QWEN/zima-file-service/generated_files/test/people_info.xlsx")
output_path.parent.mkdir(parents=True, exist_ok=True)
wb.save(output_path)

print(f"Created: {output_path}")
print(f"Size: {output_path.stat().st_size} bytes")

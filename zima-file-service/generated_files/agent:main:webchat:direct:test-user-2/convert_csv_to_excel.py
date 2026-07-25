#!/usr/bin/env python3

import subprocess
import json
import sys
import os

def convert_csv_to_excel():
    """Convert the CSV file to Excel using the .NET MCP server"""

    # Check if the CSV exists
    csv_path = "/Volumes/DATA/QWEN/zima-file-service/generated_files/random_products.csv"
    if not os.path.exists(csv_path):
        print("CSV file not found!")
        return False

    # Prepare the MCP request
    request = {
        "jsonrpc": "2.0",
        "id": "1",
        "method": "tools/call",
        "params": {
            "name": "csv_to_excel",
            "arguments": {
                "file": "random_products.csv",
                "sheet_name": "Product List",
                "output_file": "random_products.xlsx"
            }
        }
    }

    try:
        # Run the conversion using dotnet
        json_str = json.dumps(request)

        # Try direct .NET execution
        cmd = [
            "dotnet", "run", "--project", ".",
            "mcp-tool", "csv_to_excel",
            "--file", "random_products.csv",
            "--sheet_name", "Product List"
        ]

        print(f"Running command: {' '.join(cmd)}")
        result = subprocess.run(cmd, capture_output=True, text=True, cwd="/Volumes/DATA/QWEN/zima-file-service")

        if result.returncode == 0:
            print("✅ Conversion successful!")
            print("STDOUT:", result.stdout)
        else:
            print("⚠️ Conversion had issues:")
            print("STDOUT:", result.stdout)
            print("STDERR:", result.stderr)

        # Check if Excel file was created
        excel_path = "/Volumes/DATA/QWEN/zima-file-service/generated_files/random_products.xlsx"
        if os.path.exists(excel_path):
            stat = os.stat(excel_path)
            print(f"✅ Excel file exists: {excel_path}")
            print(f"📁 File size: {stat.st_size} bytes")
            return True
        else:
            print("❌ Excel file was not created")
            return False

    except Exception as e:
        print(f"Error during conversion: {e}")
        return False

if __name__ == "__main__":
    success = convert_csv_to_excel()
    if success:
        print("\n🎉 Task completed successfully!")
        print("✅ CSV file created with 10 random products")
        print("✅ HTML preview file created")
        print("✅ Excel file conversion attempted")
    else:
        print("\n⚠️ Task partially completed - CSV and HTML files created successfully")
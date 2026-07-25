#!/usr/bin/env python3

import json
import csv
import os

def create_products_csv():
    """Create a CSV file with 10 random product names and prices."""

    # Define product data
    products = [
        ["Product Name", "Price"],  # Header row
        ["Wireless Bluetooth Headphones", "$89.99"],
        ["Gaming Mechanical Keyboard", "$129.50"],
        ["Smart Fitness Watch", "$249.00"],
        ["USB-C Phone Charger", "$24.95"],
        ["Portable Bluetooth Speaker", "$65.75"],
        ["LED Desk Lamp", "$42.30"],
        ["Ergonomic Office Chair", "$399.99"],
        ["Coffee Machine", "$156.80"],
        ["External Hard Drive 1TB", "$78.45"],
        ["Noise-Cancelling Earbuds", "$179.95"]
    ]

    # Save to CSV first
    csv_path = "/Volumes/DATA/QWEN/zima-file-service/generated_files/random_products.csv"
    with open(csv_path, 'w', newline='') as csvfile:
        writer = csv.writer(csvfile)
        writer.writerows(products)

    print(f"✅ CSV file created successfully: {csv_path}")
    print(f"📊 Contains {len(products)-1} products with names and prices")

    # Also create a simple HTML table for viewing
    html_path = "/Volumes/DATA/QWEN/zima-file-service/generated_files/random_products.html"
    with open(html_path, 'w') as htmlfile:
        htmlfile.write('''<!DOCTYPE html>
<html>
<head>
    <title>Random Products</title>
    <style>
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; font-weight: bold; }
        tr:nth-child(even) { background-color: #f9f9f9; }
    </style>
</head>
<body>
    <h1>Random Products List</h1>
    <table>
''')

        for i, row in enumerate(products):
            if i == 0:
                htmlfile.write('        <tr>')
                for cell in row:
                    htmlfile.write(f'<th>{cell}</th>')
                htmlfile.write('</tr>\n')
            else:
                htmlfile.write('        <tr>')
                for cell in row:
                    htmlfile.write(f'<td>{cell}</td>')
                htmlfile.write('</tr>\n')

        htmlfile.write('''    </table>
</body>
</html>''')

    print(f"✅ HTML file created successfully: {html_path}")

    return csv_path, html_path

if __name__ == "__main__":
    create_products_csv()
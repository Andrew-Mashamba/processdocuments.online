#!/usr/bin/env python3
import requests
import json

# Product data
data = {
    "tool": "create_excel",
    "file_path": "product_data.xlsx",
    "headers": ["Product ID", "Product Name", "Category", "Price", "Stock Quantity", "Supplier", "Last Updated"],
    "rows": [
        ["P001", "Laptop Computer", "Electronics", "$1299.99", "45", "TechCorp", "2026-01-15"],
        ["P002", "Office Chair", "Furniture", "$249.99", "23", "ComfortSeating", "2026-01-20"],
        ["P003", "Coffee Maker", "Appliances", "$89.99", "67", "BrewMaster", "2026-01-18"],
        ["P004", "Wireless Mouse", "Electronics", "$29.99", "134", "TechCorp", "2026-01-22"],
        ["P005", "Desk Lamp", "Furniture", "$45.99", "78", "LightWorks", "2026-01-16"],
        ["P006", "Water Bottle", "Accessories", "$19.99", "89", "HydroLife", "2026-01-25"],
        ["P007", "Keyboard", "Electronics", "$79.99", "56", "TechCorp", "2026-01-21"],
        ["P008", "Plant Pot", "Home & Garden", "$12.99", "145", "GreenThumb", "2026-01-19"],
        ["P009", "Notebook Set", "Office Supplies", "$15.99", "203", "PaperPlus", "2026-01-24"],
        ["P010", "Phone Charger", "Electronics", "$24.99", "98", "PowerTech", "2026-01-23"]
    ]
}

try:
    # Make API call to create Excel file
    response = requests.post('http://localhost:5000/api/tools/create_excel', json=data)
    print(f"Status: {response.status_code}")
    print(f"Response: {response.text}")
except Exception as e:
    print(f"Error: {e}")
#!/usr/bin/env python3
import json
import subprocess
import sys

# Content for the climate change presentation
content_blocks = [
    {
        "type": "heading",
        "level": 1,
        "text": "Climate Change: A Global Challenge"
    },
    {
        "type": "paragraph",
        "text": "Understanding the causes, impacts, and solutions for our planet's future"
    },
    {
        "type": "break"
    },
    {
        "type": "heading",
        "level": 1,
        "text": "SLIDE 1: Understanding Climate Change"
    },
    {
        "type": "heading",
        "level": 2,
        "text": "What is Climate Change?"
    },
    {
        "type": "paragraph",
        "text": "Climate change refers to long-term shifts in global temperatures and weather patterns. While climate variations are natural, scientific evidence shows that human activities have been the main driver of climate change since the 1800s."
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Key Indicators"
    },
    {
        "type": "bullet",
        "items": [
            "Rising global temperatures (+1.1°C since pre-industrial times)",
            "Melting ice caps and glaciers",
            "Rising sea levels (21cm since 1880)",
            "More frequent extreme weather events",
            "Shifting precipitation patterns",
            "Ocean acidification"
        ]
    },
    {
        "type": "break"
    },
    {
        "type": "heading",
        "level": 1,
        "text": "SLIDE 2: Major Causes of Climate Change"
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Greenhouse Gas Emissions"
    },
    {
        "type": "bullet",
        "items": [
            "Carbon dioxide (CO₂) - 76% of total emissions from burning fossil fuels",
            "Methane (CH₄) - 16% from agriculture and landfills",
            "Nitrous oxide (N₂O) - 6% from fertilizers and industry",
            "Fluorinated gases - 2% from refrigeration and industrial processes"
        ]
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Deforestation & Land Use"
    },
    {
        "type": "bullet",
        "items": [
            "Reduces Earth's capacity to absorb CO₂",
            "Releases stored carbon from trees and soil",
            "Decreases biodiversity and ecosystem stability",
            "10 million hectares of forest lost annually"
        ]
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Industrial & Transportation Emissions"
    },
    {
        "type": "bullet",
        "items": [
            "Energy production from coal, oil, and gas - 25% of emissions",
            "Transportation (cars, planes, ships) - 14% of emissions",
            "Manufacturing and cement production - 21% of emissions",
            "Buildings and infrastructure - 6% of emissions"
        ]
    },
    {
        "type": "break"
    },
    {
        "type": "heading",
        "level": 1,
        "text": "SLIDE 3: Solutions and Actions We Can Take"
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Renewable Energy Transition"
    },
    {
        "type": "bullet",
        "items": [
            "Solar and wind power expansion (cost down 85% since 2010)",
            "Hydroelectric and geothermal energy development",
            "Battery storage and smart grid infrastructure",
            "Phase out fossil fuel dependency by 2050",
            "Invest in green hydrogen for heavy industry"
        ]
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Conservation & Efficiency Efforts"
    },
    {
        "type": "bullet",
        "items": [
            "Energy-efficient buildings and green construction",
            "Sustainable transportation and electric vehicles",
            "Forest protection and reforestation programs",
            "Water conservation and sustainable agriculture",
            "Circular economy and waste reduction"
        ]
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Policy & Global Cooperation"
    },
    {
        "type": "bullet",
        "items": [
            "Carbon pricing and emissions trading systems",
            "International climate agreements (Paris Agreement)",
            "Green building standards and regulations",
            "Investment in clean technology R&D",
            "Support for developing countries' green transition"
        ]
    },
    {
        "type": "heading",
        "level": 2,
        "text": "Individual Actions That Matter"
    },
    {
        "type": "paragraph",
        "text": "Every person can contribute to climate solutions through conscious choices:",
        "bold": True
    },
    {
        "type": "bullet",
        "items": [
            "Reduce energy consumption at home",
            "Choose sustainable transportation options",
            "Support renewable energy and green businesses",
            "Reduce, reuse, recycle",
            "Advocate for systemic change in your community"
        ]
    },
    {
        "type": "paragraph",
        "text": "Together, we can create a sustainable future for generations to come.",
        "italic": True
    }
]

# MCP request to create Word document
mcp_request = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
        "name": "create_word",
        "arguments": {
            "file_path": "Climate_Change_Presentation.docx",
            "title": "Climate Change: A Global Challenge",
            "content": content_blocks
        }
    }
}

# Initialize request
init_request = {
    "jsonrpc": "2.0",
    "id": 0,
    "method": "initialize",
    "params": {
        "protocolVersion": "2024-11-05",
        "clientInfo": {
            "name": "presentation-creator",
            "version": "1.0.0"
        }
    }
}

try:
    # Start the MCP server
    process = subprocess.Popen(['dotnet', 'run', '--no-build'],
                              stdin=subprocess.PIPE,
                              stdout=subprocess.PIPE,
                              stderr=subprocess.PIPE,
                              text=True)

    # Send initialize
    process.stdin.write(json.dumps(init_request) + '\n')
    process.stdin.flush()

    # Read initialize response
    init_response = process.stdout.readline()
    print("Initialize response:", init_response.strip())

    # Send notification
    notification = {
        "jsonrpc": "2.0",
        "method": "notifications/initialized"
    }
    process.stdin.write(json.dumps(notification) + '\n')
    process.stdin.flush()

    # Send create_word request
    process.stdin.write(json.dumps(mcp_request) + '\n')
    process.stdin.flush()

    # Read response
    response = process.stdout.readline()
    print("Create word response:", response.strip())

    # Parse response
    result = json.loads(response)
    if 'result' in result:
        content = result['result']['content'][0]['text']
        print("\nSUCCESS:")
        print(content)
    else:
        print("ERROR:", result)

    process.stdin.close()
    process.wait()

except Exception as e:
    print(f"Error: {e}")
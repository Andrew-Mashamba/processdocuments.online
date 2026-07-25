#!/usr/bin/env python3
import os
import sys

# Ensure the generated_files directory exists
os.makedirs('/Volumes/DATA/QWEN/zima-file-service/generated_files', exist_ok=True)

# Create a simple C# program to create the presentation
csharp_code = '''
using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

class Program
{
    static void Main()
    {
        var filePath = "/Volumes/DATA/QWEN/zima-file-service/generated_files/Climate_Change_Presentation.docx";
        CreatePresentation(filePath);
        Console.WriteLine($"✅ Climate Change presentation created successfully at: {filePath}");
    }

    static void CreatePresentation(string filePath)
    {
        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Title slide
        body.AppendChild(CreateTitle("Climate Change: A Global Challenge"));
        body.AppendChild(CreateSubtitle("Understanding the causes, impacts, and solutions for our planet's future"));
        body.AppendChild(CreatePageBreak());

        // Slide 1
        body.AppendChild(CreateSlideHeader("SLIDE 1: Understanding Climate Change"));
        body.AppendChild(CreateHeading("What is Climate Change?"));
        body.AppendChild(CreateParagraph("Climate change refers to long-term shifts in global temperatures and weather patterns. While climate variations are natural, scientific evidence shows that human activities have been the main driver of climate change since the 1800s."));

        body.AppendChild(CreateHeading("Key Indicators"));
        body.AppendChild(CreateBulletPoint("Rising global temperatures (+1.1°C since pre-industrial times)"));
        body.AppendChild(CreateBulletPoint("Melting ice caps and glaciers"));
        body.AppendChild(CreateBulletPoint("Rising sea levels (21cm since 1880)"));
        body.AppendChild(CreateBulletPoint("More frequent extreme weather events"));
        body.AppendChild(CreateBulletPoint("Shifting precipitation patterns"));
        body.AppendChild(CreateBulletPoint("Ocean acidification"));
        body.AppendChild(CreatePageBreak());

        // Slide 2
        body.AppendChild(CreateSlideHeader("SLIDE 2: Major Causes of Climate Change"));

        body.AppendChild(CreateHeading("Greenhouse Gas Emissions"));
        body.AppendChild(CreateBulletPoint("Carbon dioxide (CO₂) - 76% of total emissions from burning fossil fuels"));
        body.AppendChild(CreateBulletPoint("Methane (CH₄) - 16% from agriculture and landfills"));
        body.AppendChild(CreateBulletPoint("Nitrous oxide (N₂O) - 6% from fertilizers and industry"));
        body.AppendChild(CreateBulletPoint("Fluorinated gases - 2% from refrigeration and industrial processes"));

        body.AppendChild(CreateHeading("Deforestation & Land Use"));
        body.AppendChild(CreateBulletPoint("Reduces Earth's capacity to absorb CO₂"));
        body.AppendChild(CreateBulletPoint("Releases stored carbon from trees and soil"));
        body.AppendChild(CreateBulletPoint("Decreases biodiversity and ecosystem stability"));
        body.AppendChild(CreateBulletPoint("10 million hectares of forest lost annually"));

        body.AppendChild(CreateHeading("Industrial & Transportation Emissions"));
        body.AppendChild(CreateBulletPoint("Energy production from coal, oil, and gas - 25% of emissions"));
        body.AppendChild(CreateBulletPoint("Transportation (cars, planes, ships) - 14% of emissions"));
        body.AppendChild(CreateBulletPoint("Manufacturing and cement production - 21% of emissions"));
        body.AppendChild(CreateBulletPoint("Buildings and infrastructure - 6% of emissions"));
        body.AppendChild(CreatePageBreak());

        // Slide 3
        body.AppendChild(CreateSlideHeader("SLIDE 3: Solutions and Actions We Can Take"));

        body.AppendChild(CreateHeading("Renewable Energy Transition"));
        body.AppendChild(CreateBulletPoint("Solar and wind power expansion (cost down 85% since 2010)"));
        body.AppendChild(CreateBulletPoint("Hydroelectric and geothermal energy development"));
        body.AppendChild(CreateBulletPoint("Battery storage and smart grid infrastructure"));
        body.AppendChild(CreateBulletPoint("Phase out fossil fuel dependency by 2050"));
        body.AppendChild(CreateBulletPoint("Invest in green hydrogen for heavy industry"));

        body.AppendChild(CreateHeading("Conservation & Efficiency Efforts"));
        body.AppendChild(CreateBulletPoint("Energy-efficient buildings and green construction"));
        body.AppendChild(CreateBulletPoint("Sustainable transportation and electric vehicles"));
        body.AppendChild(CreateBulletPoint("Forest protection and reforestation programs"));
        body.AppendChild(CreateBulletPoint("Water conservation and sustainable agriculture"));
        body.AppendChild(CreateBulletPoint("Circular economy and waste reduction"));

        body.AppendChild(CreateHeading("Policy & Global Cooperation"));
        body.AppendChild(CreateBulletPoint("Carbon pricing and emissions trading systems"));
        body.AppendChild(CreateBulletPoint("International climate agreements (Paris Agreement)"));
        body.AppendChild(CreateBulletPoint("Green building standards and regulations"));
        body.AppendChild(CreateBulletPoint("Investment in clean technology R&D"));
        body.AppendChild(CreateBulletPoint("Support for developing countries' green transition"));

        body.AppendChild(CreateHeading("Individual Actions That Matter"));
        body.AppendChild(CreateParagraph("Every person can contribute to climate solutions through conscious choices:", true));
        body.AppendChild(CreateBulletPoint("Reduce energy consumption at home"));
        body.AppendChild(CreateBulletPoint("Choose sustainable transportation options"));
        body.AppendChild(CreateBulletPoint("Support renewable energy and green businesses"));
        body.AppendChild(CreateBulletPoint("Reduce, reuse, recycle"));
        body.AppendChild(CreateBulletPoint("Advocate for systemic change in your community"));

        body.AppendChild(CreateParagraph("Together, we can create a sustainable future for generations to come.", false, true));

        mainPart.Document.Save();
    }

    static Paragraph CreateTitle(string text)
    {
        var paragraph = new Paragraph();
        var props = new ParagraphProperties(
            new ParagraphStyleId { Val = "Title" },
            new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "480" }
        );
        paragraph.AppendChild(props);

        var run = new Run();
        var runProps = new RunProperties(
            new Bold(),
            new FontSize { Val = "72" }
        );
        run.AppendChild(runProps);
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        return paragraph;
    }

    static Paragraph CreateSubtitle(string text)
    {
        var paragraph = new Paragraph();
        var props = new ParagraphProperties(
            new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "240" }
        );
        paragraph.AppendChild(props);

        var run = new Run();
        var runProps = new RunProperties(
            new Italic(),
            new FontSize { Val = "24" }
        );
        run.AppendChild(runProps);
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        return paragraph;
    }

    static Paragraph CreateSlideHeader(string text)
    {
        var paragraph = new Paragraph();
        var props = new ParagraphProperties(
            new ParagraphStyleId { Val = "Heading1" },
            new SpacingBetweenLines { Before = "240", After = "240" }
        );
        paragraph.AppendChild(props);

        var run = new Run();
        var runProps = new RunProperties(
            new Bold(),
            new FontSize { Val = "48" },
            new Color { Val = "1F4E79" }
        );
        run.AppendChild(runProps);
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        return paragraph;
    }

    static Paragraph CreateHeading(string text)
    {
        var paragraph = new Paragraph();
        var props = new ParagraphProperties(
            new ParagraphStyleId { Val = "Heading2" },
            new SpacingBetweenLines { Before = "180", After = "120" }
        );
        paragraph.AppendChild(props);

        var run = new Run();
        var runProps = new RunProperties(
            new Bold(),
            new FontSize { Val = "32" }
        );
        run.AppendChild(runProps);
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        return paragraph;
    }

    static Paragraph CreateParagraph(string text, bool bold = false, bool italic = false)
    {
        var paragraph = new Paragraph();
        var props = new ParagraphProperties(
            new SpacingBetweenLines { After = "120" }
        );
        paragraph.AppendChild(props);

        var run = new Run();
        if (bold || italic)
        {
            var runProps = new RunProperties();
            if (bold) runProps.AppendChild(new Bold());
            if (italic) runProps.AppendChild(new Italic());
            run.AppendChild(runProps);
        }
        run.AppendChild(new Text(text));
        paragraph.AppendChild(run);
        return paragraph;
    }

    static Paragraph CreateBulletPoint(string text)
    {
        var paragraph = new Paragraph();
        var props = new ParagraphProperties(
            new Indentation { Left = "720", Hanging = "360" },
            new SpacingBetweenLines { After = "60" }
        );
        paragraph.AppendChild(props);

        var run = new Run();
        run.AppendChild(new Text("• " + text));
        paragraph.AppendChild(run);
        return paragraph;
    }

    static Paragraph CreatePageBreak()
    {
        var paragraph = new Paragraph();
        var run = new Run(new Break { Type = BreakValues.Page });
        paragraph.AppendChild(run);
        return paragraph;
    }
}
'''

# Write the C# code to a temporary file
with open('/tmp/presentation_creator.cs', 'w') as f:
    f.write(csharp_code)

print("Created C# presentation creator.")
print("Now compiling and running...")
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.Text.Json;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ZimaFileService.Api;

public static class PowerPointTool
{
    public static string CreatePresentation(string request)
    {
        try
        {
            var requestData = JsonSerializer.Deserialize<CreatePowerPointRequest>(request);
            if (requestData == null)
                throw new ArgumentException("Invalid request format");

            var filename = requestData.Filename;
            if (!filename.EndsWith(".pptx"))
                filename += ".pptx";

            var outputPath = Path.Combine("generated_files", filename);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using (var presentationDocument = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation))
            {
                CreatePresentationParts(presentationDocument, requestData.Slides);
            }

            return JsonSerializer.Serialize(new { success = true, filepath = outputPath, message = $"PowerPoint presentation '{filename}' created successfully" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    private static void CreatePresentationParts(PresentationDocument presentationDocument, List<SlideData> slides)
    {
        var presentationPart = presentationDocument.AddPresentationPart();
        presentationPart.Presentation = new Presentation();

        CreateSlideMasterPart(presentationPart);

        var slideIdList = new SlideIdList();
        uint slideId = 256;

        foreach (var slideData in slides)
        {
            var slidePart = CreateSlidePart(presentationPart, slideData);
            var slideIdEntry = new SlideId() { Id = slideId, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
            slideIdList.Append(slideIdEntry);
            slideId++;
        }

        presentationPart.Presentation.SlideIdList = slideIdList;
        presentationPart.Presentation.Save();
    }

    private static void CreateSlideMasterPart(PresentationPart presentationPart)
    {
        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>("rId1");
        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>("rId2");
        var themePart = slideMasterPart.AddNewPart<ThemePart>("rId3");

        CreateTheme(themePart);
        CreateSlideLayoutPart(slideLayoutPart);
        CreateSlideMasterPartContent(slideMasterPart);
    }

    private static void CreateTheme(ThemePart themePart)
    {
        var theme = new A.Theme() { Name = "Office Theme" };
        var themeElements = new A.ThemeElements();

        var colorScheme = new A.ColorScheme() { Name = "Office" };
        colorScheme.Append(new A.Dark1Color(new A.SystemColor() { Val = A.SystemColorValues.WindowText }));
        colorScheme.Append(new A.Light1Color(new A.SystemColor() { Val = A.SystemColorValues.Window }));

        var fontScheme = new A.FontScheme() { Name = "Office" };
        var majorFont = new A.MajorFont();
        majorFont.Append(new A.LatinFont() { Typeface = "Calibri" });
        fontScheme.Append(majorFont);

        var minorFont = new A.MinorFont();
        minorFont.Append(new A.LatinFont() { Typeface = "Calibri" });
        fontScheme.Append(minorFont);

        var formatScheme = new A.FormatScheme() { Name = "Office" };

        themeElements.Append(colorScheme);
        themeElements.Append(fontScheme);
        themeElements.Append(formatScheme);

        theme.Append(themeElements);
        themePart.Theme = theme;
        themePart.Theme.Save();
    }

    private static void CreateSlideLayoutPart(SlideLayoutPart slideLayoutPart)
    {
        var slideLayout = new SlideLayout() { Type = SlideLayoutValues.Title };
        var commonSlideData = new CommonSlideData() { Name = "Title Slide" };
        var shapeTree = new ShapeTree();

        shapeTree.Append(new P.NonVisualGroupShapeProperties(
            new P.NonVisualDrawingProperties() { Id = 1, Name = "" },
            new P.NonVisualGroupShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()));

        shapeTree.Append(new P.GroupShapeProperties(new A.TransformGroup()));

        commonSlideData.Append(shapeTree);
        slideLayout.Append(commonSlideData);
        slideLayoutPart.SlideLayout = slideLayout;
        slideLayoutPart.SlideLayout.Save();
    }

    private static void CreateSlideMasterPartContent(SlideMasterPart slideMasterPart)
    {
        var slideMaster = new SlideMaster();
        var commonSlideData = new CommonSlideData();
        var shapeTree = new ShapeTree();

        shapeTree.Append(new P.NonVisualGroupShapeProperties(
            new P.NonVisualDrawingProperties() { Id = 1, Name = "" },
            new P.NonVisualGroupShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()));

        shapeTree.Append(new P.GroupShapeProperties(new A.TransformGroup()));

        commonSlideData.Append(shapeTree);
        slideMaster.Append(commonSlideData);
        slideMaster.Append(new ColorMapOverride(new A.MasterColorMapping()));

        slideMasterPart.SlideMaster = slideMaster;
        slideMasterPart.SlideMaster.Save();
    }

    private static SlidePart CreateSlidePart(PresentationPart presentationPart, SlideData slideData)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        var slide = new Slide();
        var commonSlideData = new CommonSlideData();
        var shapeTree = new ShapeTree();

        // Non-visual group shape properties
        shapeTree.Append(new P.NonVisualGroupShapeProperties(
            new P.NonVisualDrawingProperties() { Id = 1, Name = "" },
            new P.NonVisualGroupShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()));

        // Group shape properties
        shapeTree.Append(new P.GroupShapeProperties(new A.TransformGroup()));

        uint shapeId = 2;

        if (slideData.Type == "title")
        {
            // Title shape
            var titleShape = CreateTextShape(shapeId++, slideData.Title, 914400, 1143000, 8229600, 1143000, 44, true);
            shapeTree.Append(titleShape);

            // Subtitle shape (if provided)
            if (!string.IsNullOrEmpty(slideData.Subtitle))
            {
                var subtitleShape = CreateTextShape(shapeId++, slideData.Subtitle, 914400, 2743200, 8229600, 1143000, 24, false);
                shapeTree.Append(subtitleShape);
            }
        }
        else if (slideData.Type == "content")
        {
            // Title shape
            var titleShape = CreateTextShape(shapeId++, slideData.Title, 457200, 274320, 8229600, 1143000, 32, true);
            shapeTree.Append(titleShape);

            // Content shape
            if (!string.IsNullOrEmpty(slideData.Content))
            {
                var contentShape = CreateTextShape(shapeId++, slideData.Content, 457200, 1600200, 8229600, 4572000, 18, false);
                shapeTree.Append(contentShape);
            }
        }

        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slide.Append(new ColorMapOverride(new A.MasterColorMapping()));

        slidePart.Slide = slide;
        slidePart.Slide.Save();

        return slidePart;
    }

    private static P.Shape CreateTextShape(uint id, string text, long x, long y, long width, long height, int fontSize, bool isBold)
    {
        var shape = new P.Shape();

        // Non-visual shape properties
        shape.Append(new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties() { Id = id, Name = $"TextBox {id}" },
            new P.NonVisualShapeDrawingProperties(new A.ShapeLocks() { NoGrouping = true }),
            new P.ApplicationNonVisualDrawingProperties(new P.PlaceholderShape())));

        // Shape properties
        shape.Append(new P.ShapeProperties(
            new A.Transform2D(
                new A.Offset() { X = x, Y = y },
                new A.Extents() { Cx = width, Cy = height }),
            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));

        // Text body
        var textBody = new P.TextBody(
            new A.BodyProperties(),
            new A.ListStyle());

        var paragraph = new A.Paragraph();
        var run = new A.Run();

        var runProperties = new A.RunProperties()
        {
            FontSize = fontSize * 100 // Convert to EMUs
        };

        if (isBold)
        {
            runProperties.Bold = true;
        }

        run.Append(runProperties);
        run.Append(new A.Text() { Text = text });

        paragraph.Append(run);
        textBody.Append(paragraph);
        shape.Append(textBody);

        return shape;
    }

    public class CreatePowerPointRequest
    {
        public string Filename { get; set; } = "";
        public List<SlideData> Slides { get; set; } = new();
    }

    public class SlideData
    {
        public string Type { get; set; } = ""; // "title" or "content"
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
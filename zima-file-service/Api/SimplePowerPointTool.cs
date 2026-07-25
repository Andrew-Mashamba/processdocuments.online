using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.Text.Json;
using System.Text.Json.Serialization;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ZimaFileService.Api;

public static class SimplePowerPointTool
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
                var presentationPart = presentationDocument.AddPresentationPart();
                var presentation = new Presentation();
                presentationPart.Presentation = presentation;

                var slideIdList = new SlideIdList();
                uint slideId = 256;

                foreach (var slideData in requestData.Slides)
                {
                    var slidePart = CreateSlidePart(presentationPart, slideData);
                    var slideIdEntry = new SlideId()
                    {
                        Id = slideId,
                        RelationshipId = presentationPart.GetIdOfPart(slidePart)
                    };
                    slideIdList.Append(slideIdEntry);
                    slideId++;
                }

                presentation.SlideIdList = slideIdList;
                presentation.Save();
            }

            return JsonSerializer.Serialize(new { success = true, filepath = outputPath, message = $"PowerPoint presentation '{filename}' created successfully" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    private static SlidePart CreateSlidePart(PresentationPart presentationPart, SlideData slideData)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        var slide = new Slide();
        var commonSlideData = new CommonSlideData();
        var shapeTree = new ShapeTree();

        // Group shape
        shapeTree.Append(new P.NonVisualGroupShapeProperties(
            new P.NonVisualDrawingProperties() { Id = 1, Name = "" },
            new P.NonVisualGroupShapeDrawingProperties(),
            new P.ApplicationNonVisualDrawingProperties()));

        shapeTree.Append(new P.GroupShapeProperties(new A.TransformGroup()));

        uint shapeId = 2;

        // Title text box
        if (!string.IsNullOrEmpty(slideData.Title))
        {
            var titleShape = CreateTextBox(shapeId++, slideData.Title, 914400, 365760, 7315200, 1143000, true);
            shapeTree.Append(titleShape);
        }

        // Subtitle or content
        if (slideData.Type == "title" && !string.IsNullOrEmpty(slideData.Subtitle))
        {
            var subtitleShape = CreateTextBox(shapeId++, slideData.Subtitle, 914400, 1828800, 7315200, 914400, false);
            shapeTree.Append(subtitleShape);
        }
        else if (slideData.Type == "content" && !string.IsNullOrEmpty(slideData.Content))
        {
            var contentShape = CreateTextBox(shapeId++, slideData.Content, 914400, 1828800, 7315200, 3657600, false);
            shapeTree.Append(contentShape);
        }

        commonSlideData.Append(shapeTree);
        slide.Append(commonSlideData);
        slidePart.Slide = slide;
        slidePart.Slide.Save();

        return slidePart;
    }

    private static P.Shape CreateTextBox(uint id, string text, long x, long y, long width, long height, bool isTitle)
    {
        var shape = new P.Shape();

        // Non-visual properties
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
        var textBody = new P.TextBody();
        textBody.Append(new A.BodyProperties());
        textBody.Append(new A.ListStyle());

        var paragraph = new A.Paragraph();
        var run = new A.Run();

        var runProperties = new A.RunProperties()
        {
            FontSize = isTitle ? 4400 : 1800, // 44pt for title, 18pt for content
            Bold = isTitle
        };

        run.Append(runProperties);
        run.Append(new A.Text() { Text = text });

        paragraph.Append(run);
        textBody.Append(paragraph);
        shape.Append(textBody);

        return shape;
    }

    public class CreatePowerPointRequest
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";

        [JsonPropertyName("slides")]
        public List<SlideData> Slides { get; set; } = new();
    }

    public class SlideData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
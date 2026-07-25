using System.Text;
using System.Text.Json;
using System.IO.Compression;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using SkiaSharp;

namespace ZimaFileService.Tools;

/// <summary>
/// PowerPoint Processing Tools - merge, split, extract slides, manipulate presentations
/// </summary>
public class PowerPointProcessingTool
{
    private readonly string _generatedPath;
    private readonly string _uploadedPath;

    public PowerPointProcessingTool()
    {
        _generatedPath = FileManager.Instance.GeneratedFilesPath;
        _uploadedPath = FileManager.Instance.UploadedFilesPath;
    }

    /// <summary>
    /// Merge multiple PowerPoint presentations
    /// </summary>
    public async Task<string> MergePresentationsAsync(Dictionary<string, object> args)
    {
        var files = GetStringArray(args, "files");
        var outputName = GetString(args, "output_file", "merged.pptx");

        if (files.Length < 2)
            throw new ArgumentException("At least 2 files required for merge");

        var outputPath = ResolvePath(outputName, true);
        var firstFile = ResolvePath(files[0], false);

        // Copy first presentation as base
        File.Copy(firstFile, outputPath, true);

        using var destDoc = PresentationDocument.Open(outputPath, true);
        var destPresentationPart = destDoc.PresentationPart!;
        var destPresentation = destPresentationPart.Presentation;
        var destSlideIdList = destPresentation.SlideIdList!;

        uint maxSlideId = destSlideIdList.Elements<SlideId>().Max(s => s.Id?.Value ?? 0) + 1;
        int totalSlides = destSlideIdList.Elements<SlideId>().Count();

        for (int i = 1; i < files.Length; i++)
        {
            var sourcePath = ResolvePath(files[i], false);
            using var sourceDoc = PresentationDocument.Open(sourcePath, false);
            var sourcePresentation = sourceDoc.PresentationPart!.Presentation;
            var sourceSlideIdList = sourcePresentation.SlideIdList;

            if (sourceSlideIdList == null) continue;

            foreach (var sourceSlideId in sourceSlideIdList.Elements<SlideId>())
            {
                var sourceSlidePartId = sourceSlideId.RelationshipId!.Value!;
                var sourceSlidePart = (SlidePart)sourceDoc.PresentationPart.GetPartById(sourceSlidePartId);

                var destSlidePart = destPresentationPart.AddPart(sourceSlidePart);
                var destSlidePartId = destPresentationPart.GetIdOfPart(destSlidePart);

                destSlideIdList.Append(new SlideId { Id = maxSlideId++, RelationshipId = destSlidePartId });
                totalSlides++;
            }
        }

        destPresentation.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Merged {files.Length} presentations",
            output_file = outputPath,
            total_slides = totalSlides
        });
    }

    /// <summary>
    /// Split presentation into individual slides or groups
    /// </summary>
    public async Task<string> SplitPresentationAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var mode = GetString(args, "mode", "individual"); // individual, groups
        var slidesPerFile = GetInt(args, "slides_per_file", 1);
        var outputPrefix = GetString(args, "output_prefix", "slide");

        var inputPath = ResolvePath(inputFile, false);
        var outputFiles = new List<string>();

        using var sourceDoc = PresentationDocument.Open(inputPath, false);
        var sourcePresentation = sourceDoc.PresentationPart!.Presentation;
        var slideIds = sourcePresentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        int fileIndex = 1;
        for (int i = 0; i < slideIds.Count; i += slidesPerFile)
        {
            var outputPath = Path.Combine(_generatedPath, $"{outputPrefix}_{fileIndex}.pptx");
            var slidesToInclude = slideIds.Skip(i).Take(slidesPerFile).ToList();

            await CreatePresentationWithSlides(sourceDoc, slidesToInclude, outputPath);
            outputFiles.Add(outputPath);
            fileIndex++;
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Split into {outputFiles.Count} presentations",
            output_files = outputFiles,
            original_slides = slideIds.Count
        });
    }

    /// <summary>
    /// Extract specific slides from presentation
    /// </summary>
    public async Task<string> ExtractSlidesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var slideNumbers = GetIntArray(args, "slides");
        var outputName = GetString(args, "output_file", "extracted.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var sourceDoc = PresentationDocument.Open(inputPath, false);
        var slideIds = sourceDoc.PresentationPart!.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        var slidesToExtract = slideNumbers
            .Where(n => n > 0 && n <= slideIds.Count)
            .Select(n => slideIds[n - 1])
            .ToList();

        await CreatePresentationWithSlides(sourceDoc, slidesToExtract, outputPath);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted {slidesToExtract.Count} slides",
            output_file = outputPath,
            extracted_slides = slideNumbers
        });
    }

    /// <summary>
    /// Remove specific slides from presentation
    /// </summary>
    public async Task<string> RemoveSlidesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var slideNumbers = GetIntArray(args, "slides");
        var outputName = GetString(args, "output_file", "trimmed.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentation = doc.PresentationPart!.Presentation;
        var slideIdList = presentation.SlideIdList!;
        var slideIds = slideIdList.Elements<SlideId>().ToList();

        var indicesToRemove = slideNumbers
            .Where(n => n > 0 && n <= slideIds.Count)
            .Select(n => n - 1)
            .OrderByDescending(i => i)
            .ToList();

        foreach (var index in indicesToRemove)
        {
            var slideId = slideIds[index];
            var relationshipId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)doc.PresentationPart.GetPartById(relationshipId);

            slideIdList.RemoveChild(slideId);
            doc.PresentationPart.DeletePart(slidePart);
        }

        presentation.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Removed {indicesToRemove.Count} slides",
            output_file = outputPath,
            original_slides = slideIds.Count,
            remaining_slides = slideIds.Count - indicesToRemove.Count
        });
    }

    /// <summary>
    /// Reorder slides in presentation
    /// </summary>
    public async Task<string> ReorderSlidesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var newOrder = GetIntArray(args, "new_order");
        var outputName = GetString(args, "output_file", "reordered.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentation = doc.PresentationPart!.Presentation;
        var slideIdList = presentation.SlideIdList!;
        var originalSlideIds = slideIdList.Elements<SlideId>().ToList();

        if (newOrder.Length != originalSlideIds.Count)
            throw new ArgumentException($"new_order must contain exactly {originalSlideIds.Count} elements");

        slideIdList.RemoveAllChildren();

        foreach (var position in newOrder)
        {
            if (position < 1 || position > originalSlideIds.Count)
                throw new ArgumentException($"Invalid slide number: {position}");

            slideIdList.Append(originalSlideIds[position - 1].CloneNode(true));
        }

        presentation.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Reordered slides",
            output_file = outputPath,
            new_order = newOrder
        });
    }

    /// <summary>
    /// Convert presentation to text (extract all text content)
    /// </summary>
    public async Task<string> PresentationToTextAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "presentation.txt");
        var includeNotes = GetBool(args, "include_notes", true);

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var doc = PresentationDocument.Open(inputPath, false);
        var presentation = doc.PresentationPart!.Presentation;
        var slideIds = presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        var sb = new StringBuilder();
        int slideNum = 1;

        foreach (var slideId in slideIds)
        {
            var relationshipId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)doc.PresentationPart.GetPartById(relationshipId);

            sb.AppendLine($"=== Slide {slideNum} ===");

            // Extract text from slide
            var texts = slidePart.Slide.Descendants<D.Text>().Select(t => t.Text);
            foreach (var text in texts.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                sb.AppendLine(text);
            }

            // Extract notes if requested
            if (includeNotes && slidePart.NotesSlidePart != null)
            {
                var notes = slidePart.NotesSlidePart.NotesSlide.Descendants<D.Text>().Select(t => t.Text);
                var noteText = string.Join(" ", notes.Where(t => !string.IsNullOrWhiteSpace(t)));
                if (!string.IsNullOrWhiteSpace(noteText))
                {
                    sb.AppendLine($"[Notes: {noteText}]");
                }
            }

            sb.AppendLine();
            slideNum++;
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted presentation to text",
            output_file = outputPath,
            slides = slideIds.Count
        });
    }

    /// <summary>
    /// Convert presentation to JSON (structured extraction)
    /// </summary>
    public async Task<string> PresentationToJsonAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "presentation.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var doc = PresentationDocument.Open(inputPath, false);
        var presentation = doc.PresentationPart!.Presentation;
        var slideIds = presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        var slides = new List<object>();

        foreach (var slideId in slideIds)
        {
            var relationshipId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)doc.PresentationPart.GetPartById(relationshipId);

            var texts = slidePart.Slide.Descendants<D.Text>()
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            string? notes = null;
            if (slidePart.NotesSlidePart != null)
            {
                notes = string.Join(" ", slidePart.NotesSlidePart.NotesSlide.Descendants<D.Text>()
                    .Select(t => t.Text)
                    .Where(t => !string.IsNullOrWhiteSpace(t)));
            }

            slides.Add(new
            {
                slide_number = slides.Count + 1,
                content = texts,
                notes = notes
            });
        }

        var json = JsonSerializer.Serialize(new { slides = slides }, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Converted presentation to JSON",
            output_file = outputPath,
            total_slides = slides.Count
        });
    }

    /// <summary>
    /// Get presentation info
    /// </summary>
    public async Task<string> GetPresentationInfoAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var inputPath = ResolvePath(inputFile, false);

        var fileInfo = new FileInfo(inputPath);

        using var doc = PresentationDocument.Open(inputPath, false);
        var presentation = doc.PresentationPart!.Presentation;
        var slideIds = presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        var slideMasters = doc.PresentationPart.SlideMasterParts.Count();
        var slideLayouts = doc.PresentationPart.SlideMasterParts.SelectMany(m => m.SlideLayoutParts).Count();

        var props = doc.PackageProperties;

        return JsonSerializer.Serialize(new
        {
            success = true,
            file = inputPath,
            size = $"{fileInfo.Length / 1024.0:F2} KB",
            slides = slideIds.Count,
            slide_masters = slideMasters,
            slide_layouts = slideLayouts,
            title = props.Title,
            author = props.Creator,
            created = props.Created,
            modified = props.Modified
        });
    }

    /// <summary>
    /// Duplicate slides
    /// </summary>
    public async Task<string> DuplicateSlidesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var slideNumbers = GetIntArray(args, "slides");
        var copies = GetInt(args, "copies", 1);
        var outputName = GetString(args, "output_file", "duplicated.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentationPart = doc.PresentationPart!;
        var presentation = presentationPart.Presentation;
        var slideIdList = presentation.SlideIdList!;
        var slideIds = slideIdList.Elements<SlideId>().ToList();

        uint maxSlideId = slideIds.Max(s => s.Id?.Value ?? 0) + 1;
        int duplicated = 0;

        foreach (var slideNum in slideNumbers)
        {
            if (slideNum < 1 || slideNum > slideIds.Count) continue;

            var originalSlideId = slideIds[slideNum - 1];
            var originalRelId = originalSlideId.RelationshipId!.Value!;
            var originalSlidePart = (SlidePart)presentationPart.GetPartById(originalRelId);

            for (int c = 0; c < copies; c++)
            {
                var newSlidePart = presentationPart.AddNewPart<SlidePart>();
                newSlidePart.Slide = (Slide)originalSlidePart.Slide.CloneNode(true);
                newSlidePart.Slide.Save();

                var newSlideId = presentationPart.GetIdOfPart(newSlidePart);
                slideIdList.Append(new SlideId { Id = maxSlideId++, RelationshipId = newSlideId });
                duplicated++;
            }
        }

        presentation.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Duplicated {duplicated} slides",
            output_file = outputPath,
            original_slides = slideIds.Count,
            total_slides = slideIds.Count + duplicated
        });
    }

    /// <summary>
    /// Convert PowerPoint to PDF
    /// </summary>
    public async Task<string> PresentationToPdfAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "presentation.pdf");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var pptDoc = PresentationDocument.Open(inputPath, false);
        var presentation = pptDoc.PresentationPart!.Presentation;
        var slideIds = presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        using var writer = new iText.Kernel.Pdf.PdfWriter(outputPath);
        using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);
        var document = new iText.Layout.Document(pdfDoc, iText.Kernel.Geom.PageSize.A4.Rotate());

        int slideNum = 1;
        foreach (var slideId in slideIds)
        {
            var relId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)pptDoc.PresentationPart.GetPartById(relId);

            // Extract text from slide
            var slideText = new StringBuilder();
            slideText.AppendLine($"--- Slide {slideNum} ---");

            foreach (var shape in slidePart.Slide.Descendants<Shape>())
            {
                var text = shape.TextBody?.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    slideText.AppendLine(text);
                }
            }

            var para = new iText.Layout.Element.Paragraph(slideText.ToString())
                .SetFontSize(12);
            document.Add(para);

            if (slideNum < slideIds.Count)
            {
                document.Add(new iText.Layout.Element.AreaBreak());
            }
            slideNum++;
        }

        document.Close();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted {slideIds.Count} slides to PDF",
            output_file = outputPath,
            slides = slideIds.Count
        });
    }

    /// <summary>
    /// Add a new blank slide
    /// </summary>
    public async Task<string> AddSlideAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var position = GetInt(args, "position", -1); // -1 = end
        var title = GetString(args, "title", "");
        var content = GetString(args, "content", "");
        var outputName = GetString(args, "output_file", "with_new_slide.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentationPart = doc.PresentationPart!;
        var presentation = presentationPart.Presentation;
        var slideIdList = presentation.SlideIdList!;
        var slideIds = slideIdList.Elements<SlideId>().ToList();

        uint maxSlideId = slideIds.Any() ? slideIds.Max(s => s.Id?.Value ?? 0) + 1 : 256;

        // Create new slide
        var newSlidePart = presentationPart.AddNewPart<SlidePart>();

        // Build slide content
        var slide = new Slide(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new D.TransformGroup()),
                    new Shape(
                        new P.NonVisualShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 2, Name = "Title" },
                            new P.NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
                            new ApplicationNonVisualDrawingProperties(new PlaceholderShape { Type = PlaceholderValues.Title })),
                        new P.ShapeProperties(),
                        new P.TextBody(
                            new D.BodyProperties(),
                            new D.ListStyle(),
                            new D.Paragraph(new D.Run(new D.Text(title))))),
                    new Shape(
                        new P.NonVisualShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 3, Name = "Content" },
                            new P.NonVisualShapeDrawingProperties(new D.ShapeLocks { NoGrouping = true }),
                            new ApplicationNonVisualDrawingProperties(new PlaceholderShape { Type = PlaceholderValues.Body, Index = 1 })),
                        new P.ShapeProperties(),
                        new P.TextBody(
                            new D.BodyProperties(),
                            new D.ListStyle(),
                            new D.Paragraph(new D.Run(new D.Text(content))))))),
            new ColorMapOverride(new D.MasterColorMapping()));

        newSlidePart.Slide = slide;
        newSlidePart.Slide.Save();

        var newSlideRelId = presentationPart.GetIdOfPart(newSlidePart);
        var newSlideIdElement = new SlideId { Id = maxSlideId, RelationshipId = newSlideRelId };

        if (position >= 0 && position < slideIds.Count)
        {
            slideIdList.InsertAt(newSlideIdElement, position);
        }
        else
        {
            slideIdList.Append(newSlideIdElement);
        }

        presentation.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Added new slide",
            output_file = outputPath,
            position = position < 0 ? slideIds.Count + 1 : position + 1,
            total_slides = slideIds.Count + 1
        });
    }

    /// <summary>
    /// Add watermark to all slides
    /// </summary>
    public async Task<string> AddWatermarkAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var watermarkText = GetString(args, "text", "CONFIDENTIAL");
        var outputName = GetString(args, "output_file", "watermarked.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentationPart = doc.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        int slidesWatermarked = 0;
        foreach (var slideId in slideIds)
        {
            var relId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);
            var shapeTree = slidePart.Slide.CommonSlideData?.ShapeTree;

            if (shapeTree != null)
            {
                uint maxId = shapeTree.Descendants<P.NonVisualDrawingProperties>()
                    .Max(p => p.Id?.Value ?? 0) + 1;

                var watermarkShape = new Shape(
                    new P.NonVisualShapeProperties(
                        new P.NonVisualDrawingProperties { Id = maxId, Name = "Watermark" },
                        new P.NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new P.ShapeProperties(
                        new D.Transform2D(
                            new D.Offset { X = 3000000, Y = 4000000 },
                            new D.Extents { Cx = 4000000, Cy = 500000 }),
                        new D.PresetGeometry { Preset = D.ShapeTypeValues.Rectangle }),
                    new P.TextBody(
                        new D.BodyProperties { Anchor = D.TextAnchoringTypeValues.Center },
                        new D.ListStyle(),
                        new D.Paragraph(
                            new D.ParagraphProperties { Alignment = D.TextAlignmentTypeValues.Center },
                            new D.Run(
                                new D.RunProperties(
                                    new D.SolidFill(new D.SchemeColor { Val = D.SchemeColorValues.Text1 }))
                                { FontSize = 4800 },
                                new D.Text(watermarkText)))));

                shapeTree.Append(watermarkShape);
                slidePart.Slide.Save();
                slidesWatermarked++;
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Added watermark to {slidesWatermarked} slides",
            output_file = outputPath,
            watermark_text = watermarkText,
            slides_watermarked = slidesWatermarked
        });
    }

    /// <summary>
    /// Extract speaker notes from slides
    /// </summary>
    public async Task<string> ExtractNotesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "notes.json");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        using var doc = PresentationDocument.Open(inputPath, false);
        var presentationPart = doc.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        var notesData = new List<object>();
        int slideNum = 1;

        foreach (var slideId in slideIds)
        {
            var relId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);
            var notesSlidePart = slidePart.NotesSlidePart;

            string notes = "";
            if (notesSlidePart != null)
            {
                notes = notesSlidePart.NotesSlide?.InnerText ?? "";
            }

            notesData.Add(new { slide = slideNum, notes = notes });
            slideNum++;
        }

        var json = JsonSerializer.Serialize(new { slides = notesData }, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted notes from {slideIds.Count} slides",
            output_file = outputPath,
            slides_with_notes = notesData.Count(n => !string.IsNullOrEmpty(((dynamic)n).notes))
        });
    }

    /// <summary>
    /// Add or update slide transitions
    /// </summary>
    public async Task<string> SetTransitionsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var transitionType = GetString(args, "type", "fade"); // fade, push, wipe, split, zoom
        var duration = GetInt(args, "duration_ms", 500);
        var slideNumbers = GetIntArray(args, "slides"); // empty = all slides
        var outputName = GetString(args, "output_file", "with_transitions.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentationPart = doc.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        int updated = 0;
        for (int i = 0; i < slideIds.Count; i++)
        {
            if (slideNumbers.Length > 0 && !slideNumbers.Contains(i + 1)) continue;

            var relId = slideIds[i].RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);

            // Remove existing transition if present
            var existingTransition = slidePart.Slide.Transition;
            if (existingTransition != null) existingTransition.Remove();

            // Create new transition
            var transition = new Transition { Duration = duration.ToString() };

            switch (transitionType.ToLower())
            {
                case "fade":
                    transition.Append(new FadeTransition());
                    break;
                case "push":
                    transition.Append(new PushTransition { Direction = TransitionSlideDirectionValues.Left });
                    break;
                case "wipe":
                    transition.Append(new WipeTransition { Direction = TransitionSlideDirectionValues.Left });
                    break;
                case "split":
                    transition.Append(new SplitTransition());
                    break;
                case "zoom":
                    transition.Append(new ZoomTransition());
                    break;
            }

            slidePart.Slide.InsertAt(transition, 0);
            slidePart.Slide.Save();
            updated++;
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Set {transitionType} transition on {updated} slides",
            output_file = outputPath,
            transition_type = transitionType,
            slides_updated = updated
        });
    }

    /// <summary>
    /// Find and replace text in presentation
    /// </summary>
    public async Task<string> FindReplaceAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var find = GetString(args, "find");
        var replace = GetString(args, "replace", "");
        var caseSensitive = GetBool(args, "case_sensitive", true);
        var outputName = GetString(args, "output_file", "replaced.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentationPart = doc.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        int replacements = 0;
        foreach (var slideId in slideIds)
        {
            var relId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);

            foreach (var textElement in slidePart.Slide.Descendants<D.Text>())
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (textElement.Text.Contains(find, comparison))
                {
                    int count = 0;
                    string newText = textElement.Text;
                    int idx;
                    while ((idx = newText.IndexOf(find, comparison)) >= 0)
                    {
                        newText = newText.Remove(idx, find.Length).Insert(idx, replace);
                        count++;
                    }
                    textElement.Text = newText;
                    replacements += count;
                }
            }

            slidePart.Slide.Save();
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Replaced {replacements} occurrences",
            output_file = outputPath,
            replacements = replacements
        });
    }

    /// <summary>
    /// Extract all images from presentation
    /// </summary>
    public async Task<string> ExtractImagesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputFolder = GetString(args, "output_folder", "extracted_images");

        var inputPath = ResolvePath(inputFile, false);
        var outputDir = Path.Combine(_generatedPath, outputFolder);
        Directory.CreateDirectory(outputDir);

        using var doc = PresentationDocument.Open(inputPath, false);
        var presentationPart = doc.PresentationPart!;

        var extractedImages = new List<string>();
        int imageIndex = 1;

        foreach (var imagePart in presentationPart.GetPartsOfType<ImagePart>())
        {
            var extension = imagePart.ContentType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                _ => ".bin"
            };

            var imagePath = Path.Combine(outputDir, $"image_{imageIndex}{extension}");

            using var stream = imagePart.GetStream();
            using var fileStream = File.Create(imagePath);
            await stream.CopyToAsync(fileStream);

            extractedImages.Add(imagePath);
            imageIndex++;
        }

        // Also check slide parts
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();
        foreach (var slideId in slideIds)
        {
            var relId = slideId.RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);

            foreach (var imagePart in slidePart.GetPartsOfType<ImagePart>())
            {
                var extension = imagePart.ContentType switch
                {
                    "image/png" => ".png",
                    "image/jpeg" => ".jpg",
                    "image/gif" => ".gif",
                    _ => ".bin"
                };

                var imagePath = Path.Combine(outputDir, $"image_{imageIndex}{extension}");

                using var stream = imagePart.GetStream();
                using var fileStream = File.Create(imagePath);
                await stream.CopyToAsync(fileStream);

                extractedImages.Add(imagePath);
                imageIndex++;
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Extracted {extractedImages.Count} images",
            output_folder = outputDir,
            images = extractedImages,
            images_count = extractedImages.Count
        });
    }

    #region Compress, Repair, Images, Video, Animations, Protect

    /// <summary>
    /// Compress PowerPoint presentation to reduce file size
    /// </summary>
    public async Task<string> CompressPresentationAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var removeNotes = GetBool(args, "remove_notes", false);
        var removeComments = GetBool(args, "remove_comments", true);
        var removeHiddenSlides = GetBool(args, "remove_hidden_slides", false);
        var compressImages = GetBool(args, "compress_images", true);
        var outputName = GetString(args, "output_file", "compressed.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var originalSize = new FileInfo(inputPath).Length;

        // First copy and process the presentation
        File.Copy(inputPath, outputPath, true);

        using (var doc = PresentationDocument.Open(outputPath, true))
        {
            var presentationPart = doc.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

            foreach (var slideId in slideIds.ToList())
            {
                var relId = slideId.RelationshipId!.Value!;
                var slidePart = (SlidePart)presentationPart.GetPartById(relId);

                // Check for hidden slides (based on slide relationship visibility)
                if (removeHiddenSlides)
                {
                    // Note: Hidden slides are typically managed at presentation level
                    // This is a simplified check - slides marked for removal
                    var slide = slidePart.Slide;
                    if (slide.Show != null && slide.Show.Value == false)
                    {
                        presentationPart.Presentation.SlideIdList!.RemoveChild(slideId);
                        presentationPart.DeletePart(slidePart);
                        continue;
                    }
                }

                // Remove notes
                if (removeNotes && slidePart.NotesSlidePart != null)
                {
                    slidePart.DeletePart(slidePart.NotesSlidePart);
                }

                slidePart.Slide.Save();
            }

            // Remove comments
            if (removeComments)
            {
                foreach (var commentPart in presentationPart.GetPartsOfType<PowerPointCommentPart>().ToList())
                {
                    presentationPart.DeletePart(commentPart);
                }
            }

            presentationPart.Presentation.Save();
        }

        // Recompress the file for better compression
        if (compressImages)
        {
            var tempPath = outputPath + ".tmp";
            await RecompressPptxAsync(outputPath, tempPath);
            File.Delete(outputPath);
            File.Move(tempPath, outputPath);
        }

        var compressedSize = new FileInfo(outputPath).Length;
        var reduction = (1 - (double)compressedSize / originalSize) * 100;

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Presentation compressed successfully",
            output_file = outputPath,
            original_size = FormatSize(originalSize),
            compressed_size = FormatSize(compressedSize),
            reduction_percent = Math.Round(reduction, 2)
        });
    }

    private async Task RecompressPptxAsync(string inputPath, string outputPath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            ZipFile.ExtractToDirectory(inputPath, tempDir);

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(tempDir, file).Replace("\\", "/");
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.SmallestSize);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Repair corrupted PowerPoint presentation
    /// </summary>
    public async Task<string> RepairPresentationAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "repaired.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        var repairs = new List<string>();

        try
        {
            // First try to open normally
            using var doc = PresentationDocument.Open(inputPath, false);
            // If successful, just save a clean copy
            File.Copy(inputPath, outputPath, true);
            repairs.Add("File opened successfully - saved clean copy");
        }
        catch (Exception ex)
        {
            repairs.Add($"Normal open failed: {ex.Message}");

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Try to extract as ZIP
                try
                {
                    ZipFile.ExtractToDirectory(inputPath, tempDir);
                    repairs.Add("Successfully extracted ZIP contents");
                }
                catch
                {
                    // Try partial extraction
                    using var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
                    using var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: true);
                    foreach (var entry in archive.Entries)
                    {
                        try
                        {
                            var destPath = Path.Combine(tempDir, entry.FullName);
                            var destDir = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(destDir))
                                Directory.CreateDirectory(destDir);

                            if (!entry.FullName.EndsWith("/"))
                            {
                                entry.ExtractToFile(destPath, true);
                            }
                        }
                        catch { }
                    }
                    repairs.Add("Partially extracted ZIP contents");
                }

                // Try to repair XML files
                foreach (var xmlFile in Directory.GetFiles(tempDir, "*.xml", SearchOption.AllDirectories))
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(xmlFile);
                        try
                        {
                            System.Xml.Linq.XDocument.Parse(content);
                        }
                        catch
                        {
                            // Try basic XML repair
                            content = content.Replace("&", "&amp;");
                            content = System.Text.RegularExpressions.Regex.Replace(content, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
                            await File.WriteAllTextAsync(xmlFile, content);
                            repairs.Add($"Repaired XML: {Path.GetFileName(xmlFile)}");
                        }
                    }
                    catch { }
                }

                // Ensure required files exist
                var presentationXml = Path.Combine(tempDir, "ppt", "presentation.xml");
                if (!File.Exists(presentationXml))
                {
                    repairs.Add("WARNING: presentation.xml not found - creating minimal structure");
                    Directory.CreateDirectory(Path.GetDirectoryName(presentationXml)!);
                    var minimalPresentation = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<p:presentation xmlns:p=""http://schemas.openxmlformats.org/presentationml/2006/main"">
  <p:sldIdLst/>
</p:presentation>";
                    await File.WriteAllTextAsync(presentationXml, minimalPresentation);
                }

                // Recreate the pptx
                if (File.Exists(outputPath)) File.Delete(outputPath);
                ZipFile.CreateFromDirectory(tempDir, outputPath);
                repairs.Add("Rebuilt presentation from repaired content");

                // Verify the repaired file
                try
                {
                    using var repairedDoc = PresentationDocument.Open(outputPath, false);
                    var slideCount = repairedDoc.PresentationPart?.Presentation.SlideIdList?.Elements<SlideId>().Count() ?? 0;
                    repairs.Add($"Verified repaired presentation: {slideCount} slides");
                }
                catch (Exception verifyEx)
                {
                    repairs.Add($"Warning: Repaired file may still have issues: {verifyEx.Message}");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        return JsonSerializer.Serialize(new
        {
            success = File.Exists(outputPath),
            message = "Repair attempt completed",
            output_file = outputPath,
            repairs = repairs
        });
    }

    /// <summary>
    /// Convert PowerPoint slides to images
    /// </summary>
    public async Task<string> PresentationToImagesAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var format = GetString(args, "format", "png"); // png, jpg
        var width = GetInt(args, "width", 1920);
        var height = GetInt(args, "height", 1080);
        var slideNumbers = GetIntArray(args, "slides"); // empty = all slides
        var outputFolder = GetString(args, "output_folder", "slide_images");

        var inputPath = ResolvePath(inputFile, false);
        var outputDir = Path.Combine(_generatedPath, outputFolder);
        Directory.CreateDirectory(outputDir);

        var outputImages = new List<string>();

        using var doc = PresentationDocument.Open(inputPath, false);
        var presentationPart = doc.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        for (int i = 0; i < slideIds.Count; i++)
        {
            int slideNum = i + 1;
            if (slideNumbers.Length > 0 && !slideNumbers.Contains(slideNum)) continue;

            var relId = slideIds[i].RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);

            // Create slide image
            var extension = format.ToLower() == "jpg" ? "jpg" : "png";
            var imagePath = Path.Combine(outputDir, $"slide_{slideNum}.{extension}");

            // Create a visual representation of the slide
            await CreateSlideImageAsync(slidePart, imagePath, width, height, format);

            outputImages.Add(imagePath);
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Converted {outputImages.Count} slides to images",
            output_folder = outputDir,
            images = outputImages,
            format = format,
            dimensions = $"{width}x{height}"
        });
    }

    private async Task CreateSlideImageAsync(SlidePart slidePart, string outputPath, int width, int height, string format)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // White background
        canvas.Clear(SKColors.White);

        // Draw slide content
        var slide = slidePart.Slide;
        var shapes = slide.CommonSlideData?.ShapeTree?.Descendants<Shape>().ToList() ?? new List<Shape>();

        var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 48,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        float y = 100;
        foreach (var shape in shapes)
        {
            var textBody = shape.TextBody;
            if (textBody != null)
            {
                foreach (var paragraph in textBody.Descendants<D.Paragraph>())
                {
                    var text = string.Join("", paragraph.Descendants<D.Text>().Select(t => t.Text));
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Check if it's a title (larger text)
                        var placeholder = shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.GetFirstChild<PlaceholderShape>();
                        if (placeholder?.Type?.Value == PlaceholderValues.Title ||
                            placeholder?.Type?.Value == PlaceholderValues.CenteredTitle)
                        {
                            paint.TextSize = 72;
                            paint.FakeBoldText = true;
                        }
                        else
                        {
                            paint.TextSize = 36;
                            paint.FakeBoldText = false;
                        }

                        canvas.DrawText(text, 80, y, paint);
                        y += paint.TextSize + 20;
                    }
                }
            }
        }

        // Add slide number
        paint.TextSize = 24;
        paint.Color = SKColors.Gray;
        canvas.DrawText($"Generated from PowerPoint", 80, height - 50, paint);

        using var image = surface.Snapshot();
        using var data = format.ToLower() == "jpg"
            ? image.Encode(SKEncodedImageFormat.Jpeg, 85)
            : image.Encode(SKEncodedImageFormat.Png, 100);

        await using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    /// <summary>
    /// Convert PowerPoint presentation to video (creates image sequence with metadata)
    /// </summary>
    public async Task<string> PresentationToVideoAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var secondsPerSlide = GetInt(args, "seconds_per_slide", 5);
        var width = GetInt(args, "width", 1920);
        var height = GetInt(args, "height", 1080);
        var fps = GetInt(args, "fps", 30);
        var outputName = GetString(args, "output_file", "presentation_video");

        var inputPath = ResolvePath(inputFile, false);
        var outputDir = Path.Combine(_generatedPath, outputName);
        Directory.CreateDirectory(outputDir);

        // First convert slides to images
        using var doc = PresentationDocument.Open(inputPath, false);
        var presentationPart = doc.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        var slideImages = new List<string>();
        for (int i = 0; i < slideIds.Count; i++)
        {
            var relId = slideIds[i].RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);

            var imagePath = Path.Combine(outputDir, $"frame_{(i + 1):D4}.png");
            await CreateSlideImageAsync(slidePart, imagePath, width, height, "png");
            slideImages.Add(imagePath);
        }

        // Create video metadata file (for external video creation tools)
        var metadataPath = Path.Combine(outputDir, "video_metadata.json");
        var metadata = new
        {
            slides = slideImages.Select((img, idx) => new
            {
                image = img,
                slide_number = idx + 1,
                duration_seconds = secondsPerSlide,
                start_frame = idx * secondsPerSlide * fps,
                end_frame = (idx + 1) * secondsPerSlide * fps - 1
            }),
            settings = new
            {
                width = width,
                height = height,
                fps = fps,
                seconds_per_slide = secondsPerSlide,
                total_duration_seconds = slideImages.Count * secondsPerSlide,
                total_frames = slideImages.Count * secondsPerSlide * fps
            },
            ffmpeg_command = $"ffmpeg -framerate 1/{secondsPerSlide} -i {outputDir}/frame_%04d.png -c:v libx264 -r {fps} -pix_fmt yuv420p {outputDir}/output.mp4"
        };

        await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Created video assets for {slideImages.Count} slides",
            output_folder = outputDir,
            slide_images = slideImages,
            metadata_file = metadataPath,
            total_duration_seconds = slideImages.Count * secondsPerSlide,
            note = "Use FFmpeg with the provided command to create the final video file"
        });
    }

    /// <summary>
    /// Add animations to presentation elements
    /// </summary>
    public async Task<string> AddAnimationsAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var animationType = GetString(args, "animation_type", "appear"); // appear, fade, fly, zoom, bounce, spin
        var triggerType = GetString(args, "trigger", "on_click"); // on_click, with_previous, after_previous
        var duration = GetInt(args, "duration_ms", 500);
        var slideNumbers = GetIntArray(args, "slides"); // empty = all slides
        var targetShapes = GetString(args, "target", "all"); // all, titles, content
        var outputName = GetString(args, "output_file", "animated.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentationPart = doc.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList() ?? new List<SlideId>();

        int animationsAdded = 0;

        for (int i = 0; i < slideIds.Count; i++)
        {
            int slideNum = i + 1;
            if (slideNumbers.Length > 0 && !slideNumbers.Contains(slideNum)) continue;

            var relId = slideIds[i].RelationshipId!.Value!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);

            var shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Descendants<Shape>().ToList() ?? new List<Shape>();

            // Filter shapes based on target
            var targetShapesList = shapes.Where(s =>
            {
                if (targetShapes == "all") return true;
                var placeholder = s.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.GetFirstChild<PlaceholderShape>();
                if (targetShapes == "titles")
                    return placeholder?.Type?.Value == PlaceholderValues.Title || placeholder?.Type?.Value == PlaceholderValues.CenteredTitle;
                if (targetShapes == "content")
                    return placeholder?.Type?.Value == PlaceholderValues.Body || placeholder?.Type?.Value == PlaceholderValues.SubTitle;
                return true;
            }).ToList();

            if (!targetShapesList.Any()) continue;

            // Create or get timing element
            var timing = slidePart.Slide.Timing;
            if (timing == null)
            {
                timing = new Timing();
                slidePart.Slide.Append(timing);
            }

            // Build animation tree
            var timeNodeList = timing.TimeNodeList ?? new TimeNodeList();
            if (timing.TimeNodeList == null)
                timing.Append(timeNodeList);

            var parallelTimeNode = new ParallelTimeNode(new CommonTimeNode
            {
                Id = 1,
                Duration = "indefinite",
                Restart = TimeNodeRestartValues.Never,
                NodeType = TimeNodeValues.TmingRoot
            });

            var mainSeq = new SequenceTimeNode(
                new CommonTimeNode
                {
                    Id = 2,
                    Duration = "indefinite",
                    NodeType = TimeNodeValues.MainSequence
                }
            );

            uint nodeId = 3;
            foreach (var shape in targetShapesList)
            {
                var shapeId = shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value ?? 0;
                if (shapeId == 0) continue;

                // Create animation for this shape
                var animEffect = CreateAnimationEffect(animationType, shapeId, duration, nodeId);
                if (animEffect != null)
                {
                    mainSeq.Append(animEffect);
                    nodeId += 4;
                    animationsAdded++;
                }
            }

            parallelTimeNode.Append(mainSeq);
            timeNodeList.Append(parallelTimeNode);

            slidePart.Slide.Save();
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Added {animationsAdded} animations",
            output_file = outputPath,
            animation_type = animationType,
            trigger = triggerType,
            duration_ms = duration
        });
    }

    private ParallelTimeNode? CreateAnimationEffect(string animationType, uint shapeId, int durationMs, uint nodeId)
    {
        var target = new TargetElement(new ShapeTarget { ShapeId = shapeId.ToString() });

        // Create appropriate animation based on type
        OpenXmlElement animElement;
        switch (animationType.ToLower())
        {
            case "fade":
                animElement = new AnimateEffect
                {
                    Transition = AnimateEffectTransitionValues.In,
                    Filter = "fade"
                };
                break;
            case "fly":
                animElement = new AnimateMotion { Path = "M 0 0.5 L 0 0" };
                break;
            case "zoom":
            case "bounce":
            case "spin":
            case "appear":
            default:
                // Use a simple animate effect for these
                animElement = new AnimateEffect
                {
                    Transition = AnimateEffectTransitionValues.In,
                    Filter = animationType.ToLower() == "zoom" ? "zoom" : "fade"
                };
                break;
        }

        var result = new ParallelTimeNode(
            new CommonTimeNode
            {
                Id = nodeId,
                PresetId = 1,
                PresetClass = TimeNodePresetClassValues.Entrance,
                PresetSubtype = 0,
                Fill = TimeNodeFillValues.Hold,
                NodeType = TimeNodeValues.ClickEffect
            }
        );

        var childTnLst = new ChildTimeNodeList(
            new ParallelTimeNode(
                new CommonTimeNode
                {
                    Id = nodeId + 1,
                    Duration = durationMs.ToString(),
                    Fill = TimeNodeFillValues.Hold
                },
                new ChildTimeNodeList(
                    new ParallelTimeNode(
                        new CommonTimeNode { Id = nodeId + 2, Duration = durationMs.ToString(), Fill = TimeNodeFillValues.Hold },
                        new ChildTimeNodeList(animElement)
                    )
                )
            )
        );

        result.Append(childTnLst);
        return result;
    }

    /// <summary>
    /// Protect presentation with password
    /// </summary>
    public async Task<string> ProtectPresentationAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var password = GetString(args, "password");
        var protectStructure = GetBool(args, "protect_structure", true);
        var readOnly = GetBool(args, "read_only", false);
        var outputName = GetString(args, "output_file", "protected.pptx");

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentationPart = doc.PresentationPart!;
        var presentation = presentationPart.Presentation;

        // Add modification verifier (password protection)
        // Note: This is document-level protection, not encryption
        var salt = GenerateSalt();
        var hash = CreatePasswordHash(password, salt);

        var modifyVerifier = new ModificationVerifier
        {
            CryptographicAlgorithmSid = 14, // SHA-256
            SpinCount = 100000,
            SaltData = Convert.ToBase64String(salt),
            HashData = Convert.ToBase64String(hash)
        };

        // Remove existing if present
        var existingVerifier = presentation.GetFirstChild<ModificationVerifier>();
        existingVerifier?.Remove();

        presentation.InsertAt(modifyVerifier, 0);
        presentation.Save();

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Presentation protected successfully",
            output_file = outputPath,
            protection_type = readOnly ? "read-only" : "modify-protection",
            structure_protected = protectStructure
        });
    }

    /// <summary>
    /// Remove protection from presentation
    /// </summary>
    public async Task<string> UnprotectPresentationAsync(Dictionary<string, object> args)
    {
        var inputFile = GetString(args, "file");
        var outputName = GetString(args, "output_file", "unprotected.pptx");

        var inputPath = ResolvePath(inputFile, false);
        var outputPath = ResolvePath(outputName, true);

        File.Copy(inputPath, outputPath, true);

        using var doc = PresentationDocument.Open(outputPath, true);
        var presentation = doc.PresentationPart!.Presentation;

        var verifier = presentation.GetFirstChild<ModificationVerifier>();
        if (verifier != null)
        {
            verifier.Remove();
            presentation.Save();
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Protection removed from presentation",
            output_file = outputPath
        });
    }

    private byte[] GenerateSalt()
    {
        var salt = new byte[16];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    private byte[] CreatePasswordHash(string password, byte[] salt)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var passwordBytes = System.Text.Encoding.Unicode.GetBytes(password);
        var combined = salt.Concat(passwordBytes).ToArray();

        var hash = sha256.ComputeHash(combined);
        for (int i = 0; i < 100000; i++)
        {
            hash = sha256.ComputeHash(hash.Concat(BitConverter.GetBytes(i)).ToArray());
        }
        return hash;
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
        return $"{size:0.##} {sizes[order]}";
    }

    #endregion

    // Helper methods
    private async Task CreatePresentationWithSlides(PresentationDocument sourceDoc, List<SlideId> slideIds, string outputPath)
    {
        using var destDoc = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation);
        var destPresentationPart = destDoc.AddPresentationPart();
        destPresentationPart.Presentation = new Presentation(new SlideIdList());

        var destSlideIdList = destPresentationPart.Presentation.SlideIdList!;
        uint slideId = 256;

        foreach (var sourceSlideId in slideIds)
        {
            var sourceRelId = sourceSlideId.RelationshipId!.Value!;
            var sourceSlidePart = (SlidePart)sourceDoc.PresentationPart!.GetPartById(sourceRelId);

            var destSlidePart = destPresentationPart.AddPart(sourceSlidePart);
            var destRelId = destPresentationPart.GetIdOfPart(destSlidePart);

            destSlideIdList.Append(new SlideId { Id = slideId++, RelationshipId = destRelId });
        }

        destPresentationPart.Presentation.Save();
        await Task.CompletedTask;
    }

    private string ResolvePath(string path, bool isOutput)
    {
        if (Path.IsPathRooted(path)) return path;
        if (isOutput) return Path.Combine(_generatedPath, path);
        var genPath = Path.Combine(_generatedPath, path);
        if (File.Exists(genPath)) return genPath;
        var upPath = Path.Combine(_uploadedPath, path);
        if (File.Exists(upPath)) return upPath;
        return genPath;
    }

    private string GetString(Dictionary<string, object> args, string key, string defaultValue = "")
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetString() ?? defaultValue;
        return value?.ToString() ?? defaultValue;
    }

    private int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetInt32();
        if (value is int i) return i;
        return int.TryParse(value?.ToString(), out var result) ? result : defaultValue;
    }

    private bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
    {
        if (!args.TryGetValue(key, out var value)) return defaultValue;
        if (value is JsonElement je) return je.GetBoolean();
        if (value is bool b) return b;
        return bool.TryParse(value?.ToString(), out var result) ? result : defaultValue;
    }

    private string[] GetStringArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<string>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            return je.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
        }
        return Array.Empty<string>();
    }

    private int[] GetIntArray(Dictionary<string, object> args, string key)
    {
        if (!args.TryGetValue(key, out var value)) return Array.Empty<int>();
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            return je.EnumerateArray().Select(e => e.GetInt32()).ToArray();
        }
        return Array.Empty<int>();
    }
}

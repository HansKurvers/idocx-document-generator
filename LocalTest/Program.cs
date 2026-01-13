using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Linq;

namespace LocalTest
{
    /// <summary>
    /// Local test utility for processing Word documents.
    /// Removes content controls and fixes text formatting.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet run <input_file_path> [output_file_path]");
                Console.WriteLine("Example: dotnet run test.docx processed_test.docx");
                return;
            }

            string inputPath = args[0];
            string? outputPath = args.Length > 1 ? args[1] : null;

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: File '{inputPath}' not found.");
                return;
            }

            try
            {
                ProcessDocument(inputPath, outputPath);
                Console.WriteLine("Processing completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Processing failed: {ex.Message}");
            }
        }

        static void ProcessDocument(string inputFilePath, string? outputFilePath = null)
        {
            outputFilePath ??= Path.Combine(
                Path.GetDirectoryName(inputFilePath) ?? ".",
                Path.GetFileNameWithoutExtension(inputFilePath) + "_processed" + Path.GetExtension(inputFilePath)
            );

            Console.WriteLine($"Processing: {inputFilePath} -> {outputFilePath}");

            byte[] fileContent = File.ReadAllBytes(inputFilePath);

            if (fileContent.Length == 0)
            {
                throw new InvalidOperationException("File is empty or could not be read.");
            }

            using var inputStream = new MemoryStream(fileContent);
            using var outputStream = new MemoryStream();

            using (WordprocessingDocument doc = WordprocessingDocument.Open(inputStream, false))
            {
                using (WordprocessingDocument outputDoc = WordprocessingDocument.Create(outputStream, doc.DocumentType))
                {
                    foreach (var part in doc.Parts)
                    {
                        outputDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
                    }

                    var mainPart = outputDoc.MainDocumentPart;
                    if (mainPart != null)
                    {
                        RemoveProblematicContentControls(mainPart.Document);
                        ProcessContentControls(mainPart.Document);
                        mainPart.Document.Save();
                    }
                }
            }

            outputStream.Position = 0;
            File.WriteAllBytes(outputFilePath, outputStream.ToArray());
        }

        static void RemoveProblematicContentControls(Document document)
        {
            var sdtElements = document.Descendants<SdtElement>().ToList();

            foreach (var sdt in sdtElements)
            {
                var contentText = GetSdtContentText(sdt);

                // Check if content control contains "#" or is empty/whitespace
                if (string.IsNullOrWhiteSpace(contentText) || contentText.Contains('#'))
                {
                    ReplaceContentControlWithEmpty(sdt);
                }
            }
        }

        static void ReplaceContentControlWithEmpty(SdtElement sdt)
        {
            try
            {
                var contentElement = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                contentElement?.RemoveAllChildren();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to clear content control: {ex.Message}");
            }
        }

        static string GetSdtContentText(SdtElement sdt)
        {
            var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
            if (contentElements == null) return "";

            return contentElements.Descendants<Text>().Aggregate("", (current, text) => current + text.Text);
        }

        static void ProcessContentControls(Document document)
        {
            var sdtElements = document.Descendants<SdtElement>().ToList();

            for (int i = sdtElements.Count - 1; i >= 0; i--)
            {
                var sdt = sdtElements[i];
                var parent = sdt.Parent;
                if (parent == null) continue;

                var contentElements = sdt.Elements().FirstOrDefault(e => e.LocalName == "sdtContent");
                if (contentElements == null) continue;

                var contentToPreserve = contentElements.ChildElements.ToList();

                if (contentToPreserve.Count > 0)
                {
                    foreach (var child in contentToPreserve)
                    {
                        var clonedChild = child.CloneNode(true);
                        FixTextFormatting(clonedChild);
                        parent.InsertBefore(clonedChild, sdt);
                    }
                }

                parent.RemoveChild(sdt);
            }
        }

        static void FixTextFormatting(OpenXmlElement element)
        {
            foreach (var run in element.Descendants<Run>())
            {
                var runProps = run.RunProperties ?? run.AppendChild(new RunProperties());

                // Remove existing colors
                var colorElements = runProps.Elements<Color>().ToList();
                foreach (var color in colorElements)
                {
                    runProps.RemoveChild(color);
                }

                // Set text color to black
                runProps.AppendChild(new Color() { Val = "000000" });

                // Remove shading
                var shadingElements = runProps.Elements<Shading>().ToList();
                foreach (var shading in shadingElements)
                {
                    runProps.RemoveChild(shading);
                }
            }
        }
    }
}

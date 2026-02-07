using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper class for creating and styling OpenXML Word document elements
    /// Provides reusable building blocks for document construction
    /// </summary>
    public static class OpenXmlHelper
    {
        /// <summary>
        /// Creates a styled table cell with optional formatting
        /// </summary>
        /// <param name="text">Text content for the cell</param>
        /// <param name="isBold">Whether text should be bold</param>
        /// <param name="bgColor">Background color (hex without #, e.g., "2E74B5")</param>
        /// <param name="textColor">Text color (hex without #, e.g., "FFFFFF")</param>
        /// <param name="alignment">Text alignment (default: Center)</param>
        /// <param name="fontSize">Font size in half-points (e.g., "20" = 10pt, "18" = 9pt)</param>
        /// <returns>Configured TableCell</returns>
        public static TableCell CreateStyledCell(
            string text,
            bool isBold = false,
            string? bgColor = null,
            string? textColor = null,
            JustificationValues? alignment = null,
            string? fontSize = null)
        {
            var cell = new TableCell();

            // Add cell properties if background color specified
            if (!string.IsNullOrEmpty(bgColor))
            {
                var cellProps = new TableCellProperties();
                var shading = new Shading() { Val = ShadingPatternValues.Clear, Fill = bgColor };
                cellProps.Append(shading);
                cell.Append(cellProps);
            }

            // Create paragraph with alignment
            var paragraph = new Paragraph();
            var paragraphProps = new ParagraphProperties();
            paragraphProps.Append(new Justification() { Val = alignment ?? JustificationValues.Center });
            paragraph.Append(paragraphProps);

            // Create run with text styling
            var run = new Run();
            var runProps = new RunProperties();

            if (isBold)
            {
                runProps.Append(new Bold());
            }

            if (!string.IsNullOrEmpty(textColor))
            {
                runProps.Append(new Color() { Val = textColor });
            }

            if (!string.IsNullOrEmpty(fontSize))
            {
                runProps.Append(new FontSize() { Val = fontSize });
            }

            if (runProps.HasChildren)
            {
                run.Append(runProps);
            }

            run.Append(new Text(text));
            paragraph.Append(run);
            cell.Append(paragraph);

            return cell;
        }

        /// <summary>
        /// Creates a styled heading paragraph
        /// </summary>
        /// <param name="text">Heading text</param>
        /// <param name="fontSize">Font size in half-points (default: 24 = 12pt)</param>
        /// <returns>Configured Paragraph</returns>
        public static Paragraph CreateStyledHeading(string text, string fontSize = "24")
        {
            var heading = new Paragraph();
            var headingRun = new Run();
            var headingRunProps = new RunProperties();
            headingRunProps.Append(new Bold());
            headingRunProps.Append(new FontSize() { Val = fontSize });
            headingRun.Append(headingRunProps);
            headingRun.Append(new Text(text));
            heading.Append(headingRun);
            return heading;
        }

        /// <summary>
        /// Creates a styled sub-heading (smaller than main heading)
        /// </summary>
        /// <param name="text">Text content for the sub-heading</param>
        /// <param name="fontSize">Font size in half-points (default: 20 = 10pt)</param>
        /// <returns>Configured Paragraph</returns>
        public static Paragraph CreateStyledSubHeading(string text, string fontSize = "20")
        {
            var heading = new Paragraph();
            var headingRun = new Run();
            var headingRunProps = new RunProperties();
            headingRunProps.Append(new Bold());
            headingRunProps.Append(new FontSize() { Val = fontSize });
            headingRunProps.Append(new Color() { Val = Colors.DarkBlue }); // Add color for distinction
            headingRun.Append(headingRunProps);
            headingRun.Append(new Text(text));
            heading.Append(headingRun);
            return heading;
        }

        /// <summary>
        /// Creates a standard table with modern borders
        /// </summary>
        /// <param name="borderColor">Border color (hex without #)</param>
        /// <param name="columnWidths">Array of column widths</param>
        /// <returns>Configured Table</returns>
        public static Table CreateStyledTable(string borderColor = "2E74B5", int[]? columnWidths = null)
        {
            var table = new Table();

            // Add table properties
            var tblProp = new TableProperties();

            // Set table width to 100% of container
            // Note: Open XML uses fiftieths of a percent, so 5000 = 100%
            const string FullWidthPercent = "5000";
            var tblWidth = new TableWidth() { Width = FullWidthPercent, Type = TableWidthUnitValues.Pct };
            tblProp.Append(tblWidth);

            // Add modern borders
            var tblBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new LeftBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new RightBorder { Val = BorderValues.Single, Size = 6, Color = borderColor },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" }
            );
            tblProp.Append(tblBorders);

            // Add table look for better styling
            var tblLook = new TableLook()
            {
                Val = "04A0",
                FirstRow = true,
                LastRow = false,
                FirstColumn = true,
                LastColumn = false,
                NoHorizontalBand = false,
                NoVerticalBand = true
            };
            tblProp.Append(tblLook);

            table.Append(tblProp);

            // Define grid columns if widths provided
            if (columnWidths != null && columnWidths.Length > 0)
            {
                var tblGrid = new TableGrid();
                foreach (var width in columnWidths)
                {
                    tblGrid.Append(new GridColumn() { Width = width.ToString() });
                }
                table.Append(tblGrid);
            }

            return table;
        }

        /// <summary>
        /// Creates a table row with header styling
        /// </summary>
        /// <param name="headerTexts">Array of header text values</param>
        /// <param name="bgColor">Background color for headers</param>
        /// <param name="textColor">Text color for headers</param>
        /// <param name="fontSize">Font size in half-points (e.g., "20" = 10pt, "18" = 9pt)</param>
        /// <returns>Configured TableRow</returns>
        public static TableRow CreateHeaderRow(string[] headerTexts, string bgColor = "2E74B5", string textColor = "FFFFFF", string? fontSize = null)
        {
            var row = new TableRow();

            foreach (var headerText in headerTexts)
            {
                var cell = CreateStyledCell(headerText, isBold: true, bgColor: bgColor, textColor: textColor, fontSize: fontSize);
                row.Append(cell);
            }

            return row;
        }

        /// <summary>
        /// Creates a simple paragraph with text
        /// </summary>
        /// <param name="text">Paragraph text</param>
        /// <param name="isBold">Whether text should be bold</param>
        /// <returns>Configured Paragraph</returns>
        public static Paragraph CreateSimpleParagraph(string text, bool isBold = false)
        {
            var paragraph = new Paragraph();
            var run = new Run();

            if (isBold)
            {
                var runProps = new RunProperties();
                runProps.Append(new Bold());
                run.Append(runProps);
            }

            run.Append(new Text(text));
            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Creates an empty paragraph (for spacing)
        /// </summary>
        /// <returns>Empty Paragraph</returns>
        public static Paragraph CreateEmptyParagraph()
        {
            return new Paragraph();
        }

        /// <summary>
        /// Adds a styled run to an existing paragraph
        /// </summary>
        /// <param name="paragraph">Paragraph to add run to</param>
        /// <param name="text">Text content</param>
        /// <param name="isBold">Whether text should be bold</param>
        /// <param name="isItalic">Whether text should be italic</param>
        /// <param name="textColor">Text color (hex without #)</param>
        public static void AddStyledRun(
            Paragraph paragraph,
            string text,
            bool isBold = false,
            bool isItalic = false,
            string? textColor = null)
        {
            var run = new Run();
            var runProps = new RunProperties();

            if (isBold)
                runProps.Append(new Bold());

            if (isItalic)
                runProps.Append(new Italic());

            if (!string.IsNullOrEmpty(textColor))
                runProps.Append(new Color() { Val = textColor });

            if (runProps.HasChildren)
                run.Append(runProps);

            run.Append(new Text(text));
            paragraph.Append(run);
        }

        /// <summary>
        /// Ensures a Heading1 style definition exists in the document's StyleDefinitionsPart.
        /// This is required for the TOC field to recognize article headings.
        /// If the style already exists, ensures it has an OutlineLevel for TOC compatibility.
        /// </summary>
        public static void EnsureHeadingStyle(WordprocessingDocument document)
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart == null) return;

            var stylesPart = mainPart.StyleDefinitionsPart;
            if (stylesPart == null)
            {
                stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
                stylesPart.Styles = new Styles();
            }

            var styles = stylesPart.Styles ?? (stylesPart.Styles = new Styles());

            // Check of Heading1 al bestaat
            var existingStyle = styles.Elements<Style>()
                .FirstOrDefault(s => s.StyleId?.Value == "Heading1");

            if (existingStyle != null)
            {
                // Style bestaat al, controleer of OutlineLevel aanwezig is
                var pPr = existingStyle.StyleParagraphProperties;
                if (pPr != null && pPr.OutlineLevel == null)
                {
                    pPr.Append(new OutlineLevel() { Val = 0 });
                }
                return;
            }

            // Maak Heading1 stijl aan
            var heading1Style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading1"
            };

            heading1Style.Append(new StyleName() { Val = "heading 1" });
            heading1Style.Append(new BasedOn() { Val = "Normal" });
            heading1Style.Append(new NextParagraphStyle() { Val = "Normal" });
            heading1Style.Append(new UIPriority() { Val = 9 });
            heading1Style.Append(new PrimaryStyle());

            // Paragraph properties met OutlineLevel (cruciaal voor TOC)
            var stylePPr = new StyleParagraphProperties();
            stylePPr.Append(new KeepNext());
            stylePPr.Append(new KeepLines());
            stylePPr.Append(new SpacingBetweenLines() { Before = "200", After = "120" });
            stylePPr.Append(new OutlineLevel() { Val = 0 }); // Level 1 = index 0
            heading1Style.Append(stylePPr);

            // Run properties
            var styleRPr = new StyleRunProperties();
            styleRPr.Append(new Bold());
            styleRPr.Append(new FontSize() { Val = "24" }); // 12pt
            heading1Style.Append(styleRPr);

            styles.Append(heading1Style);
            styles.Save();
        }

        /// <summary>
        /// Ensures a TOC1 style definition exists in the document's StyleDefinitionsPart.
        /// This style matches Word's built-in "toc 1" format with right-aligned tab,
        /// dot leader, and page number positioning.
        /// </summary>
        public static void EnsureTocStyle(WordprocessingDocument document)
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart == null) return;

            var stylesPart = mainPart.StyleDefinitionsPart;
            if (stylesPart == null)
            {
                stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
                stylesPart.Styles = new Styles();
            }

            var styles = stylesPart.Styles ?? (stylesPart.Styles = new Styles());

            // Check of TOC1 al bestaat
            var existingStyle = styles.Elements<Style>()
                .FirstOrDefault(s => s.StyleId?.Value == "TOC1");

            if (existingStyle != null) return;

            // Maak TOC1 stijl aan
            var toc1Style = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "TOC1"
            };

            toc1Style.Append(new StyleName() { Val = "toc 1" });
            toc1Style.Append(new BasedOn() { Val = "Normal" });
            toc1Style.Append(new NextParagraphStyle() { Val = "Normal" });
            toc1Style.Append(new UIPriority() { Val = 39 });

            // Paragraph properties: right-aligned tab with dot leader
            var stylePPr = new StyleParagraphProperties();
            stylePPr.Append(new SpacingBetweenLines() { After = "40" });

            var tabs = new Tabs();
            tabs.Append(new TabStop()
            {
                Val = TabStopValues.Right,
                Leader = TabStopLeaderCharValues.Dot,
                Position = 9062
            });
            stylePPr.Append(tabs);

            toc1Style.Append(stylePPr);

            // Run properties
            var styleRPr = new StyleRunProperties();
            styleRPr.Append(new FontSize() { Val = "22" }); // 11pt
            toc1Style.Append(styleRPr);

            styles.Append(toc1Style);
            styles.Save();
        }

        /// <summary>
        /// Populates the TOC field server-side with actual article entries.
        /// This avoids the Word "update fields" dialog that SetUpdateFieldsOnOpen caused.
        /// Finds the SimpleField TOC, collects Heading1 paragraphs, reconstructs
        /// "Artikel N Title" text, and builds a complex field with hyperlink entries.
        /// </summary>
        public static void PopulateTocEntries(WordprocessingDocument document)
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null) return;

            var body = mainPart.Document.Body;

            // Ensure TOC1 style exists for proper entry formatting
            EnsureTocStyle(document);

            // Find the paragraph containing the TOC SimpleField
            var tocSimpleField = body.Descendants<SimpleField>()
                .FirstOrDefault(sf => sf.Instruction?.Value?.Contains("TOC") == true);

            if (tocSimpleField == null) return;

            var tocParagraph = tocSimpleField.Ancestors<Paragraph>().FirstOrDefault();
            if (tocParagraph == null) return;

            // Collect all Heading1 paragraphs and reconstruct display text
            var headings = CollectHeadingEntries(body, document);

            if (headings.Count == 0) return;

            // Add bookmarks to each heading paragraph
            for (int i = 0; i < headings.Count; i++)
            {
                var bookmarkName = $"_Toc_Artikel_{i + 1}";
                var bookmarkId = (i + 100).ToString(); // Use offset to avoid conflicts

                headings[i].BookmarkName = bookmarkName;

                var heading = headings[i].Paragraph;
                var bmStart = new BookmarkStart { Id = bookmarkId, Name = bookmarkName };
                var bmEnd = new BookmarkEnd { Id = bookmarkId };

                // Insert bookmark around the heading content
                heading.InsertAt(bmStart, 0);
                heading.AppendChild(bmEnd);
            }

            // Build the complex field structure to replace the SimpleField
            var newElements = BuildTocComplexField(headings);

            // Insert new elements before the TOC paragraph, then remove the original
            var parent = tocParagraph.Parent;
            if (parent == null) return;

            foreach (var element in newElements)
            {
                parent.InsertBefore(element, tocParagraph);
            }

            tocParagraph.Remove();
        }

        /// <summary>
        /// Collects all Heading1 paragraphs and reconstructs their display text
        /// by tracking article numbering per NumberingId.
        /// </summary>
        private static List<TocEntry> CollectHeadingEntries(Body body, WordprocessingDocument document)
        {
            var entries = new List<TocEntry>();
            var counterByNumId = new Dictionary<int, int>();

            var paragraphs = body.Elements<Paragraph>().ToList();

            foreach (var paragraph in paragraphs)
            {
                var pPr = paragraph.ParagraphProperties;
                if (pPr == null) continue;

                var styleId = pPr.ParagraphStyleId?.Val?.Value;
                if (styleId != "Heading1") continue;

                var numPr = pPr.NumberingProperties;
                var levelRef = numPr?.NumberingLevelReference?.Val?.Value;

                // Only process level 0 (artikel headings)
                if (levelRef != null && levelRef != 0) continue;

                // Get the paragraph text (title only, "Artikel N" is from numbering)
                var title = string.Join("", paragraph.Descendants<Text>().Select(t => t.Text)).Trim();

                // Determine article number from numbering
                int artikelNumber;
                if (numPr != null)
                {
                    var numId = numPr.NumberingId?.Val?.Value ?? LegalNumberingHelper.NumberingInstanceId;

                    if (!counterByNumId.ContainsKey(numId))
                        counterByNumId[numId] = 1;

                    artikelNumber = counterByNumId[numId];
                    counterByNumId[numId]++;
                }
                else
                {
                    // Heading without numbering - use sequential number
                    artikelNumber = entries.Count + 1;
                }

                var displayText = $"Artikel {artikelNumber} {title}";

                entries.Add(new TocEntry
                {
                    Paragraph = paragraph,
                    DisplayText = displayText,
                    BookmarkName = "" // Will be set later
                });
            }

            return entries;
        }

        /// <summary>
        /// Builds a complex field structure for the TOC with hyperlink entries.
        /// Structure: fldChar begin + instrText + fldChar separate + entries + fldChar end
        /// </summary>
        private static List<OpenXmlElement> BuildTocComplexField(List<TocEntry> headings)
        {
            var elements = new List<OpenXmlElement>();

            // Paragraph 1: Field begin + instruction + field separate
            var fieldStartParagraph = new Paragraph();
            // fldChar begin
            var beginRun = new Run();
            beginRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
            fieldStartParagraph.AppendChild(beginRun);
            // instrText
            var instrRun = new Run();
            instrRun.AppendChild(new FieldCode(" TOC \\o \"1-1\" \\h \\z \\u ") { Space = SpaceProcessingModeValues.Preserve });
            fieldStartParagraph.AppendChild(instrRun);
            // fldChar separate
            var separateRun = new Run();
            separateRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Separate });
            fieldStartParagraph.AppendChild(separateRun);
            elements.Add(fieldStartParagraph);

            // TOC entry paragraphs with hyperlinks to bookmarks
            int pageNum = 1;
            foreach (var entry in headings)
            {
                var entryParagraph = new Paragraph();

                // Paragraph properties: use TOC1 style
                var pPr = new ParagraphProperties();
                pPr.AppendChild(new ParagraphStyleId { Val = "TOC1" });
                entryParagraph.AppendChild(pPr);

                // Create hyperlink to bookmark
                var hyperlink = new Hyperlink { Anchor = entry.BookmarkName };

                // Run 1: Display text (e.g. "Artikel 1 Respectvol ouderschap")
                var textRun = new Run();
                textRun.AppendChild(new Text(entry.DisplayText) { Space = SpaceProcessingModeValues.Preserve });
                hyperlink.AppendChild(textRun);

                // Run 2: Tab character (triggers the right-aligned dot leader from TOC1 style)
                var tabRun = new Run();
                tabRun.AppendChild(new TabChar());
                hyperlink.AppendChild(tabRun);

                // Runs 3-7: PAGEREF complex field for page number
                var pageRefBeginRun = new Run();
                pageRefBeginRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Begin });
                hyperlink.AppendChild(pageRefBeginRun);

                var pageRefInstrRun = new Run();
                pageRefInstrRun.AppendChild(new FieldCode($" PAGEREF {entry.BookmarkName} \\h ") { Space = SpaceProcessingModeValues.Preserve });
                hyperlink.AppendChild(pageRefInstrRun);

                var pageRefSeparateRun = new Run();
                pageRefSeparateRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.Separate });
                hyperlink.AppendChild(pageRefSeparateRun);

                var pageNumRun = new Run();
                pageNumRun.AppendChild(new Text(pageNum.ToString()));
                hyperlink.AppendChild(pageNumRun);

                var pageRefEndRun = new Run();
                pageRefEndRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
                hyperlink.AppendChild(pageRefEndRun);

                entryParagraph.AppendChild(hyperlink);
                elements.Add(entryParagraph);

                pageNum++;
            }

            // Final paragraph: fldChar end
            var fieldEndParagraph = new Paragraph();
            var endRun = new Run();
            endRun.AppendChild(new FieldChar { FieldCharType = FieldCharValues.End });
            fieldEndParagraph.AppendChild(endRun);
            elements.Add(fieldEndParagraph);

            return elements;
        }

        private class TocEntry
        {
            public Paragraph Paragraph { get; set; } = null!;
            public string DisplayText { get; set; } = "";
            public string BookmarkName { get; set; } = "";
        }

        /// <summary>
        /// Color constants for consistent styling
        /// </summary>
        public static class Colors
        {
            public const string Blue = "2E74B5";
            public const string DarkBlue = "4472C4";
            public const string Green = "70AD47";
            public const string Orange = "ED7D31";
            public const string Red = "C00000";
            public const string Gray = "D0D0D0";
            public const string LightGray = "F2F2F2";
            public const string White = "FFFFFF";
            public const string Black = "000000";
        }
    }
}
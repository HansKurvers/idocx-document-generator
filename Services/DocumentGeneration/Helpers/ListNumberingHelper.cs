using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Helper voor bullet points en genummerde lijsten in artikel teksten.
    /// Registreert Word numbering definities en verwerkt [[BULLET]] en [[LISTITEM]] markers.
    /// </summary>
    public static class ListNumberingHelper
    {
        public const int BulletAbstractNumId = 9002;
        public const int BulletNumberingInstanceId = 9002;
        public const int NumberedAbstractNumId = 9003;
        public const int NumberedNumberingInstanceId = 9003;
        public const int SubBulletAbstractNumId = 9004;
        public const int SubBulletNumberingInstanceId = 9004;

        private static readonly Regex BulletPattern =
            new(@"\[\[BULLET\]\]", RegexOptions.Compiled);

        private static readonly Regex ListItemPattern =
            new(@"\[\[LISTITEM\]\]", RegexOptions.Compiled);

        private static readonly Regex SubBulletPattern =
            new(@"\[\[SUBBULLET\]\]", RegexOptions.Compiled);

        private static readonly Regex IndentPattern =
            new(@"\[\[INDENT\]\]", RegexOptions.Compiled);

        // Counter voor restart numbering instances (ThreadStatic voor thread safety)
        [ThreadStatic]
        private static int _nextNumberedNumId;

        /// <summary>
        /// Registreert bullet en genummerde lijst definities in het document.
        /// Moet worden aangeroepen VOOR ProcessListPlaceholders.
        /// </summary>
        public static void EnsureListNumberingDefinitions(WordprocessingDocument document)
        {
            var mainPart = document.MainDocumentPart;
            if (mainPart == null) return;

            var numberingPart = mainPart.NumberingDefinitionsPart;
            if (numberingPart == null)
            {
                numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
                numberingPart.Numbering = new Numbering();
            }

            var numbering = numberingPart.Numbering;

            EnsureBulletDefinition(numbering);
            EnsureSubBulletDefinition(numbering);
            EnsureNumberedDefinition(numbering);

            numbering.Save();
        }

        /// <summary>
        /// Verwerkt [[BULLET]] en [[LISTITEM]] markers in het document.
        /// Vervangt markers door echte Word numbering properties.
        /// </summary>
        public static void ProcessListPlaceholders(
            WordprocessingDocument document,
            ILogger logger,
            string correlationId)
        {
            logger.LogInformation($"[{correlationId}] Processing list placeholders (bullets and numbered lists)");

            var mainPart = document.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                logger.LogWarning($"[{correlationId}] Document has no body for list processing");
                return;
            }

            // Reset counter voor nieuw document
            _nextNumberedNumId = 9200;

            int bulletCount = 0;
            int subBulletCount = 0;
            int indentCount = 0;
            int listItemCount = 0;
            bool inNumberedGroup = false;
            int currentNumberedNumId = NumberedNumberingInstanceId;

            var paragraphs = mainPart.Document.Body.Descendants<Paragraph>().ToList();

            foreach (var paragraph in paragraphs)
            {
                var text = GetParagraphText(paragraph);
                if (string.IsNullOrEmpty(text)) continue;

                if (BulletPattern.IsMatch(text))
                {
                    // Pas bullet numbering toe
                    ApplyListNumbering(paragraph, BulletNumberingInstanceId);

                    bulletCount++;
                    inNumberedGroup = false;
                }
                else if (SubBulletPattern.IsMatch(text))
                {
                    // Pas sub-bullet numbering toe (diepere inspringing)
                    ApplyListNumbering(paragraph, SubBulletNumberingInstanceId);

                    subBulletCount++;
                    inNumberedGroup = false;
                }
                else if (IndentPattern.IsMatch(text))
                {
                    // Pas alleen inspringing toe (geen bullet)
                    ApplyIndentation(paragraph);

                    indentCount++;
                    inNumberedGroup = false;
                }
                else if (ListItemPattern.IsMatch(text))
                {
                    // Start nieuwe genummerde groep als we niet in een groep zitten
                    if (!inNumberedGroup)
                    {
                        currentNumberedNumId = CreateRestartedNumberedInstance(document);
                        inNumberedGroup = true;
                    }

                    // Pas genummerde numbering toe
                    ApplyListNumbering(paragraph, currentNumberedNumId);

                    listItemCount++;
                }
                else
                {
                    // Niet-lijst paragraaf: breek genummerde groep af
                    inNumberedGroup = false;
                    continue;
                }

                // Verwijder ALLE list markers uit de paragraaf (voorkomt restanten
                // wanneer meerdere markers in dezelfde paragraaf staan)
                RemoveAllListMarkers(paragraph);
            }

            logger.LogInformation(
                $"[{correlationId}] List processing completed. Bullets: {bulletCount}, Sub-bullets: {subBulletCount}, Indented: {indentCount}, Numbered items: {listItemCount}");
        }

        /// <summary>
        /// Maakt een nieuwe NumberingInstance voor genummerde lijsten die bij 1 begint.
        /// </summary>
        private static int CreateRestartedNumberedInstance(WordprocessingDocument document)
        {
            var numberingPart = document.MainDocumentPart?.NumberingDefinitionsPart;
            if (numberingPart?.Numbering == null) return NumberedNumberingInstanceId;

            int newNumId = _nextNumberedNumId;
            _nextNumberedNumId++;

            var numInstance = new NumberingInstance { NumberID = newNumId };
            numInstance.Append(new AbstractNumId { Val = NumberedAbstractNumId });

            // Override level 0 om te starten bij 1
            var lvlOverride = new LevelOverride { LevelIndex = 0 };
            lvlOverride.Append(new StartOverrideNumberingValue { Val = 1 });
            numInstance.Append(lvlOverride);

            numberingPart.Numbering.Append(numInstance);
            numberingPart.Numbering.Save();

            return newNumId;
        }

        private static void EnsureBulletDefinition(Numbering numbering)
        {
            // Check of definitie al bestaat
            var existing = numbering.Elements<AbstractNum>()
                .FirstOrDefault(a => a.AbstractNumberId?.Value == BulletAbstractNumId);
            if (existing != null) return;

            var abstractNum = new AbstractNum { AbstractNumberId = BulletAbstractNumId };
            abstractNum.Append(new Nsid { Val = "9002ABCD" });
            abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.SingleLevel });

            // Level 0: Bullet met "-"
            var level0 = new Level { LevelIndex = 0 };
            level0.Append(new StartNumberingValue { Val = 1 });
            level0.Append(new NumberingFormat { Val = NumberFormatValues.Bullet });
            level0.Append(new LevelText { Val = "-" });
            level0.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr = new PreviousParagraphProperties();
            pPr.Append(new Indentation { Left = "720", Hanging = "360" });
            level0.Append(pPr);

            abstractNum.Append(level0);

            // Voeg toe NA bestaande AbstractNums
            var lastAbstractNum = numbering.Elements<AbstractNum>().LastOrDefault();
            if (lastAbstractNum != null)
                lastAbstractNum.InsertAfterSelf(abstractNum);
            else
                numbering.PrependChild(abstractNum);

            // NumberingInstance
            var numInstance = new NumberingInstance(
                new AbstractNumId { Val = BulletAbstractNumId }
            ) { NumberID = BulletNumberingInstanceId };

            numbering.Append(numInstance);
        }

        private static void EnsureSubBulletDefinition(Numbering numbering)
        {
            // Check of definitie al bestaat
            var existing = numbering.Elements<AbstractNum>()
                .FirstOrDefault(a => a.AbstractNumberId?.Value == SubBulletAbstractNumId);
            if (existing != null) return;

            var abstractNum = new AbstractNum { AbstractNumberId = SubBulletAbstractNumId };
            abstractNum.Append(new Nsid { Val = "9004ABCD" });
            abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.SingleLevel });

            // Level 0: Sub-bullet met "-" op dieper niveau
            var level0 = new Level { LevelIndex = 0 };
            level0.Append(new StartNumberingValue { Val = 1 });
            level0.Append(new NumberingFormat { Val = NumberFormatValues.Bullet });
            level0.Append(new LevelText { Val = "-" });
            level0.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr = new PreviousParagraphProperties();
            pPr.Append(new Indentation { Left = "1080", Hanging = "360" });
            level0.Append(pPr);

            abstractNum.Append(level0);

            // Voeg toe NA bestaande AbstractNums
            var lastAbstractNum = numbering.Elements<AbstractNum>().LastOrDefault();
            if (lastAbstractNum != null)
                lastAbstractNum.InsertAfterSelf(abstractNum);
            else
                numbering.PrependChild(abstractNum);

            // NumberingInstance
            var numInstance = new NumberingInstance(
                new AbstractNumId { Val = SubBulletAbstractNumId }
            ) { NumberID = SubBulletNumberingInstanceId };

            numbering.Append(numInstance);
        }

        private static void EnsureNumberedDefinition(Numbering numbering)
        {
            // Check of definitie al bestaat
            var existing = numbering.Elements<AbstractNum>()
                .FirstOrDefault(a => a.AbstractNumberId?.Value == NumberedAbstractNumId);
            if (existing != null) return;

            var abstractNum = new AbstractNum { AbstractNumberId = NumberedAbstractNumId };
            abstractNum.Append(new Nsid { Val = "9003ABCD" });
            abstractNum.Append(new MultiLevelType { Val = MultiLevelValues.SingleLevel });

            // Level 0: Genummerd "1.", "2.", etc.
            var level0 = new Level { LevelIndex = 0 };
            level0.Append(new StartNumberingValue { Val = 1 });
            level0.Append(new NumberingFormat { Val = NumberFormatValues.Decimal });
            level0.Append(new LevelText { Val = "%1." });
            level0.Append(new LevelJustification { Val = LevelJustificationValues.Left });

            var pPr = new PreviousParagraphProperties();
            pPr.Append(new Indentation { Left = "720", Hanging = "360" });
            level0.Append(pPr);

            abstractNum.Append(level0);

            // Voeg toe NA bestaande AbstractNums
            var lastAbstractNum = numbering.Elements<AbstractNum>().LastOrDefault();
            if (lastAbstractNum != null)
                lastAbstractNum.InsertAfterSelf(abstractNum);
            else
                numbering.PrependChild(abstractNum);

            // NumberingInstance
            var numInstance = new NumberingInstance(
                new AbstractNumId { Val = NumberedAbstractNumId }
            ) { NumberID = NumberedNumberingInstanceId };

            numbering.Append(numInstance);
        }

        /// <summary>
        /// Verwijdert ALLE bekende list markers uit een paragraaf.
        /// Voorkomt dat restanten als letterlijke tekst verschijnen wanneer
        /// meerdere markers in dezelfde paragraaf staan (bijv. [[INDENT]] + [[BULLET]]).
        /// </summary>
        private static void RemoveAllListMarkers(Paragraph paragraph)
        {
            RemoveMarkerText(paragraph, BulletPattern);
            RemoveMarkerText(paragraph, SubBulletPattern);
            RemoveMarkerText(paragraph, IndentPattern);
            RemoveMarkerText(paragraph, ListItemPattern);
        }

        private static string GetParagraphText(Paragraph paragraph)
        {
            return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
        }

        private static void RemoveMarkerText(Paragraph paragraph, Regex pattern)
        {
            foreach (var text in paragraph.Descendants<Text>())
            {
                if (pattern.IsMatch(text.Text))
                {
                    text.Text = pattern.Replace(text.Text, "");
                }
            }
        }

        /// <summary>
        /// Past Word list numbering toe op een paragraph met indent.
        /// </summary>
        private static void ApplyListNumbering(Paragraph paragraph, int numId)
        {
            var pPr = paragraph.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                paragraph.InsertAt(pPr, 0);
            }

            // Verwijder bestaande nummering
            var existingNumPr = pPr.NumberingProperties;
            existingNumPr?.Remove();

            // Voeg numbering toe
            var numPr = new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = numId }
            );
            pPr.InsertAt(numPr, 0);

            // Voeg indentation toe als die er nog niet is
            var existingInd = pPr.Indentation;
            if (existingInd == null)
            {
                pPr.Append(new Indentation { Left = "720", Hanging = "360" });
            }
        }

        /// <summary>
        /// Past alleen inspringing toe op een paragraph (zonder bullet of nummering).
        /// Gebruikt voor ingesprongen tekst met [[INDENT]] marker.
        /// </summary>
        private static void ApplyIndentation(Paragraph paragraph)
        {
            var pPr = paragraph.ParagraphProperties;
            if (pPr == null)
            {
                pPr = new ParagraphProperties();
                paragraph.InsertAt(pPr, 0);
            }

            // Verwijder bestaande indentation
            var existingInd = pPr.Indentation;
            existingInd?.Remove();

            // Voeg indentation toe (zelfde niveau als bullet, maar zonder bullet-karakter)
            pPr.Append(new Indentation { Left = "720" });
        }
    }
}

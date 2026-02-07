using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.Artikel;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Genereert artikelen content voor het ouderschapsplan/convenant
    /// Vervangt de [[ARTIKELEN]] placeholder met alle actieve artikelen
    /// </summary>
    public class ArtikelContentGenerator : ITableGenerator
    {
        private readonly ILogger<ArtikelContentGenerator> _logger;
        private readonly IArtikelService _artikelService;

        /// <summary>
        /// Placeholder replacements die worden ingesteld voor processing
        /// </summary>
        public Dictionary<string, string>? Replacements { get; set; }

        public string PlaceholderTag => "[[ARTIKELEN]]";

        public ArtikelContentGenerator(
            ILogger<ArtikelContentGenerator> logger,
            IArtikelService artikelService)
        {
            _logger = logger;
            _artikelService = artikelService;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            if (data.Artikelen == null || data.Artikelen.Count == 0)
            {
                _logger.LogWarning($"[{correlationId}] Geen artikelen beschikbaar voor document generatie");
                return elements;
            }

            _logger.LogInformation($"[{correlationId}] Genereren van {data.Artikelen.Count} artikelen");

            // Gebruik de replacements voor conditionele filtering
            var replacements = Replacements ?? new Dictionary<string, string>();

            // Filter conditionele artikelen (met dossierData voor geavanceerde AND/OR condities)
            var artikelen = _artikelService.FilterConditioneleArtikelen(data.Artikelen, replacements, data);

            _logger.LogInformation($"[{correlationId}] Na conditionele filtering: {artikelen.Count} artikelen");

            // Sorteer op volgorde
            artikelen = artikelen.OrderBy(a => a.Volgorde).ToList();

            int artikelCount = 0;

            foreach (var artikel in artikelen)
            {
                // Genereer artikel elementen (nummering wordt later toegepast door ArticleNumberingHelper)
                var artikelElements = GenerateArtikelContent(artikel, replacements, correlationId);

                if (artikelElements.Count > 0)
                {
                    elements.AddRange(artikelElements);
                    artikelCount++;
                }
            }

            _logger.LogInformation($"[{correlationId}] {artikelCount} artikelen gegenereerd (nummering via [[ARTIKEL]] placeholders)");
            return elements;
        }

        /// <summary>
        /// Genereert de content voor een enkel artikel.
        /// Gebruikt [[ARTIKEL]] placeholder voor dynamische nummering via ArticleNumberingHelper.
        /// </summary>
        private List<OpenXmlElement> GenerateArtikelContent(
            ArtikelData artikel,
            Dictionary<string, string> replacements,
            string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            // Verwerk artikel tekst (conditionele blokken en placeholders)
            var verwerkteTekst = _artikelService.VerwerkArtikelTekst(artikel, replacements);

            // Skip artikel als tekst leeg is na verwerking
            if (string.IsNullOrWhiteSpace(verwerkteTekst))
            {
                _logger.LogDebug($"[{correlationId}] Artikel '{artikel.ArtikelCode}' overgeslagen (lege tekst na verwerking)");
                return elements;
            }

            // Artikel kop met nummering placeholder (nummering via ArticleNumberingHelper)
            var effectieveTitel = _artikelService.VervangPlaceholders(artikel.EffectieveTitel, replacements);

            switch (artikel.NummeringType)
            {
                case "nieuw_nummer":
                    // Heading1 style voor Word TOC met [[ARTIKEL]] nummering
                    elements.Add(CreateHeadingParagraph($"[[ARTIKEL]] {effectieveTitel}", "Heading1", "24", "200", "120"));
                    break;
                case "doornummeren":
                    // Heading2 style met [[SUBARTIKEL]] nummering (1.1, 1.2, etc.)
                    elements.Add(CreateHeadingParagraph($"[[SUBARTIKEL]] {effectieveTitel}", "Heading2", "22", "120", "60"));
                    break;
                case "geen_nummer":
                default:
                    // Geen nummering, geen heading style (considerans, ondertekening, etc.)
                    elements.Add(CreateHeadingParagraph(effectieveTitel, null, "24", "200", "120"));
                    break;
            }

            // Maak body paragraphs (split op newlines)
            var bodyParagraphs = CreateBodyParagraphs(verwerkteTekst);
            elements.AddRange(bodyParagraphs);

            // Voeg lege regel toe na artikel
            elements.Add(OpenXmlHelper.CreateEmptyParagraph());

            return elements;
        }

        /// <summary>
        /// Maakt een heading paragraph met configureerbare style en grootte.
        /// </summary>
        /// <param name="text">Tekst inclusief eventuele nummering placeholder</param>
        /// <param name="headingStyle">Word heading style (bijv. "Heading1", "Heading2") of null voor geen heading style</param>
        /// <param name="fontSize">Font grootte in half-points (bijv. "24" = 12pt)</param>
        /// <param name="spaceBefore">Ruimte boven in twips</param>
        /// <param name="spaceAfter">Ruimte onder in twips</param>
        private Paragraph CreateHeadingParagraph(string text, string? headingStyle, string fontSize, string spaceBefore, string spaceAfter)
        {
            var paragraph = new Paragraph();
            var paragraphProps = new ParagraphProperties();

            if (headingStyle != null)
            {
                paragraphProps.Append(new ParagraphStyleId() { Val = headingStyle });
            }

            paragraphProps.Append(new SpacingBetweenLines()
            {
                Before = spaceBefore,
                After = spaceAfter
            });

            paragraph.Append(paragraphProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new Bold());
            runProps.Append(new FontSize() { Val = fontSize });
            run.Append(runProps);
            run.Append(new Text(text));

            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Maakt body paragraphs van tekst, gesplitst op newlines
        /// </summary>
        private List<OpenXmlElement> CreateBodyParagraphs(string tekst)
        {
            var paragraphs = new List<OpenXmlElement>();

            // Split tekst op newlines
            var regels = tekst.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            foreach (var regel in regels)
            {
                if (string.IsNullOrWhiteSpace(regel))
                {
                    // Lege regel wordt een spacing paragraph
                    paragraphs.Add(CreateSpacingParagraph());
                }
                else
                {
                    paragraphs.Add(CreateBodyParagraph(regel));
                }
            }

            return paragraphs;
        }

        /// <summary>
        /// Maakt een body paragraph met standaard styling
        /// </summary>
        private Paragraph CreateBodyParagraph(string text)
        {
            var paragraph = new Paragraph();

            // Paragraph properties
            var paragraphProps = new ParagraphProperties();

            // Spacing voor body text
            paragraphProps.Append(new SpacingBetweenLines()
            {
                After = "60",   // 3pt ruimte onder
                Line = "276",   // 1.15 line spacing
                LineRule = LineSpacingRuleValues.Auto
            });

            // Justified alignment
            paragraphProps.Append(new Justification() { Val = JustificationValues.Both });

            paragraph.Append(paragraphProps);

            // Run met text
            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new FontSize() { Val = "22" }); // 11pt
            run.Append(runProps);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Maakt een lege paragraph voor spacing
        /// </summary>
        private Paragraph CreateSpacingParagraph()
        {
            var paragraph = new Paragraph();
            var paragraphProps = new ParagraphProperties();
            paragraphProps.Append(new SpacingBetweenLines() { After = "0" });
            paragraph.Append(paragraphProps);
            return paragraph;
        }
    }
}

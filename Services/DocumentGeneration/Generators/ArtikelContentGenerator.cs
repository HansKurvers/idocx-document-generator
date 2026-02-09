using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.Artikel;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Genereert artikelen content voor het ouderschapsplan/convenant
    /// Vervangt de [[ARTIKELEN]] placeholder met alle actieve artikelen
    /// </summary>
    public class ArtikelContentGenerator : ITableGenerator
    {
        private static readonly Regex BulletLinePattern = new(@"^- ", RegexOptions.Compiled);
        private static readonly Regex NumberedLinePattern = new(@"^\d+\.\s", RegexOptions.Compiled);

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

            for (int i = 0; i < artikelen.Count; i++)
            {
                var artikel = artikelen[i];

                // Lege regel vóór een nieuw hoofdartikel (niet voor het eerste artikel)
                if (artikel.NummeringType == "nieuw_nummer" && artikelCount > 0)
                {
                    elements.Add(OpenXmlHelper.CreateEmptyParagraph());
                }

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

            // Artikel kop met nummering afhankelijk van nummering_type
            var effectieveTitel = _artikelService.VervangPlaceholders(artikel.EffectieveTitel, replacements);

            // Alleen een heading toevoegen als de titel niet leeg is
            if (!string.IsNullOrWhiteSpace(effectieveTitel))
            {
                switch (artikel.NummeringType)
                {
                    case "nieuw_nummer":
                        elements.Add(CreateArtikelHeading($"[[ARTIKEL]] {effectieveTitel.ToUpper()}"));
                        break;
                    case "doornummeren":
                        elements.Add(CreateArtikelHeading($"[[SUBARTIKEL]] {effectieveTitel}"));
                        break;
                    case "geen_nummer":
                    default:
                        elements.Add(CreateBoldParagraph(effectieveTitel));
                        break;
                }
            }

            // Maak body paragraphs als er tekst is (split op newlines)
            if (!string.IsNullOrWhiteSpace(verwerkteTekst))
            {
                var bodyParagraphs = CreateBodyParagraphs(verwerkteTekst);
                elements.AddRange(bodyParagraphs);
            }

            return elements;
        }

        /// <summary>
        /// Maakt een artikel heading met Heading1 style voor Word inhoudsopgave
        /// </summary>
        private Paragraph CreateArtikelHeading(string text)
        {
            var paragraph = new Paragraph();

            // Paragraph properties
            var paragraphProps = new ParagraphProperties();

            // Heading1 style voor Word TOC (inhoudsopgave)
            paragraphProps.Append(new ParagraphStyleId() { Val = "Heading1" });

            // Spacing voor heading (ruimte boven)
            paragraphProps.Append(new SpacingBetweenLines()
            {
                Before = "200",  // 10pt ruimte boven
                After = "120"    // 6pt ruimte onder
            });

            paragraph.Append(paragraphProps);

            // Run met bold text
            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new Bold());
            runProps.Append(new FontSize() { Val = "24" }); // 12pt
            run.Append(runProps);
            run.Append(new Text(text));

            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Maakt een bold paragraph zonder Heading style (verschijnt niet in TOC)
        /// Gebruikt voor artikelen met nummering_type 'geen_nummer'
        /// </summary>
        private Paragraph CreateBoldParagraph(string text)
        {
            var paragraph = new Paragraph();
            var paragraphProps = new ParagraphProperties();
            paragraphProps.Append(new SpacingBetweenLines()
            {
                Before = "200",
                After = "120"
            });
            paragraph.Append(paragraphProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new Bold());
            runProps.Append(new FontSize() { Val = "24" }); // 12pt
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
                else if (BulletLinePattern.IsMatch(regel))
                {
                    // Bullet list item: strip "- " prefix, voeg [[BULLET]] marker toe
                    var bulletTekst = BulletLinePattern.Replace(regel, "", 1);
                    paragraphs.Add(CreateBodyParagraph("[[BULLET]]" + bulletTekst));
                }
                else if (NumberedLinePattern.IsMatch(regel))
                {
                    // Genummerd list item: strip "1. " prefix, voeg [[LISTITEM]] marker toe
                    var listTekst = NumberedLinePattern.Replace(regel, "", 1);
                    paragraphs.Add(CreateBodyParagraph("[[LISTITEM]]" + listTekst));
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

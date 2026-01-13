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
    /// Genereert een inhoudsopgave met hyperlinks naar artikelen.
    /// Vervangt de [[INHOUDSOPGAVE]] placeholder.
    /// </summary>
    public class InhoudsopgaveGenerator : ITableGenerator
    {
        private readonly ILogger<InhoudsopgaveGenerator> _logger;
        private readonly IArtikelService _artikelService;

        /// <summary>
        /// Placeholder replacements die worden ingesteld voor processing
        /// </summary>
        public Dictionary<string, string>? Replacements { get; set; }

        public string PlaceholderTag => "[[INHOUDSOPGAVE]]";

        public InhoudsopgaveGenerator(
            ILogger<InhoudsopgaveGenerator> logger,
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
                _logger.LogWarning($"[{correlationId}] Geen artikelen beschikbaar voor inhoudsopgave");
                return elements;
            }

            _logger.LogInformation($"[{correlationId}] Genereren inhoudsopgave voor {data.Artikelen.Count} artikelen");

            // Gebruik de replacements voor conditionele filtering (zelfde logica als ArtikelContentGenerator)
            var replacements = Replacements ?? new Dictionary<string, string>();

            // Filter conditionele artikelen
            var artikelen = _artikelService.FilterConditioneleArtikelen(data.Artikelen, replacements);

            // Sorteer op volgorde
            artikelen = artikelen.OrderBy(a => a.Volgorde).ToList();

            // Heading voor inhoudsopgave
            elements.Add(CreateTocHeading("Inhoudsopgave"));

            int artikelNummer = 1;
            int tocEntries = 0;

            foreach (var artikel in artikelen)
            {
                // Verwerk artikel tekst om te checken of het artikel content heeft
                var verwerkteTekst = _artikelService.VerwerkArtikelTekst(artikel, replacements);

                // Skip artikelen zonder tekst (zelfde logica als ArtikelContentGenerator)
                if (string.IsNullOrWhiteSpace(verwerkteTekst))
                {
                    continue;
                }

                // Haal effectieve titel op
                var effectieveTitel = _artikelService.VervangPlaceholders(artikel.EffectieveTitel, replacements);
                var bookmarkName = $"artikel_{artikel.ArtikelCode}";
                var displayText = $"Artikel {artikelNummer}: {effectieveTitel}";

                // Maak TOC entry met hyperlink
                elements.Add(CreateTocEntry(displayText, bookmarkName));

                artikelNummer++;
                tocEntries++;
            }

            // Lege regel na inhoudsopgave
            elements.Add(OpenXmlHelper.CreateEmptyParagraph());

            _logger.LogInformation($"[{correlationId}] Inhoudsopgave gegenereerd met {tocEntries} entries");

            return elements;
        }

        /// <summary>
        /// Maakt de heading voor de inhoudsopgave
        /// </summary>
        private Paragraph CreateTocHeading(string text)
        {
            var paragraph = new Paragraph();

            var paragraphProps = new ParagraphProperties();
            paragraphProps.Append(new SpacingBetweenLines()
            {
                Before = "0",
                After = "200"  // 10pt ruimte onder heading
            });

            paragraph.Append(paragraphProps);

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new Bold());
            runProps.Append(new FontSize() { Val = "28" }); // 14pt
            run.Append(runProps);
            run.Append(new Text(text));

            paragraph.Append(run);
            return paragraph;
        }

        /// <summary>
        /// Maakt een TOC entry met hyperlink naar artikel bookmark
        /// </summary>
        private Paragraph CreateTocEntry(string text, string bookmarkAnchor)
        {
            var paragraph = new Paragraph();

            var paragraphProps = new ParagraphProperties();
            paragraphProps.Append(new SpacingBetweenLines()
            {
                After = "60"  // 3pt ruimte onder elke entry
            });

            paragraph.Append(paragraphProps);

            // Hyperlink naar bookmark
            var hyperlink = new Hyperlink() { Anchor = bookmarkAnchor };

            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new Color() { Val = "0563C1" }); // Standaard hyperlink blauw
            runProps.Append(new Underline() { Val = UnderlineValues.Single });
            runProps.Append(new FontSize() { Val = "22" }); // 11pt
            run.Append(runProps);
            run.Append(new Text(text));

            hyperlink.Append(run);
            paragraph.Append(hyperlink);

            return paragraph;
        }
    }
}

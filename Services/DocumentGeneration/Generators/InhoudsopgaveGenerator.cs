using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Genereert een Word inhoudsopgave (TOC) die automatisch wordt gevuld
    /// op basis van Heading1 styles in het document.
    /// Vervangt de [[INHOUDSOPGAVE]] placeholder.
    /// </summary>
    public class InhoudsopgaveGenerator : ITableGenerator
    {
        private readonly ILogger<InhoudsopgaveGenerator> _logger;

        /// <summary>
        /// Placeholder replacements (niet gebruikt voor TOC, maar vereist door interface patroon)
        /// </summary>
        public Dictionary<string, string>? Replacements { get; set; }

        public string PlaceholderTag => "[[INHOUDSOPGAVE]]";

        public InhoudsopgaveGenerator(ILogger<InhoudsopgaveGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            _logger.LogInformation($"[{correlationId}] Genereren Word inhoudsopgave (TOC field)");

            // Heading voor inhoudsopgave
            elements.Add(CreateTocHeading("Inhoudsopgave"));

            // TOC Field - Word herkent dit en vult het automatisch met Heading1 entries
            elements.Add(CreateTocField());

            // Lege regel na inhoudsopgave
            elements.Add(OpenXmlHelper.CreateEmptyParagraph());

            _logger.LogInformation($"[{correlationId}] Word TOC field gegenereerd (update in Word met F9 of rechtermuisklik)");

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
        /// Maakt een Word TOC field dat automatisch wordt gevuld op basis van Heading styles.
        /// De gebruiker kan dit updaten in Word met F9 of rechtermuisklik → "Veld bijwerken".
        /// </summary>
        private Paragraph CreateTocField()
        {
            var paragraph = new Paragraph();

            // SimpleField met TOC instructie
            // \o "1-1" = include heading levels 1-1 (alleen Heading1)
            // \h = hyperlinks
            // \z = hide page numbers in web view
            // \u = use outline levels
            var field = new SimpleField()
            {
                Instruction = " TOC \\o \"1-1\" \\h \\z \\u "
            };

            // Placeholder tekst (wordt vervangen wanneer TOC wordt bijgewerkt in Word)
            var run = new Run();
            var runProps = new RunProperties();
            runProps.Append(new Color() { Val = "808080" }); // Grijs
            runProps.Append(new Italic());
            run.Append(runProps);
            run.Append(new Text("Klik met rechtermuisknop en kies 'Veld bijwerken' om de inhoudsopgave te genereren"));

            field.Append(run);
            paragraph.Append(field);

            return paragraph;
        }
    }
}

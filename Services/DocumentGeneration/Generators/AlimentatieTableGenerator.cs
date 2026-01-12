using DocumentFormat.OpenXml;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Generators
{
    /// <summary>
    /// Generates alimentatie (alimony/financial agreements) table
    /// </summary>
    public class AlimentatieTableGenerator : ITableGenerator
    {
        private readonly ILogger<AlimentatieTableGenerator> _logger;

        public string PlaceholderTag => "[[TABEL_ALIMENTATIE]]";

        /// <summary>
        /// Placeholder replacements for accessing party designations (Partij1Benaming, Partij2Benaming)
        /// </summary>
        public Dictionary<string, string>? Replacements { get; set; }

        public AlimentatieTableGenerator(ILogger<AlimentatieTableGenerator> logger)
        {
            _logger = logger;
        }

        public List<OpenXmlElement> Generate(DossierData data, string correlationId)
        {
            var elements = new List<OpenXmlElement>();

            if (data.Alimentatie == null)
            {
                _logger.LogWarning($"[{correlationId}] No alimentatie data available");
                return elements;
            }

            var alimentatie = data.Alimentatie;

            // Add general alimentatie information section
            if (alimentatie.NettoBesteedbaarGezinsinkomen.HasValue ||
                alimentatie.KostenKinderen.HasValue ||
                alimentatie.BijdrageKostenKinderen.HasValue)
            {
                elements.Add(OpenXmlHelper.CreateStyledHeading("Algemene financiële gegevens"));

                var generalTable = CreateGeneralAlimentatieTable(alimentatie);
                elements.Add(generalTable);
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            // Add per person contributions table
            if (alimentatie.BijdragenKostenKinderen.Any())
            {
                elements.Add(OpenXmlHelper.CreateStyledHeading("Eigen aandeel per partij"));

                var contributionsTable = CreateContributionsTable(alimentatie.BijdragenKostenKinderen);
                elements.Add(contributionsTable);
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            // Add kinderrekening section if applicable
            if (alimentatie.IsKinderrekeningBetaalwijze)
            {
                AddKinderrekeningSection(elements, alimentatie, data.Partij1, data.Partij2);
            }

            // Add per child financial agreements table
            if (alimentatie.FinancieleAfsprakenKinderen.Any() && data.Kinderen.Any())
            {
                elements.Add(OpenXmlHelper.CreateStyledHeading("Financiële afspraken per kind"));

                var childAgreementsTable = CreateChildAgreementsTable(
                    alimentatie.FinancieleAfsprakenKinderen,
                    data.Kinderen,
                    data.Partij1,
                    data.Partij2,
                    alimentatie);
                elements.Add(childAgreementsTable);
                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            _logger.LogInformation($"[{correlationId}] Generated alimentatie tables");
            return elements;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateGeneralAlimentatieTable(AlimentatieData alimentatie)
        {
            var columnWidths = new[] { 3500, 2500 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add header row with smaller font size
            var headers = new[] { "Omschrijving", "Bedrag" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.DarkBlue, OpenXmlHelper.Colors.White, fontSize: "18");
            table.Append(headerRow);

            // Add data rows
            if (alimentatie.NettoBesteedbaarGezinsinkomen.HasValue)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Netto besteedbaar gezinsinkomen", fontSize: "18"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(alimentatie.NettoBesteedbaarGezinsinkomen), fontSize: "18"));
                table.Append(row);
            }

            if (alimentatie.KostenKinderen.HasValue)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Kosten kinderen", fontSize: "18"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(alimentatie.KostenKinderen), fontSize: "18"));
                table.Append(row);
            }

            if (alimentatie.BijdrageKostenKinderen.HasValue)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Bijdrage kosten kinderen", fontSize: "18"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(alimentatie.BijdrageKostenKinderen), fontSize: "18"));
                table.Append(row);
            }

            if (!string.IsNullOrEmpty(alimentatie.BijdrageTemplateOmschrijving))
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell("Bijdrage template", fontSize: "18"));
                row.Append(OpenXmlHelper.CreateStyledCell(alimentatie.BijdrageTemplateOmschrijving, fontSize: "18"));
                table.Append(row);
            }

            return table;
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateContributionsTable(List<BijdrageKostenKinderenData> bijdragen)
        {
            var columnWidths = new[] { 3500, 2500 };
            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add header row with smaller font size
            var headers = new[] { "Partij", "Eigen aandeel" };
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.DarkBlue, OpenXmlHelper.Colors.White, fontSize: "18");
            table.Append(headerRow);

            // Add data rows
            foreach (var bijdrage in bijdragen)
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                row.Append(OpenXmlHelper.CreateStyledCell(bijdrage.PersoonNaam ?? "Onbekend", fontSize: "18"));
                row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(bijdrage.EigenAandeel), fontSize: "18"));
                table.Append(row);
            }

            return table;
        }

        private void AddKinderrekeningSection(List<OpenXmlElement> elements, AlimentatieData alimentatie, PersonData? partij1, PersonData? partij2)
        {
            elements.Add(OpenXmlHelper.CreateStyledHeading("Kinderrekening informatie"));

            // Stortingen op kinderrekening
            if (alimentatie.StortingOuder1Kinderrekening.HasValue || alimentatie.StortingOuder2Kinderrekening.HasValue)
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("Stortingen op kinderrekening:", true));

                if (alimentatie.StortingOuder1Kinderrekening.HasValue && partij1 != null)
                {
                    var benaming1 = GetPartijBenaming(1, partij1, partij2);
                    elements.Add(OpenXmlHelper.CreateSimpleParagraph($"- {benaming1}: {DataFormatter.FormatCurrency(alimentatie.StortingOuder1Kinderrekening)} per maand"));
                }

                if (alimentatie.StortingOuder2Kinderrekening.HasValue && partij2 != null)
                {
                    var benaming2 = GetPartijBenaming(2, partij1, partij2);
                    elements.Add(OpenXmlHelper.CreateSimpleParagraph($"- {benaming2}: {DataFormatter.FormatCurrency(alimentatie.StortingOuder2Kinderrekening)} per maand"));
                }

                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            // Kostensoorten die van kinderrekening mogen worden betaald
            if (alimentatie.KinderrekeningKostensoorten.Any())
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("De volgende kosten mogen van de kinderrekening worden betaald:", true));

                foreach (var kostensoort in alimentatie.KinderrekeningKostensoorten)
                {
                    elements.Add(OpenXmlHelper.CreateSimpleParagraph($"- {kostensoort}"));
                }

                elements.Add(OpenXmlHelper.CreateEmptyParagraph());
            }

            // Maximum opnamebedrag
            elements.Add(OpenXmlHelper.CreateSimpleParagraph("Maximum opnamebedrag:", true));

            if (alimentatie.KinderrekeningMaximumOpname.GetValueOrDefault())
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- Er is een maximum bedrag voor opnames van de kinderrekening zonder overeenstemming"));
                if (alimentatie.KinderrekeningMaximumOpnameBedrag.HasValue)
                {
                    elements.Add(OpenXmlHelper.CreateSimpleParagraph($"- Maximum opnamebedrag: {DataFormatter.FormatCurrency(alimentatie.KinderrekeningMaximumOpnameBedrag)}"));
                }
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- Bij opnames of bestedingen boven dit bedrag is overeenstemming met de andere ouder benodigd"));
            }
            else
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- Geen maximum opnamebedrag ingesteld"));
            }

            elements.Add(OpenXmlHelper.CreateEmptyParagraph());

            // Kinderbijslag
            elements.Add(OpenXmlHelper.CreateSimpleParagraph("Kinderbijslag:", true));
            if (alimentatie.KinderbijslagStortenOpKinderrekening.GetValueOrDefault())
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- Kinderbijslag wordt gestort op de kinderrekening"));
            }
            else
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- Kinderbijslag wordt NIET op de kinderrekening gestort"));
            }

            elements.Add(OpenXmlHelper.CreateEmptyParagraph());

            // Kindgebonden budget
            elements.Add(OpenXmlHelper.CreateSimpleParagraph("Kindgebonden budget:", true));

            if (alimentatie.KindgebondenBudgetStortenOpKinderrekening.GetValueOrDefault())
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- Kindgebonden budget wordt gestort op de kinderrekening"));
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- LET OP: Volgens de alimentatienormen is het niet gebruikelijk om het kindgebonden budget op de kinderrekening te storten, daar het bedoeld is voor de ouder die ook de aanvrager van de kinderbijslag is."));
            }
            else
            {
                elements.Add(OpenXmlHelper.CreateSimpleParagraph("- Kindgebonden budget wordt NIET op de kinderrekening gestort"));
            }

            elements.Add(OpenXmlHelper.CreateEmptyParagraph());
        }

        private DocumentFormat.OpenXml.Wordprocessing.Table CreateChildAgreementsTable(
            List<FinancieleAfsprakenKinderenData> afspraken,
            List<ChildData> kinderen,
            PersonData? partij1,
            PersonData? partij2,
            AlimentatieData alimentatie)
        {
            // Determine columns based on betaalwijze
            bool isKinderrekening = alimentatie.IsKinderrekeningBetaalwijze;
            bool showKinderbijslag = !alimentatie.KinderbijslagStortenOpKinderrekening.GetValueOrDefault();

            // Build headers dynamically
            var headersList = new List<string> { "Kind" };

            // Only show alimentatie bedrag if NOT kinderrekening
            if (!isKinderrekening)
            {
                headersList.Add("Alimentatie");
            }

            headersList.Add("Hoofdverblijf");

            // Only show kinderbijslag if not stored on kinderrekening
            if (showKinderbijslag)
            {
                headersList.Add("Kinderbijslag");
            }

            headersList.Add("Zorgkorting %");
            headersList.Add("Inschrijving");
            headersList.Add("Kindgebonden budget");

            var headers = headersList.ToArray();
            var columnCount = headers.Length;
            var columnWidth = 8000 / columnCount;
            var columnWidths = Enumerable.Repeat(columnWidth, columnCount).ToArray();

            var table = OpenXmlHelper.CreateStyledTable(OpenXmlHelper.Colors.DarkBlue, columnWidths);

            // Add header row with smaller font size
            var headerRow = OpenXmlHelper.CreateHeaderRow(headers, OpenXmlHelper.Colors.DarkBlue, OpenXmlHelper.Colors.White, fontSize: "18");
            table.Append(headerRow);

            // Add data rows - iterate through children to maintain order
            foreach (var kind in kinderen)
            {
                var afspraak = afspraken.FirstOrDefault(a => a.KindId == kind.Id);

                if (afspraak != null)
                {
                    var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();

                    row.Append(OpenXmlHelper.CreateStyledCell(kind.Roepnaam ?? kind.Voornamen ?? "", fontSize: "18"));

                    // Only show alimentatie bedrag if NOT kinderrekening
                    if (!isKinderrekening)
                    {
                        row.Append(OpenXmlHelper.CreateStyledCell(DataFormatter.FormatCurrency(afspraak.AlimentatieBedrag), fontSize: "18"));
                    }

                    row.Append(OpenXmlHelper.CreateStyledCell(afspraak.Hoofdverblijf ?? "", fontSize: "18"));

                    // Only show kinderbijslag if not stored on kinderrekening
                    if (showKinderbijslag)
                    {
                        row.Append(OpenXmlHelper.CreateStyledCell(afspraak.KinderbijslagOntvanger ?? "", fontSize: "18"));
                    }

                    row.Append(OpenXmlHelper.CreateStyledCell(afspraak.ZorgkortingPercentage.HasValue ? $"{afspraak.ZorgkortingPercentage:0.##}%" : "", fontSize: "18"));
                    row.Append(OpenXmlHelper.CreateStyledCell(afspraak.Inschrijving ?? "", fontSize: "18"));
                    row.Append(OpenXmlHelper.CreateStyledCell(afspraak.KindgebondenBudget ?? "", fontSize: "18"));

                    table.Append(row);
                }
            }

            return table;
        }

        private string GetPartijNaam(int? partijNummer, PersonData? partij1, PersonData? partij2)
        {
            return GetPartijBenaming(partijNummer ?? 0, partij1, partij2);
        }

        private string GetKinderbijslagOntvanger(int? partijNummer, PersonData? partij1, PersonData? partij2)
        {
            return partijNummer switch
            {
                1 => GetPartijBenaming(1, partij1, partij2),
                2 => GetPartijBenaming(2, partij1, partij2),
                3 => "Kinderrekening",
                _ => ""
            };
        }

        /// <summary>
        /// Gets the party designation (de vader/de moeder) for a party number
        /// </summary>
        private string GetPartijBenaming(int partijNummer, PersonData? partij1, PersonData? partij2)
        {
            // Use Partij1Benaming/Partij2Benaming from replacements if available
            if (Replacements != null)
            {
                if (partijNummer == 1 && Replacements.TryGetValue("Partij1Benaming", out var benaming1))
                    return benaming1;
                if (partijNummer == 2 && Replacements.TryGetValue("Partij2Benaming", out var benaming2))
                    return benaming2;
            }

            // Fallback: determine benaming from gender
            var persoon = partijNummer == 1 ? partij1 : partij2;
            if (persoon == null) return partijNummer == 1 ? "Partij 1" : "Partij 2";

            var geslacht = persoon.Geslacht?.Trim().ToLowerInvariant();
            return geslacht switch
            {
                "m" or "man" => "de vader",
                "v" or "vrouw" => "de moeder",
                _ => persoon.Roepnaam ?? persoon.Voornamen ?? $"Partij {partijNummer}"
            };
        }
    }
}
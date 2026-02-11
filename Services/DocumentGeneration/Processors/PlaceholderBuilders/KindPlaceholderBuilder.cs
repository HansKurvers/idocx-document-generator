using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Builder voor kinderen placeholders.
    /// Verantwoordelijk voor Kind1*, Kind2*, etc. en samenvattende placeholders.
    /// </summary>
    public class KindPlaceholderBuilder : BasePlaceholderBuilder
    {
        public override int Order => 30;

        public KindPlaceholderBuilder(ILogger<KindPlaceholderBuilder> logger)
            : base(logger)
        {
        }

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogDebug("Building kind placeholders for dossier {DossierId}", data.Id);

            var kinderen = data.Kinderen ?? new List<ChildData>();

            if (!kinderen.Any())
            {
                _logger.LogDebug("No children found, skipping kind placeholders");
                return;
            }

            // Total count
            AddPlaceholder(replacements, "AantalKinderen", kinderen.Count.ToString());

            // Individual children placeholders
            for (int i = 0; i < kinderen.Count; i++)
            {
                var child = kinderen[i];
                var prefix = $"Kind{i + 1}";

                AddPlaceholder(replacements, $"{prefix}Naam", child.VolledigeNaam);
                AddPlaceholder(replacements, $"{prefix}Voornaam", child.Voornamen);
                AddPlaceholder(replacements, $"{prefix}Roepnaam",
                    child.Roepnaam ?? child.Voornamen?.Split(' ').FirstOrDefault());
                AddPlaceholder(replacements, $"{prefix}Achternaam", child.Achternaam);
                AddPlaceholder(replacements, $"{prefix}Tussenvoegsel", child.Tussenvoegsel);
                AddPlaceholder(replacements, $"{prefix}RoepnaamAchternaam", GetKindRoepnaamAchternaam(child));
                AddPlaceholder(replacements, $"{prefix}Geboortedatum", DataFormatter.FormatDate(child.GeboorteDatum));
                AddPlaceholder(replacements, $"{prefix}Geboorteplaats", child.GeboortePlaats);
                AddPlaceholder(replacements, $"{prefix}Leeftijd", child.Leeftijd?.ToString());
                AddPlaceholder(replacements, $"{prefix}Geslacht", child.Geslacht);
            }

            // Lists with proper Dutch grammar
            var voornamenList = kinderen
                .Select(k => k.Voornamen ?? k.VolledigeNaam)
                .ToList();
            var roepnamenList = kinderen
                .Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam)
                .ToList();
            var volledigeNamenList = kinderen
                .Select(k => k.VolledigeNaam)
                .ToList();

            AddPlaceholder(replacements, "KinderenNamen", DutchLanguageHelper.FormatList(voornamenList));
            AddPlaceholder(replacements, "KinderenRoepnamen", DutchLanguageHelper.FormatList(roepnamenList));
            AddPlaceholder(replacements, "KinderenVolledigeNamen", DutchLanguageHelper.FormatList(volledigeNamenList));

            // Minor children (under 18)
            var minderjarigeKinderen = kinderen
                .Where(k => k.Leeftijd.HasValue && k.Leeftijd.Value < 18)
                .ToList();
            var roepnamenMinderjaarigenList = minderjarigeKinderen
                .Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam)
                .ToList();

            AddPlaceholder(replacements, "AantalMinderjarigeKinderen", minderjarigeKinderen.Count.ToString());
            AddPlaceholder(replacements, "RoepnamenMinderjarigeKinderen",
                DutchLanguageHelper.FormatList(roepnamenMinderjaarigenList));

            // Opsomming van alle kinderen met geboortegegevens (voor considerans)
            var opsommingLines = kinderen
                .Select(k =>
                {
                    var naam = k.VolledigeNaam;
                    var geboren = DataFormatter.FormatDateDutchLong(k.GeboorteDatum);
                    var plaats = k.GeboortePlaats ?? "";
                    var roepnaam = k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? naam;
                    return $"- {naam}, geboren op {geboren} te {plaats}, hierna te noemen {roepnaam}";
                })
                .ToList();
            AddPlaceholder(replacements, "KINDEREN_OPSOMMING", string.Join("\n", opsommingLines));

            // Minderjarige kinderen zin
            if (minderjarigeKinderen.Any())
            {
                var zijn_is = minderjarigeKinderen.Count == 1 ? "is" : "zijn";
                AddPlaceholder(replacements, "MINDERJARIGE_KINDEREN_ZIN",
                    $"Van wie {DutchLanguageHelper.FormatList(roepnamenMinderjaarigenList)} nog minderjarig {zijn_is}.");
            }
            else
            {
                AddPlaceholder(replacements, "MINDERJARIGE_KINDEREN_ZIN", "");
            }

            // dit kind/deze kinderen
            AddPlaceholder(replacements, "DIT_KIND_DEZE_KINDEREN",
                kinderen.Count == 1 ? "dit kind" : "deze kinderen");

            _logger.LogDebug("Added kind placeholders for {Count} children ({MinorCount} minors)",
                kinderen.Count, minderjarigeKinderen.Count);
        }
    }
}

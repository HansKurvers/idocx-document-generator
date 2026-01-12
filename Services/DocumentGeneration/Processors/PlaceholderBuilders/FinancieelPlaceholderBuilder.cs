using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Builder voor financiële placeholders (alimentatie, kinderrekening, kosten).
    /// </summary>
    public class FinancieelPlaceholderBuilder : BasePlaceholderBuilder
    {
        public override int Order => 50;

        public FinancieelPlaceholderBuilder(ILogger<FinancieelPlaceholderBuilder> logger)
            : base(logger)
        {
        }

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogDebug("Building financieel placeholders for dossier {DossierId}", data.Id);

            var kinderen = data.Kinderen ?? new List<ChildData>();

            // Add alimentatie placeholders
            AddAlimentatieReplacements(replacements, data.Alimentatie, data.Partij1, data.Partij2, kinderen, data.IsAnoniem);

            // Add hoofdverblijf and inschrijving verdeling
            AddPlaceholder(replacements, "HoofdverblijfVerdeling",
                GetHoofdverblijfVerdeling(data.Alimentatie, data.Partij1, data.Partij2, kinderen, data.IsAnoniem));
            AddPlaceholder(replacements, "InschrijvingVerdeling",
                GetInschrijvingVerdeling(data.Alimentatie, data.Partij1, data.Partij2, kinderen, data.IsAnoniem));
        }

        private void AddAlimentatieReplacements(
            Dictionary<string, string> replacements,
            AlimentatieData? alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen,
            bool? isAnoniem)
        {
            // Initialize all placeholders with empty values first
            InitializeEmptyPlaceholders(replacements);

            if (alimentatie == null)
            {
                _logger.LogDebug("No alimentatie data available, placeholders set to empty strings");
                return;
            }

            // Basic alimentatie data
            AddPlaceholder(replacements, "NettoBesteedbaarGezinsinkomen", DataFormatter.FormatCurrency(alimentatie.NettoBesteedbaarGezinsinkomen));
            AddPlaceholder(replacements, "KostenKinderen", DataFormatter.FormatCurrency(alimentatie.KostenKinderen));
            AddPlaceholder(replacements, "BijdrageKostenKinderen", DataFormatter.FormatCurrency(alimentatie.BijdrageKostenKinderen));
            AddPlaceholder(replacements, "BijdrageTemplateOmschrijving", alimentatie.BijdrageTemplateOmschrijving);

            // Kinderrekening velden
            AddPlaceholder(replacements, "StortingOuder1Kinderrekening", DataFormatter.FormatCurrency(alimentatie.StortingOuder1Kinderrekening));
            AddPlaceholder(replacements, "StortingOuder2Kinderrekening", DataFormatter.FormatCurrency(alimentatie.StortingOuder2Kinderrekening));
            AddPlaceholder(replacements, "KinderrekeningKostensoorten", FormatKostensoortenList(alimentatie.KinderrekeningKostensoorten));
            AddPlaceholder(replacements, "KinderrekeningMaximumOpname", DataFormatter.ConvertToString(alimentatie.KinderrekeningMaximumOpname));
            AddPlaceholder(replacements, "KinderrekeningMaximumOpnameBedrag", DataFormatter.FormatCurrency(alimentatie.KinderrekeningMaximumOpnameBedrag));
            AddPlaceholder(replacements, "KinderbijslagStortenOpKinderrekening", DataFormatter.ConvertToString(alimentatie.KinderbijslagStortenOpKinderrekening));
            AddPlaceholder(replacements, "KindgebondenBudgetStortenOpKinderrekening", DataFormatter.ConvertToString(alimentatie.KindgebondenBudgetStortenOpKinderrekening));

            // Alimentatie settings
            AddPlaceholder(replacements, "BedragenAlleKinderenGelijk", DataFormatter.ConvertToString(alimentatie.BedragenAlleKinderenGelijk));
            AddPlaceholder(replacements, "AlimentatiebedragPerKind", DataFormatter.FormatCurrency(alimentatie.AlimentatiebedragPerKind));
            AddPlaceholder(replacements, "Alimentatiegerechtigde", alimentatie.Alimentatiegerechtigde);
            AddPlaceholder(replacements, "ZorgkortingPercentageAlleKinderen",
                alimentatie.ZorgkortingPercentageAlleKinderen.HasValue
                    ? $"{alimentatie.ZorgkortingPercentageAlleKinderen:0.##}%"
                    : "");

            // Template detection flags
            AddPlaceholder(replacements, "IsKinderrekeningBetaalwijze", DataFormatter.ConvertToString(alimentatie.IsKinderrekeningBetaalwijze));
            AddPlaceholder(replacements, "IsAlimentatieplichtBetaalwijze", DataFormatter.ConvertToString(alimentatie.IsAlimentatieplichtBetaalwijze));

            // Sync settings for all children
            AddPlaceholder(replacements, "AfsprakenAlleKinderenGelijk", DataFormatter.ConvertToString(alimentatie.AfsprakenAlleKinderenGelijk));
            AddPlaceholder(replacements, "HoofdverblijfAlleKinderen", GetPartyName(alimentatie.HoofdverblijfAlleKinderen, partij1, partij2));
            AddPlaceholder(replacements, "InschrijvingAlleKinderen", GetPartyName(alimentatie.InschrijvingAlleKinderen, partij1, partij2));
            AddPlaceholder(replacements, "KinderbijslagOntvangerAlleKinderen", GetPartyNameOrKinderrekening(alimentatie.KinderbijslagOntvangerAlleKinderen, partij1, partij2));
            AddPlaceholder(replacements, "KindgebondenBudgetAlleKinderen", GetPartyNameOrKinderrekening(alimentatie.KindgebondenBudgetAlleKinderen, partij1, partij2));

            // Generate betaalwijze beschrijving
            AddPlaceholder(replacements, "BetaalwijzeBeschrijving", GetBetaalwijzeBeschrijving(alimentatie, partij1, partij2, isAnoniem));

            _logger.LogDebug("Added alimentatie basic data: Gezinsinkomen={Gezinsinkomen}, KostenKinderen={KostenKinderen}, IsKinderrekening={IsKinderrekening}",
                replacements["NettoBesteedbaarGezinsinkomen"],
                replacements["KostenKinderen"],
                replacements["IsKinderrekeningBetaalwijze"]);

            // Per person contributions (eigen aandeel)
            if (alimentatie.BijdragenKostenKinderen.Any())
            {
                foreach (var bijdrage in alimentatie.BijdragenKostenKinderen)
                {
                    if (partij1 != null && bijdrage.PersonenId == partij1.Id)
                    {
                        AddPlaceholder(replacements, "Partij1EigenAandeel", DataFormatter.FormatCurrency(bijdrage.EigenAandeel));
                        _logger.LogDebug("Set Partij1EigenAandeel to {Amount}", replacements["Partij1EigenAandeel"]);
                    }
                    else if (partij2 != null && bijdrage.PersonenId == partij2.Id)
                    {
                        AddPlaceholder(replacements, "Partij2EigenAandeel", DataFormatter.FormatCurrency(bijdrage.EigenAandeel));
                        _logger.LogDebug("Set Partij2EigenAandeel to {Amount}", replacements["Partij2EigenAandeel"]);
                    }
                }
            }

            // Build formatted list of all children's financial agreements
            if (alimentatie.FinancieleAfsprakenKinderen.Any() && kinderen.Any())
            {
                var kinderenAlimentatieList = new List<string>();

                foreach (var kind in kinderen)
                {
                    var afspraak = alimentatie.FinancieleAfsprakenKinderen.FirstOrDefault(f => f.KindId == kind.Id);

                    if (afspraak != null)
                    {
                        var lines = new List<string>();
                        lines.Add($"{kind.VolledigeNaam}:");

                        if (afspraak.AlimentatieBedrag.HasValue)
                            lines.Add($"  - Alimentatie: {DataFormatter.FormatCurrency(afspraak.AlimentatieBedrag)}");
                        if (!string.IsNullOrEmpty(afspraak.Hoofdverblijf))
                            lines.Add($"  - Hoofdverblijf: {afspraak.Hoofdverblijf}");
                        if (!string.IsNullOrEmpty(afspraak.KinderbijslagOntvanger))
                            lines.Add($"  - Kinderbijslag: {afspraak.KinderbijslagOntvanger}");
                        if (afspraak.ZorgkortingPercentage.HasValue)
                            lines.Add($"  - Zorgkorting: {afspraak.ZorgkortingPercentage:0.##}%");
                        if (!string.IsNullOrEmpty(afspraak.Inschrijving))
                            lines.Add($"  - Inschrijving bij: {afspraak.Inschrijving}");
                        if (!string.IsNullOrEmpty(afspraak.KindgebondenBudget))
                            lines.Add($"  - Kindgebonden budget: {afspraak.KindgebondenBudget}");

                        kinderenAlimentatieList.Add(string.Join("\n", lines));
                    }
                }

                AddPlaceholder(replacements, "KinderenAlimentatie", string.Join("\n\n", kinderenAlimentatieList));
                _logger.LogDebug("Added KinderenAlimentatie with {Count} children", kinderenAlimentatieList.Count);
            }
        }

        private void InitializeEmptyPlaceholders(Dictionary<string, string> replacements)
        {
            var placeholders = new[]
            {
                "NettoBesteedbaarGezinsinkomen", "KostenKinderen", "BijdrageKostenKinderen", "BijdrageTemplateOmschrijving",
                "Partij1EigenAandeel", "Partij2EigenAandeel", "KinderenAlimentatie",
                "StortingOuder1Kinderrekening", "StortingOuder2Kinderrekening", "KinderrekeningKostensoorten",
                "KinderrekeningMaximumOpname", "KinderrekeningMaximumOpnameBedrag",
                "KinderbijslagStortenOpKinderrekening", "KindgebondenBudgetStortenOpKinderrekening",
                "BedragenAlleKinderenGelijk", "AlimentatiebedragPerKind", "Alimentatiegerechtigde",
                "ZorgkortingPercentageAlleKinderen", "IsKinderrekeningBetaalwijze", "IsAlimentatieplichtBetaalwijze",
                "AfsprakenAlleKinderenGelijk", "HoofdverblijfAlleKinderen", "InschrijvingAlleKinderen",
                "KinderbijslagOntvangerAlleKinderen", "KindgebondenBudgetAlleKinderen", "BetaalwijzeBeschrijving"
            };

            foreach (var placeholder in placeholders)
            {
                AddPlaceholder(replacements, placeholder, "");
            }
        }

        #region Betaalwijze Beschrijving

        private string GetBetaalwijzeBeschrijving(AlimentatieData alimentatie, PersonData? partij1, PersonData? partij2, bool? isAnoniem)
        {
            var ouder1Naam = GetPartijBenaming(partij1, false);
            var ouder2Naam = GetPartijBenaming(partij2, false);

            if (string.IsNullOrEmpty(ouder1Naam)) ouder1Naam = "Ouder 1";
            if (string.IsNullOrEmpty(ouder2Naam)) ouder2Naam = "Ouder 2";

            if (alimentatie.IsKinderrekeningBetaalwijze)
            {
                return GetKinderrekeningBeschrijving(alimentatie, ouder1Naam, ouder2Naam);
            }
            else if (alimentatie.IsAlimentatieplichtBetaalwijze)
            {
                return GetAlimentatieBeschrijving(alimentatie, ouder1Naam, ouder2Naam);
            }

            return "";
        }

        private string GetKinderrekeningBeschrijving(AlimentatieData alimentatie, string ouder1Naam, string ouder2Naam)
        {
            var paragrafen = new List<string>();

            paragrafen.Add("Wij hebben ervoor gekozen om gebruik te maken van een gezamenlijke kinderrekening.");

            // Kinderbijslag en kindgebonden budget
            var toeslagenZinnen = new List<string>();

            var kinderbijslagOntvanger = alimentatie.KinderbijslagOntvangerAlleKinderen?.ToLower();
            if (!string.IsNullOrEmpty(kinderbijslagOntvanger))
            {
                var ontvangerNaam = kinderbijslagOntvanger == "partij1" ? ouder1Naam : (kinderbijslagOntvanger == "partij2" ? ouder2Naam : "");
                if (!string.IsNullOrEmpty(ontvangerNaam))
                {
                    var actie = alimentatie.KinderbijslagStortenOpKinderrekening == true ? "stort deze op de kinderrekening" : "houdt deze";
                    toeslagenZinnen.Add($"{ontvangerNaam} ontvangt de kinderbijslag en {actie}.");
                }
            }

            var kgbOntvanger = alimentatie.KindgebondenBudgetAlleKinderen?.ToLower();
            if (!string.IsNullOrEmpty(kgbOntvanger))
            {
                var ontvangerNaam = kgbOntvanger == "partij1" ? ouder1Naam : (kgbOntvanger == "partij2" ? ouder2Naam : "");
                if (!string.IsNullOrEmpty(ontvangerNaam))
                {
                    var actie = alimentatie.KindgebondenBudgetStortenOpKinderrekening == true ? "stort deze op de kinderrekening" : "houdt deze";
                    toeslagenZinnen.Add($"{ontvangerNaam} ontvangt het kindgebonden budget en {actie}.");
                }
            }

            if (toeslagenZinnen.Any())
            {
                paragrafen.Add(string.Join(" ", toeslagenZinnen));
            }

            paragrafen.Add("Wij betalen allebei de eigen verblijfskosten.");

            if (alimentatie.KinderrekeningKostensoorten != null && alimentatie.KinderrekeningKostensoorten.Any())
            {
                var kostenLijst = FormatKostensoortenList(alimentatie.KinderrekeningKostensoorten);
                paragrafen.Add($"De verblijfsoverstijgende kosten betalen wij van de kinderrekening:\n{kostenLijst}\nVan deze rekening hebben wij allebei een pinpas.");
            }
            else
            {
                paragrafen.Add("De verblijfsoverstijgende kosten betalen wij van de kinderrekening. Van deze rekening hebben wij allebei een pinpas.");
            }

            // Stortingen
            var stortingenZinnen = new List<string>();
            if (alimentatie.StortingOuder1Kinderrekening.HasValue && alimentatie.StortingOuder1Kinderrekening > 0)
            {
                stortingenZinnen.Add($"{ouder1Naam} zal iedere maand een bedrag van {DataFormatter.FormatCurrency(alimentatie.StortingOuder1Kinderrekening)} op deze rekening storten.");
            }
            if (alimentatie.StortingOuder2Kinderrekening.HasValue && alimentatie.StortingOuder2Kinderrekening > 0)
            {
                stortingenZinnen.Add($"{ouder2Naam} zal iedere maand een bedrag van {DataFormatter.FormatCurrency(alimentatie.StortingOuder2Kinderrekening)} op deze rekening storten.");
            }
            if (stortingenZinnen.Any())
            {
                paragrafen.Add(string.Join(" ", stortingenZinnen));
            }

            paragrafen.Add("Wij zullen regelmatig controleren of onze bijdragen genoeg zijn om alle kosten te betalen. Als er structureel een tekort is, zullen wij in overleg met elkaar een hogere bijdrage op de kinderrekening storten.");

            paragrafen.Add("Wij zullen op verzoek aan elkaar uitleggen waarvoor wij bepaalde opnames van de kinderrekening hebben gedaan.");

            if (alimentatie.KinderrekeningMaximumOpname == true && alimentatie.KinderrekeningMaximumOpnameBedrag.HasValue && alimentatie.KinderrekeningMaximumOpnameBedrag > 0)
            {
                paragrafen.Add($"Per transactie kan maximaal {DataFormatter.FormatCurrency(alimentatie.KinderrekeningMaximumOpnameBedrag)} zonder overleg worden opgenomen.");
            }

            var opheffingsOptie = alimentatie.KinderrekeningOpheffen?.ToLower();
            if (!string.IsNullOrEmpty(opheffingsOptie))
            {
                var opheffingsTekst = opheffingsOptie switch
                {
                    "helft" => "krijgen we ieder de helft van het saldo op de rekening",
                    "verhouding" => "krijgen we ieder het deel waar we recht op hebben in verhouding tot ieders bijdrage op de rekening",
                    "spaarrekening" => "maken we het saldo over op een spaarrekening van onze kinderen. Ieder kind krijgt dan evenveel",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(opheffingsTekst))
                {
                    paragrafen.Add($"Als de rekening wordt opgeheven, dan {opheffingsTekst}.");
                }
            }

            return string.Join("\n", paragrafen);
        }

        private string GetAlimentatieBeschrijving(AlimentatieData alimentatie, string ouder1Naam, string ouder2Naam)
        {
            var zinnen = new List<string>();

            zinnen.Add("Wij hebben ervoor gekozen om een maandelijkse kinderalimentatie af te spreken.");

            var alimentatiegerechtigde = alimentatie.Alimentatiegerechtigde?.ToLower();
            var gerechtigdeNaam = alimentatiegerechtigde == "partij1" ? ouder1Naam : (alimentatiegerechtigde == "partij2" ? ouder2Naam : "");
            var plichtigeNaam = alimentatiegerechtigde == "partij1" ? ouder2Naam : (alimentatiegerechtigde == "partij2" ? ouder1Naam : "");

            if (!string.IsNullOrEmpty(gerechtigdeNaam))
            {
                zinnen.Add($"{gerechtigdeNaam} ontvangt en houdt de kinderbijslag en het kindgebonden budget.");
            }

            zinnen.Add("Wij betalen allebei de eigen verblijfskosten.");

            if (alimentatie.ZorgkortingPercentageAlleKinderen.HasValue)
            {
                zinnen.Add($"Wij houden rekening met een zorgkorting van {alimentatie.ZorgkortingPercentageAlleKinderen:0.##}%.");
            }

            if (!string.IsNullOrEmpty(gerechtigdeNaam))
            {
                zinnen.Add($"{gerechtigdeNaam} betaalt de verblijfsoverstijgende kosten.");
            }

            if (!string.IsNullOrEmpty(plichtigeNaam) && !string.IsNullOrEmpty(gerechtigdeNaam) && alimentatie.AlimentatiebedragPerKind.HasValue)
            {
                var ingangsdatum = GetIngangsdatumTekst(alimentatie);
                zinnen.Add($"{plichtigeNaam} betaalt vanaf {ingangsdatum} een kinderalimentatie van {DataFormatter.FormatCurrency(alimentatie.AlimentatiebedragPerKind)} per kind per maand aan {gerechtigdeNaam}.");
            }

            zinnen.Add("Het alimentatiebedrag wordt ieder jaar verhoogd op basis van de wettelijke indexering.");

            if (alimentatie.EersteIndexeringJaar.HasValue)
            {
                zinnen.Add($"De eerste jaarlijkse verhoging is per 1 januari {alimentatie.EersteIndexeringJaar}.");
            }

            return string.Join("\n", zinnen);
        }

        private string GetIngangsdatumTekst(AlimentatieData alimentatie)
        {
            var optie = alimentatie.IngangsdatumOptie?.ToLower();

            return optie switch
            {
                "ondertekening" => "datum ondertekening",
                "anders" when !string.IsNullOrEmpty(alimentatie.IngangsdatumAnders) => alimentatie.IngangsdatumAnders,
                _ when alimentatie.Ingangsdatum.HasValue => alimentatie.Ingangsdatum.Value.ToString("d MMMM yyyy", new CultureInfo("nl-NL")),
                _ => "de ingangsdatum"
            };
        }

        #endregion

        #region Hoofdverblijf en Inschrijving Verdeling

        private string GetHoofdverblijfVerdeling(
            AlimentatieData? alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen,
            bool? isAnoniem)
        {
            if (alimentatie == null || kinderen == null || !kinderen.Any())
                return "";

            if (!alimentatie.FinancieleAfsprakenKinderen.Any())
                return "";

            var kinderenBijPartij1 = new List<ChildData>();
            var kinderenBijPartij2 = new List<ChildData>();
            var kinderenCoOuderschap = new List<ChildData>();

            foreach (var kind in kinderen)
            {
                var afspraak = alimentatie.FinancieleAfsprakenKinderen.FirstOrDefault(f => f.KindId == kind.Id);

                if (afspraak != null && !string.IsNullOrEmpty(afspraak.Hoofdverblijf))
                {
                    var hoofdverblijf = afspraak.Hoofdverblijf.ToLower().Trim();

                    if (hoofdverblijf == "partij1")
                        kinderenBijPartij1.Add(kind);
                    else if (hoofdverblijf == "partij2")
                        kinderenBijPartij2.Add(kind);
                    else if (hoofdverblijf.Contains("co-ouderschap") || hoofdverblijf.Contains("coouderschap"))
                        kinderenCoOuderschap.Add(kind);
                }
            }

            var zinnen = new List<string>();

            if (kinderenBijPartij1.Any())
            {
                var namen = kinderenBijPartij1.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var heeftHebben = kinderenBijPartij1.Count == 1 ? "heeft" : "hebben";
                var zijnHaarHun = kinderenBijPartij1.Count == 1
                    ? (kinderenBijPartij1[0].Geslacht?.ToLower() == "m" ? "zijn" : "haar")
                    : "hun";
                var partij1Benaming = GetPartijBenaming(partij1, false);

                zinnen.Add($"{namenTekst} {heeftHebben} {zijnHaarHun} hoofdverblijf bij {partij1Benaming}.");
            }

            if (kinderenBijPartij2.Any())
            {
                var namen = kinderenBijPartij2.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var heeftHebben = kinderenBijPartij2.Count == 1 ? "heeft" : "hebben";
                var zijnHaarHun = kinderenBijPartij2.Count == 1
                    ? (kinderenBijPartij2[0].Geslacht?.ToLower() == "m" ? "zijn" : "haar")
                    : "hun";
                var partij2Benaming = GetPartijBenaming(partij2, false);

                zinnen.Add($"{namenTekst} {heeftHebben} {zijnHaarHun} hoofdverblijf bij {partij2Benaming}.");
            }

            if (kinderenCoOuderschap.Any())
            {
                var namen = kinderenCoOuderschap.Select(k => k.Naam).ToList();
                var enkelvoud = kinderenCoOuderschap.Count == 1;

                if (enkelvoud)
                {
                    var kindNaam = namen[0];
                    var zijnHaar = kinderenCoOuderschap[0].Geslacht?.ToLower() == "m" ? "hij" : "zij";
                    var heeftZin = $"{zijnHaar} heeft";

                    zinnen.Add($"Voor {kindNaam} hebben wij een zorgregeling afgesproken waarbij {zijnHaar} ongeveer evenveel tijd bij ieder van ons verblijft. {char.ToUpper(zijnHaar[0]) + zijnHaar.Substring(1)} {heeftZin} dus geen hoofdverblijf.");
                }
                else
                {
                    var namenTekst = DutchLanguageHelper.FormatList(namen);
                    zinnen.Add($"Voor {namenTekst} hebben wij een zorgregeling afgesproken waarbij zij ongeveer evenveel tijd bij ieder van ons verblijven. Zij hebben dus geen hoofdverblijf.");
                }
            }

            // Special case: all children have co-parenting
            if (kinderenCoOuderschap.Count == kinderen.Count && !kinderenBijPartij1.Any() && !kinderenBijPartij2.Any())
            {
                var enkelvoud = kinderen.Count == 1;
                var onzeTekst = enkelvoud ? "ons kind" : "onze kinderen";
                var verblijftVerblijven = enkelvoud ? "verblijft" : "verblijven";
                var heeftHebben = enkelvoud ? "heeft" : "hebben";
                var hetKindZij = enkelvoud ? "Het kind" : "Zij";

                return $"Wij hebben een zorgregeling afgesproken waarbij {onzeTekst} ongeveer evenveel tijd bij ieder van ons {verblijftVerblijven}. {hetKindZij} {heeftHebben} dus geen hoofdverblijf.";
            }

            return string.Join(" ", zinnen);
        }

        private string GetInschrijvingVerdeling(
            AlimentatieData? alimentatie,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen,
            bool? isAnoniem)
        {
            if (alimentatie == null || kinderen == null || !kinderen.Any())
                return "";

            if (!alimentatie.FinancieleAfsprakenKinderen.Any())
                return "";

            var kinderenBijPartij1 = new List<ChildData>();
            var kinderenBijPartij2 = new List<ChildData>();

            foreach (var kind in kinderen)
            {
                var afspraak = alimentatie.FinancieleAfsprakenKinderen.FirstOrDefault(f => f.KindId == kind.Id);

                if (afspraak != null && !string.IsNullOrEmpty(afspraak.Inschrijving))
                {
                    var inschrijving = afspraak.Inschrijving.ToLower().Trim();

                    if (inschrijving == "partij1")
                        kinderenBijPartij1.Add(kind);
                    else if (inschrijving == "partij2")
                        kinderenBijPartij2.Add(kind);
                }
            }

            var zinnen = new List<string>();

            if (kinderenBijPartij1.Any())
            {
                var namen = kinderenBijPartij1.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var zalZullen = kinderenBijPartij1.Count == 1 ? "zal" : "zullen";
                var partij1Benaming = GetPartijBenaming(partij1, false);
                var plaats1 = partij1?.Plaats ?? "onbekend";

                zinnen.Add($"{namenTekst} {zalZullen} ingeschreven staan in de Basisregistratie Personen aan het adres van {partij1Benaming} in {plaats1}.");
            }

            if (kinderenBijPartij2.Any())
            {
                var namen = kinderenBijPartij2.Select(k => k.Naam).ToList();
                var namenTekst = DutchLanguageHelper.FormatList(namen);
                var zalZullen = kinderenBijPartij2.Count == 1 ? "zal" : "zullen";
                var partij2Benaming = GetPartijBenaming(partij2, false);
                var plaats2 = partij2?.Plaats ?? "onbekend";

                zinnen.Add($"{namenTekst} {zalZullen} ingeschreven staan in de Basisregistratie Personen aan het adres van {partij2Benaming} in {plaats2}.");
            }

            return string.Join(" ", zinnen);
        }

        #endregion
    }
}

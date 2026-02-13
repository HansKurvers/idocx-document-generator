using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    /// <summary>
    /// Builds Dutch grammar rules based on child data
    /// Handles singular/plural forms and gender-specific pronouns
    /// </summary>
    public class GrammarRulesBuilder
    {
        private readonly ILogger<GrammarRulesBuilder> _logger;

        public GrammarRulesBuilder(ILogger<GrammarRulesBuilder> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates grammar rules based on the number of minor children and their genders
        /// </summary>
        /// <param name="children">List of all children</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Dictionary of grammar rule replacements</returns>
        public Dictionary<string, string> BuildRules(List<ChildData> children, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Building grammar rules for {children.Count} children");

            var rules = new Dictionary<string, string>();

            // Count minor children (under 18)
            var minorChildren = children.Where(c => c.Leeftijd.HasValue && c.Leeftijd.Value < 18).ToList();
            int minorCount = minorChildren.Count;
            bool isPlural = minorCount > 1;

            _logger.LogInformation($"[{correlationId}] Found {minorCount} minor children (isPlural: {isPlural})");

            // New placeholder system: {KIND}/{KINDEREN}
            // Generate child/children names based on context
            string kindNaam = "";
            string kinderenNamen = "";
            
            if (minorChildren.Any())
            {
                if (minorCount == 1)
                {
                    // Single child: use roepnaam or first name
                    var child = minorChildren.First();
                    kindNaam = child.Roepnaam ?? child.Voornamen?.Split(' ').FirstOrDefault() ?? child.Achternaam ?? "het kind";
                }
                else
                {
                    // Multiple children: format as list
                    var roepnamen = minorChildren.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList();
                    kinderenNamen = DutchLanguageHelper.FormatList(roepnamen);
                }
            }
            else
            {
                // No minor children - use generic terms
                kindNaam = "het kind";
                kinderenNamen = "de kinderen";
            }

            // Add the new {KIND} and {KINDEREN} placeholders
            rules["KIND"] = isPlural ? kinderenNamen : kindNaam;
            rules["KINDEREN"] = kinderenNamen.Any() ? kinderenNamen : "de kinderen";

            // Basic singular/plural rules (for backward compatibility)
            rules["ons kind/onze kinderen"] = DutchLanguageHelper.GetChildTerm(isPlural);
            rules["het kind/de kinderen"] = isPlural ? "de kinderen" : "het kind";
            rules["kind/kinderen"] = isPlural ? "kinderen" : "kind";
            rules["heeft/hebben"] = DutchLanguageHelper.VerbForms.Heeft_Hebben(isPlural);
            rules["is/zijn"] = DutchLanguageHelper.VerbForms.Is_Zijn(isPlural);
            rules["verblijft/verblijven"] = DutchLanguageHelper.VerbForms.Verblijft_Verblijven(isPlural);
            rules["kan/kunnen"] = DutchLanguageHelper.VerbForms.Kan_Kunnen(isPlural);
            rules["zal/zullen"] = DutchLanguageHelper.VerbForms.Zal_Zullen(isPlural);
            rules["moet/moeten"] = DutchLanguageHelper.VerbForms.Moet_Moeten(isPlural);
            rules["wordt/worden"] = DutchLanguageHelper.VerbForms.Wordt_Worden(isPlural);
            rules["blijft/blijven"] = DutchLanguageHelper.VerbForms.Blijft_Blijven(isPlural);
            rules["gaat/gaan"] = DutchLanguageHelper.VerbForms.Gaat_Gaan(isPlural);
            rules["komt/komen"] = DutchLanguageHelper.VerbForms.Komt_Komen(isPlural);

            // New verb forms for template system
            rules["zou/zouden"] = isPlural ? "zouden" : "zou";
            rules["wil/willen"] = isPlural ? "willen" : "wil";
            rules["mag/mogen"] = isPlural ? "mogen" : "mag";
            rules["doet/doen"] = isPlural ? "doen" : "doet";
            rules["krijgt/krijgen"] = isPlural ? "krijgen" : "krijgt";
            rules["neemt/nemen"] = isPlural ? "nemen" : "neemt";
            rules["brengt/brengen"] = isPlural ? "brengen" : "brengt";
            rules["haalt/halen"] = isPlural ? "halen" : "haalt";

            // Possessive pronouns
            rules["zijn/haar/hun"] = isPlural ? "hun" : "zijn/haar";
            rules["diens/dier/hun"] = isPlural ? "hun" : "diens/dier";

            // Gender and count specific pronouns
            if (isPlural)
            {
                // Multiple children - use plural pronouns
                rules["hem/haar/hen"] = DutchLanguageHelper.GetObjectPronoun(null, isPlural: true);
                rules["hij/zij/ze"] = DutchLanguageHelper.GetSubjectPronoun(null, isPlural: true);
            }
            else if (minorCount == 1)
            {
                // Single child - use gender-specific pronouns
                var child = minorChildren.First();
                rules["hem/haar/hen"] = DutchLanguageHelper.GetObjectPronoun(child.Geslacht, isPlural: false);
                rules["hij/zij/ze"] = DutchLanguageHelper.GetSubjectPronoun(child.Geslacht, isPlural: false);

                _logger.LogDebug($"[{correlationId}] Single child gender: {child.Geslacht}, pronouns: {rules["hij/zij/ze"]}/{rules["hem/haar/hen"]}");
            }
            else
            {
                // No minor children - use neutral forms
                rules["hem/haar/hen"] = "hem/haar";
                rules["hij/zij/ze"] = "hij/zij";
            }

            // === "Alle" rules: based on TOTAL child count (minor + adult) ===
            int totalCount = children.Count;
            bool allPlural = totalCount > 1;

            _logger.LogInformation($"[{correlationId}] Building 'alle' grammar rules for {totalCount} total children (allPlural: {allPlural})");

            rules["alle ons kind/onze kinderen"] = DutchLanguageHelper.GetChildTerm(allPlural);
            rules["alle het kind/de kinderen"] = allPlural ? "de kinderen" : "het kind";
            rules["alle kind/kinderen"] = allPlural ? "kinderen" : "kind";
            rules["alle heeft/hebben"] = DutchLanguageHelper.VerbForms.Heeft_Hebben(allPlural);
            rules["alle is/zijn"] = DutchLanguageHelper.VerbForms.Is_Zijn(allPlural);
            rules["alle verblijft/verblijven"] = DutchLanguageHelper.VerbForms.Verblijft_Verblijven(allPlural);
            rules["alle kan/kunnen"] = DutchLanguageHelper.VerbForms.Kan_Kunnen(allPlural);
            rules["alle zal/zullen"] = DutchLanguageHelper.VerbForms.Zal_Zullen(allPlural);
            rules["alle moet/moeten"] = DutchLanguageHelper.VerbForms.Moet_Moeten(allPlural);
            rules["alle wordt/worden"] = DutchLanguageHelper.VerbForms.Wordt_Worden(allPlural);
            rules["alle blijft/blijven"] = DutchLanguageHelper.VerbForms.Blijft_Blijven(allPlural);
            rules["alle gaat/gaan"] = DutchLanguageHelper.VerbForms.Gaat_Gaan(allPlural);
            rules["alle komt/komen"] = DutchLanguageHelper.VerbForms.Komt_Komen(allPlural);
            rules["alle zou/zouden"] = allPlural ? "zouden" : "zou";
            rules["alle wil/willen"] = allPlural ? "willen" : "wil";
            rules["alle mag/mogen"] = allPlural ? "mogen" : "mag";
            rules["alle doet/doen"] = allPlural ? "doen" : "doet";
            rules["alle krijgt/krijgen"] = allPlural ? "krijgen" : "krijgt";
            rules["alle neemt/nemen"] = allPlural ? "nemen" : "neemt";
            rules["alle brengt/brengen"] = allPlural ? "brengen" : "brengt";
            rules["alle haalt/halen"] = allPlural ? "halen" : "haalt";

            // "Alle" possessive pronouns
            rules["alle zijn/haar/hun"] = allPlural ? "hun" : "zijn/haar";
            rules["alle diens/dier/hun"] = allPlural ? "hun" : "diens/dier";

            // "Alle" gender and count specific pronouns
            if (allPlural)
            {
                rules["alle hem/haar/hen"] = DutchLanguageHelper.GetObjectPronoun(null, isPlural: true);
                rules["alle hij/zij/ze"] = DutchLanguageHelper.GetSubjectPronoun(null, isPlural: true);
            }
            else if (totalCount == 1)
            {
                var singleChild = children.First();
                rules["alle hem/haar/hen"] = DutchLanguageHelper.GetObjectPronoun(singleChild.Geslacht, isPlural: false);
                rules["alle hij/zij/ze"] = DutchLanguageHelper.GetSubjectPronoun(singleChild.Geslacht, isPlural: false);
            }
            else
            {
                rules["alle hem/haar/hen"] = "hem/haar";
                rules["alle hij/zij/ze"] = "hij/zij";
            }

            _logger.LogInformation($"[{correlationId}] Created {rules.Count} grammar rules");

            return rules;
        }

        /// <summary>
        /// Creates grammar rules based on child count only (simpler version)
        /// </summary>
        /// <param name="childCount">Number of children</param>
        /// <param name="correlationId">Correlation ID for logging</param>
        /// <returns>Dictionary of grammar rule replacements</returns>
        public Dictionary<string, string> BuildSimpleRules(int childCount, string correlationId)
        {
            _logger.LogInformation($"[{correlationId}] Building simple grammar rules for {childCount} children");

            bool isPlural = childCount > 1;

            var rules = new Dictionary<string, string>
            {
                // New placeholder system
                ["KIND"] = isPlural ? "de kinderen" : "het kind",
                ["KINDEREN"] = "de kinderen",

                // Basic singular/plural rules (for backward compatibility)
                ["ons kind/onze kinderen"] = DutchLanguageHelper.GetChildTerm(isPlural),
                ["het kind/de kinderen"] = isPlural ? "de kinderen" : "het kind",
                ["kind/kinderen"] = isPlural ? "kinderen" : "kind",
                ["heeft/hebben"] = DutchLanguageHelper.VerbForms.Heeft_Hebben(isPlural),
                ["is/zijn"] = DutchLanguageHelper.VerbForms.Is_Zijn(isPlural),
                ["verblijft/verblijven"] = DutchLanguageHelper.VerbForms.Verblijft_Verblijven(isPlural),
                ["kan/kunnen"] = DutchLanguageHelper.VerbForms.Kan_Kunnen(isPlural),
                ["zal/zullen"] = DutchLanguageHelper.VerbForms.Zal_Zullen(isPlural),
                ["moet/moeten"] = DutchLanguageHelper.VerbForms.Moet_Moeten(isPlural),
                ["wordt/worden"] = DutchLanguageHelper.VerbForms.Wordt_Worden(isPlural),
                ["blijft/blijven"] = DutchLanguageHelper.VerbForms.Blijft_Blijven(isPlural),
                ["gaat/gaan"] = DutchLanguageHelper.VerbForms.Gaat_Gaan(isPlural),
                ["komt/komen"] = DutchLanguageHelper.VerbForms.Komt_Komen(isPlural),

                // New verb forms for template system
                ["zou/zouden"] = isPlural ? "zouden" : "zou",
                ["wil/willen"] = isPlural ? "willen" : "wil",
                ["mag/mogen"] = isPlural ? "mogen" : "mag",
                ["doet/doen"] = isPlural ? "doen" : "doet",
                ["krijgt/krijgen"] = isPlural ? "krijgen" : "krijgt",
                ["neemt/nemen"] = isPlural ? "nemen" : "neemt",
                ["brengt/brengen"] = isPlural ? "brengen" : "brengt",
                ["haalt/halen"] = isPlural ? "halen" : "haalt",

                // Possessive pronouns
                ["zijn/haar/hun"] = isPlural ? "hun" : "zijn/haar",
                ["diens/dier/hun"] = isPlural ? "hun" : "diens/dier",

                // Object and subject pronouns
                ["hem/haar/hen"] = isPlural ? "hen" : "hem/haar",
                ["hij/zij/ze"] = isPlural ? "ze" : "hij/zij",

                // === "Alle" rules: same as above in simple mode (childCount = total) ===
                ["alle ons kind/onze kinderen"] = DutchLanguageHelper.GetChildTerm(isPlural),
                ["alle het kind/de kinderen"] = isPlural ? "de kinderen" : "het kind",
                ["alle kind/kinderen"] = isPlural ? "kinderen" : "kind",
                ["alle heeft/hebben"] = DutchLanguageHelper.VerbForms.Heeft_Hebben(isPlural),
                ["alle is/zijn"] = DutchLanguageHelper.VerbForms.Is_Zijn(isPlural),
                ["alle verblijft/verblijven"] = DutchLanguageHelper.VerbForms.Verblijft_Verblijven(isPlural),
                ["alle kan/kunnen"] = DutchLanguageHelper.VerbForms.Kan_Kunnen(isPlural),
                ["alle zal/zullen"] = DutchLanguageHelper.VerbForms.Zal_Zullen(isPlural),
                ["alle moet/moeten"] = DutchLanguageHelper.VerbForms.Moet_Moeten(isPlural),
                ["alle wordt/worden"] = DutchLanguageHelper.VerbForms.Wordt_Worden(isPlural),
                ["alle blijft/blijven"] = DutchLanguageHelper.VerbForms.Blijft_Blijven(isPlural),
                ["alle gaat/gaan"] = DutchLanguageHelper.VerbForms.Gaat_Gaan(isPlural),
                ["alle komt/komen"] = DutchLanguageHelper.VerbForms.Komt_Komen(isPlural),
                ["alle zou/zouden"] = isPlural ? "zouden" : "zou",
                ["alle wil/willen"] = isPlural ? "willen" : "wil",
                ["alle mag/mogen"] = isPlural ? "mogen" : "mag",
                ["alle doet/doen"] = isPlural ? "doen" : "doet",
                ["alle krijgt/krijgen"] = isPlural ? "krijgen" : "krijgt",
                ["alle neemt/nemen"] = isPlural ? "nemen" : "neemt",
                ["alle brengt/brengen"] = isPlural ? "brengen" : "brengt",
                ["alle haalt/halen"] = isPlural ? "halen" : "haalt",
                ["alle zijn/haar/hun"] = isPlural ? "hun" : "zijn/haar",
                ["alle diens/dier/hun"] = isPlural ? "hun" : "diens/dier",
                ["alle hem/haar/hen"] = isPlural ? "hen" : "hem/haar",
                ["alle hij/zij/ze"] = isPlural ? "ze" : "hij/zij"
            };

            _logger.LogInformation($"[{correlationId}] Created {rules.Count} simple grammar rules");

            return rules;
        }

        /// <summary>
        /// Voegt grammatica regels toe op basis van het aantal items in JSON-collecties.
        /// Bijv. 1 bankrekening → "bankrekening", 2+ → "bankrekeningen".
        /// Wordt aangeroepen NA BuildRules zodat kinderen-regels al bestaan.
        /// Collectie met 0 items wordt overgeslagen (geen grammatica toegevoegd).
        /// Bij gedeelde sleutels (bijv. "bankrekening/bankrekeningen") wint de eerste collectie met items.
        /// </summary>
        public void AddCollectionGrammarRules(Dictionary<string, string> rules, DossierData data, string correlationId)
        {
            // Declaratieve registratie: JSON-bron + enkelvoud/meervoud paren
            var collectionGrammars = new (string Name, string? Json, (string Singular, string Plural)[] Pairs)[]
            {
                ("BANKREKENINGEN_KINDEREN", data.CommunicatieAfspraken?.BankrekeningKinderen, new[]
                {
                    ("bankrekening", "bankrekeningen"),
                    ("de bankrekening", "de bankrekeningen"),
                    ("saldo", "saldi"),
                    ("het saldo", "de saldi"),
                    ("rekeningnummer", "rekeningnummers"),
                    ("valt", "vallen"),
                    ("staat", "staan"),
                    ("rekening blijft", "rekeningen blijven"),
                    ("rekening zal", "rekeningen zullen"),
                }),
                ("BANKREKENINGEN", data.ConvenantInfo?.Bankrekeningen, new[]
                {
                    ("bankrekening", "bankrekeningen"),
                    ("de bankrekening", "de bankrekeningen"),
                    ("saldo", "saldi"),
                    ("het saldo", "de saldi"),
                    ("valt", "vallen"),
                    ("staat", "staan"),
                    ("rekening blijft", "rekeningen blijven"),
                    ("rekening zal", "rekeningen zullen"),
                }),
                ("BELEGGINGEN", data.ConvenantInfo?.Beleggingen, new[]
                {
                    ("belegging", "beleggingen"),
                    ("de belegging", "de beleggingen"),
                }),
                ("VOERTUIGEN", data.ConvenantInfo?.Voertuigen, new[]
                {
                    ("voertuig", "voertuigen"),
                    ("het voertuig", "de voertuigen"),
                }),
                ("VERZEKERINGEN", data.ConvenantInfo?.Verzekeringen, new[]
                {
                    ("verzekering", "verzekeringen"),
                    ("de verzekering", "de verzekeringen"),
                    ("polis", "polissen"),
                    ("de polis", "de polissen"),
                }),
                ("SCHULDEN", data.ConvenantInfo?.Schulden, new[]
                {
                    ("schuld", "schulden"),
                    ("de schuld", "de schulden"),
                }),
                ("VORDERINGEN", data.ConvenantInfo?.Vorderingen, new[]
                {
                    ("vordering", "vorderingen"),
                    ("de vordering", "de vorderingen"),
                }),
                ("PENSIOENEN", data.ConvenantInfo?.Pensioenen, new[]
                {
                    ("pensioen", "pensioenen"),
                    ("het pensioen", "de pensioenen"),
                }),
            };

            int addedCount = 0;

            foreach (var (name, json, pairs) in collectionGrammars)
            {
                var count = CountJsonArrayItems(json);
                if (count == 0) continue;

                bool isPlural = count > 1;

                foreach (var (singular, plural) in pairs)
                {
                    var key = $"{singular}/{plural}";
                    if (!rules.ContainsKey(key))
                    {
                        rules[key] = isPlural ? plural : singular;
                        addedCount++;
                    }
                }
            }

            _logger.LogInformation($"[{correlationId}] Added {addedCount} collection grammar rules");
        }

        /// <summary>
        /// Telt het aantal items in een JSON array string.
        /// Returns 0 bij null, lege string, of ongeldige JSON.
        /// </summary>
        private static int CountJsonArrayItems(string? json)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Array
                    ? doc.RootElement.GetArrayLength()
                    : 0;
            }
            catch (JsonException)
            {
                return 0;
            }
        }
    }
}
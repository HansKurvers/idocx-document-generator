using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Verwerkt loop secties in artikel tekst.
    /// Syntax: [[#COLLECTIE]]...[[/COLLECTIE]]
    ///
    /// Gedrag:
    /// - Collectie leeg → blok verwijderen
    /// - Kinderen-collecties: blok bevat per-kind variabelen (KIND_*) → itereren per kind
    /// - JSON-collecties: blok bevat per-item variabelen (PREFIX_*) → itereren per item
    /// - Blok bevat GEEN per-item variabelen → blok eenmalig tonen (conditioneel gedrag)
    /// </summary>
    public static class LoopSectionProcessor
    {
        private static readonly Regex LoopPattern = new(
            @"\[\[#(\w+)\]\](.*?)\[\[/\1\]\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Per-kind variabelen die iteratie triggeren
        private static readonly HashSet<string> KindVariabelen = new(StringComparer.OrdinalIgnoreCase)
        {
            "KIND_VOORNAMEN",
            "KIND_ACHTERNAAM",
            "KIND_GEBOORTEDATUM",
            "KIND_GEBOORTEPLAATS",
            "KIND_ROEPNAAM",
            "KIND_LEEFTIJD",
            "KIND_ERKENNINGSDATUM"
        };

        private static readonly Regex KindVariabelPattern = new(
            @"\[\[(KIND_\w+)\]\]",
            RegexOptions.IgnoreCase);

        #region JSON Collection Registry

        /// <summary>
        /// Declaratieve definitie van een JSON-collectie.
        /// Nieuwe collectie toevoegen = nieuwe registratie, geen bestaande code wijzigen.
        /// </summary>
        private record CollectionDefinition(
            string Name,
            string VariablePrefix,
            Func<DossierData, string?> GetJson,
            Func<JsonElement, DossierData, Dictionary<string, string>> MapItem
        );

        private class JsonCollectionResult
        {
            public string Prefix { get; init; } = "";
            public List<Dictionary<string, string>> Items { get; init; } = new();
        }

        private static readonly CollectionDefinition[] JsonCollections = new[]
        {
            new CollectionDefinition(
                "BANKREKENINGEN_KINDEREN", "BANKREKENING",
                d => d.CommunicatieAfspraken?.BankrekeningKinderen,
                MapBankrekeningKinderen),
            new CollectionDefinition(
                "BANKREKENINGEN", "BANKREKENING",
                d => d.ConvenantInfo?.Bankrekeningen,
                MapBankrekening),
            new CollectionDefinition(
                "BELEGGINGEN", "BELEGGING",
                d => d.ConvenantInfo?.Beleggingen,
                MapBelegging),
            new CollectionDefinition(
                "VOERTUIGEN", "VOERTUIG",
                d => d.ConvenantInfo?.Voertuigen,
                MapVoertuig),
            new CollectionDefinition(
                "VERZEKERINGEN", "VERZEKERING",
                d => d.ConvenantInfo?.Verzekeringen,
                MapVerzekering),
            new CollectionDefinition(
                "SCHULDEN", "SCHULD",
                d => d.ConvenantInfo?.Schulden,
                MapSchuld),
            new CollectionDefinition(
                "VORDERINGEN", "VORDERING",
                d => d.ConvenantInfo?.Vorderingen,
                MapVordering),
            new CollectionDefinition(
                "PENSIOENEN", "PENSIOEN",
                d => d.ConvenantInfo?.Pensioenen,
                MapPensioen),
        };

        #endregion

        /// <summary>
        /// Verwerkt alle loop secties in de tekst.
        /// Moet worden aangeroepen VOOR conditionele blokken en placeholder vervanging.
        /// </summary>
        public static string Process(string tekst, DossierData? dossierData)
        {
            if (string.IsNullOrEmpty(tekst) || dossierData == null)
                return tekst;

            // Blijf verwerken voor geneste loops (max 10 iteraties)
            var previousTekst = "";
            var currentTekst = tekst;
            int maxIterations = 10;
            int iteration = 0;

            while (currentTekst != previousTekst && iteration < maxIterations)
            {
                previousTekst = currentTekst;
                currentTekst = LoopPattern.Replace(currentTekst, match =>
                {
                    var collectieNaam = match.Groups[1].Value;
                    var blokInhoud = match.Groups[2].Value;

                    // 1. Kinderen-collecties (bestaande logica)
                    var kinderen = ResolveCollection(collectieNaam, dossierData);
                    if (kinderen != null)
                    {
                        if (kinderen.Count == 0)
                            return "";

                        var heeftKindVariabelen = KindVariabelPattern.IsMatch(blokInhoud);
                        return heeftKindVariabelen
                            ? ExpandPerKind(blokInhoud, kinderen, dossierData)
                            : blokInhoud.Trim();
                    }

                    // 2. Generieke JSON-collecties
                    var jsonResult = ResolveJsonCollection(collectieNaam, dossierData);
                    if (jsonResult != null)
                    {
                        if (jsonResult.Items.Count == 0)
                            return "";

                        var sampleItem = jsonResult.Items.Count > 0 ? jsonResult.Items[0] : null;
                        return HasCollectionVariables(blokInhoud, jsonResult.Prefix, sampleItem)
                            ? ExpandPerItem(blokInhoud, jsonResult.Items, jsonResult.Prefix)
                            : blokInhoud.Trim();
                    }

                    // 3. Onbekende collectie → blok verwijderen
                    return "";
                });
                iteration++;
            }

            // Verwijder opeenvolgende lege regels die kunnen ontstaan
            currentTekst = Regex.Replace(currentTekst, @"(\r?\n){3,}", "\n\n");

            return currentTekst;
        }

        #region Kinderen Collections

        /// <summary>
        /// Resolveert een collectienaam naar een lijst kinderen uit de dossierdata.
        /// Returns null voor onbekende collectienamen (geen kinderen-collectie).
        /// </summary>
        private static List<ChildData>? ResolveCollection(string collectieNaam, DossierData dossierData)
        {
            switch (collectieNaam.ToUpperInvariant())
            {
                case "KINDEREN_UIT_HUWELIJK":
                case "KINDEREN_VOOR_HUWELIJK":
                case "MINDERJARIGE_KINDEREN":
                case "ALLE_KINDEREN":
                    break; // Known kinderen collection, continue below
                default:
                    return null; // Not a kinderen collection → try JSON
            }

            var kinderen = dossierData.Kinderen;
            if (kinderen == null || kinderen.Count == 0)
                return new List<ChildData>();

            return collectieNaam.ToUpperInvariant() switch
            {
                "KINDEREN_UIT_HUWELIJK" => FilterKinderenUitHuwelijk(kinderen, dossierData),
                "KINDEREN_VOOR_HUWELIJK" => FilterKinderenVoorHuwelijk(kinderen, dossierData),
                "MINDERJARIGE_KINDEREN" => kinderen.Where(k => k.Leeftijd.HasValue && k.Leeftijd.Value < 18).ToList(),
                "ALLE_KINDEREN" => kinderen,
                _ => new List<ChildData>()
            };
        }

        /// <summary>
        /// Filtert kinderen geboren tijdens of na het huwelijk.
        /// Fallback: als geen Huwelijksdatum beschikbaar, gebruik HeeftKinderenUitHuwelijk boolean.
        /// </summary>
        private static List<ChildData> FilterKinderenUitHuwelijk(List<ChildData> kinderen, DossierData dossierData)
        {
            var huwelijksdatum = dossierData.ConvenantInfo?.Huwelijksdatum;

            if (huwelijksdatum.HasValue)
            {
                return kinderen
                    .Where(k => k.GeboorteDatum.HasValue && k.GeboorteDatum.Value >= huwelijksdatum.Value)
                    .ToList();
            }

            // Fallback: boolean vlag
            if (dossierData.ConvenantInfo?.HeeftKinderenUitHuwelijk == true)
                return kinderen;

            return new List<ChildData>();
        }

        /// <summary>
        /// Filtert kinderen geboren voor het huwelijk.
        /// Fallback: als geen Huwelijksdatum beschikbaar, gebruik HeeftKinderenVoorHuwelijk boolean.
        /// </summary>
        private static List<ChildData> FilterKinderenVoorHuwelijk(List<ChildData> kinderen, DossierData dossierData)
        {
            var huwelijksdatum = dossierData.ConvenantInfo?.Huwelijksdatum;

            if (huwelijksdatum.HasValue)
            {
                return kinderen
                    .Where(k => k.GeboorteDatum.HasValue && k.GeboorteDatum.Value < huwelijksdatum.Value)
                    .ToList();
            }

            // Fallback: boolean vlag
            if (dossierData.ConvenantInfo?.HeeftKinderenVoorHuwelijk == true)
                return kinderen;

            return new List<ChildData>();
        }

        /// <summary>
        /// Expandeert een blok per kind, vervangt KIND_* variabelen met kindgegevens.
        /// </summary>
        private static string ExpandPerKind(string blokInhoud, List<ChildData> kinderen, DossierData dossierData)
        {
            var regels = new List<string>();

            foreach (var kind in kinderen)
            {
                var kindReplacements = BuildKindReplacements(kind, dossierData);
                var regel = blokInhoud.Trim();

                // Vervang per-kind variabelen
                regel = KindVariabelPattern.Replace(regel, match =>
                {
                    var variabele = match.Groups[1].Value.ToUpperInvariant();
                    return kindReplacements.TryGetValue(variabele, out var waarde) ? waarde : match.Value;
                });

                regels.Add(regel);
            }

            return string.Join("\n", regels);
        }

        /// <summary>
        /// Bouwt een dictionary met per-kind variabelen voor placeholder vervanging.
        /// </summary>
        private static Dictionary<string, string> BuildKindReplacements(ChildData kind, DossierData dossierData)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KIND_VOORNAMEN"] = kind.Voornamen ?? "",
                ["KIND_ACHTERNAAM"] = FormatKindAchternaam(kind),
                ["KIND_GEBOORTEDATUM"] = DataFormatter.FormatDateDutchLong(kind.GeboorteDatum),
                ["KIND_GEBOORTEPLAATS"] = kind.GeboortePlaats ?? "",
                ["KIND_ROEPNAAM"] = kind.Roepnaam ?? kind.Naam ?? "",
                ["KIND_LEEFTIJD"] = kind.Leeftijd?.ToString() ?? "",
                ["KIND_ERKENNINGSDATUM"] = DataFormatter.FormatDateDutchLong(dossierData.ConvenantInfo?.Erkenningsdatum)
            };

            return replacements;
        }

        /// <summary>
        /// Formatteert de achternaam van een kind inclusief tussenvoegsel.
        /// </summary>
        private static string FormatKindAchternaam(ChildData kind)
        {
            if (!string.IsNullOrEmpty(kind.Tussenvoegsel))
                return $"{kind.Tussenvoegsel} {kind.Achternaam}";

            return kind.Achternaam;
        }

        #endregion

        #region JSON Collection Processing

        /// <summary>
        /// Zoekt een collectienaam op in de JSON-registry en parst de JSON data.
        /// Returns null als de collectienaam niet in de registry staat.
        /// </summary>
        private static JsonCollectionResult? ResolveJsonCollection(string collectieNaam, DossierData dossierData)
        {
            var upper = collectieNaam.ToUpperInvariant();
            var def = Array.Find(JsonCollections, c => c.Name == upper);
            if (def == null)
                return null;

            var json = def.GetJson(dossierData);
            if (string.IsNullOrEmpty(json))
                return new JsonCollectionResult { Prefix = def.VariablePrefix };

            try
            {
                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.EnumerateArray()
                    .Select(item => def.MapItem(item, dossierData))
                    .ToList();
                return new JsonCollectionResult { Prefix = def.VariablePrefix, Items = items };
            }
            catch (JsonException)
            {
                return new JsonCollectionResult { Prefix = def.VariablePrefix };
            }
        }

        /// <summary>
        /// Checkt of een blok variabelen bevat die door de collectie-mapper geproduceerd worden.
        /// Controleert eerst op PREFIX_* patronen, daarna op exacte keys uit de sample item dictionary.
        /// </summary>
        private static bool HasCollectionVariables(string blokInhoud, string prefix, Dictionary<string, string>? sampleItem = null)
        {
            // Bestaande prefix-check
            var pattern = @"\[\[" + Regex.Escape(prefix) + @"_\w+\]\]";
            if (Regex.IsMatch(blokInhoud, pattern, RegexOptions.IgnoreCase))
                return true;

            // Nieuwe check: kijk of een key uit de sample item voorkomt in het blok
            if (sampleItem != null)
            {
                foreach (var key in sampleItem.Keys)
                {
                    if (blokInhoud.Contains($"[[{key}]]", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Expandeert een blok per item, vervangt ALLE [[VARIABELE]] patronen met item-specifieke waarden.
        /// Matcht zowel PREFIX_* variabelen als template-vriendelijke aliases (bijv. SALDO, PEILDATUM).
        /// </summary>
        private static string ExpandPerItem(string blokInhoud, List<Dictionary<string, string>> items, string prefix)
        {
            var pattern = new Regex(@"\[\[(\w+)\]\]", RegexOptions.IgnoreCase);
            var regels = new List<string>();

            foreach (var item in items)
            {
                var regel = blokInhoud.Trim();
                regel = pattern.Replace(regel, match =>
                {
                    var variabele = match.Groups[1].Value.ToUpperInvariant();
                    return item.TryGetValue(variabele, out var waarde) ? waarde : match.Value;
                });
                regels.Add(regel);
            }

            return string.Join("\n", regels);
        }

        #endregion

        #region JSON Helpers

        private static string GetString(JsonElement item, string propertyName)
        {
            return item.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? ""
                : "";
        }

        private static decimal? GetDecimal(JsonElement item, string propertyName)
        {
            if (!item.TryGetProperty(propertyName, out var prop))
                return null;
            return prop.ValueKind == JsonValueKind.Number ? prop.GetDecimal() : null;
        }

        /// <summary>
        /// Formatteert een IBAN met spaties elke 4 tekens.
        /// "NL91ABNA0417164300" → "NL91 ABNA 0417 1643 00"
        /// </summary>
        private static string FormatIBAN(string? iban)
        {
            if (string.IsNullOrEmpty(iban))
                return "";
            var clean = iban.Replace(" ", "");
            return string.Join(" ", Enumerable.Range(0, (clean.Length + 3) / 4)
                .Select(i => clean.Substring(i * 4, Math.Min(4, clean.Length - i * 4))));
        }

        /// <summary>
        /// Converteert snake_case naar leesbare tekst.
        /// "doorlopend_krediet" → "Doorlopend krediet"
        /// </summary>
        private static string HumanizeSnakeCase(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            var result = value.Replace("_", " ").Trim();
            if (result.Length == 0) return "";
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        /// <summary>
        /// Resolvet de effectieve waarde: als hoofd-veld gelijk is aan de trigger (standaard "anders"),
        /// gebruik het anders-veld. Anders: HumanizeSnakeCase op het hoofd-veld.
        /// </summary>
        private static string ResolveEffectiveValue(JsonElement item, string field, string andersField, string triggerValue = "anders")
        {
            var value = GetString(item, field);
            if (string.Equals(value, triggerValue, StringComparison.OrdinalIgnoreCase))
            {
                var anders = GetString(item, andersField);
                return !string.IsNullOrEmpty(anders) ? anders : "";
            }
            return HumanizeSnakeCase(value);
        }

        /// <summary>
        /// Vertaalt tenaamstelling/partij codes naar leesbare namen.
        /// "partij1" → naam partij 1, "beiden"/"gezamenlijk" → "beide partijen", etc.
        /// </summary>
        private static string TranslateTenaamstelling(JsonElement item, string field, string andersField, DossierData data)
        {
            var code = GetString(item, field);
            if (string.IsNullOrEmpty(code))
                return "";

            if (string.Equals(code, "anders", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(code, "afwijken", StringComparison.OrdinalIgnoreCase))
            {
                var anders = GetString(item, andersField);
                return !string.IsNullOrEmpty(anders) ? anders : "";
            }

            return code.ToLowerInvariant() switch
            {
                "partij1" or "ouder_1" => GetPartijNaam(data.Partij1),
                "partij2" or "ouder_2" => GetPartijNaam(data.Partij2),
                "gezamenlijk" or "beiden" or "ouders_gezamenlijk" => "beide partijen",
                "kinderen_alle" => FormatKinderenNamen(data.Kinderen?.Where(k => k.Leeftijd.HasValue && k.Leeftijd.Value < 18), "alle minderjarige kinderen"),
                "kinderen_allemaal" => FormatKinderenNamen(data.Kinderen, "alle kinderen"),
                "aflossen" => "af te lossen",
                var l when l.StartsWith("kind_") => ResolveKindNaam(l, data),
                _ => HumanizeSnakeCase(code)
            };
        }

        private static string GetPartijNaam(PersonData? partij)
        {
            if (partij == null) return "";
            return DataFormatter.FormatFullName(partij.Voornamen, partij.Tussenvoegsel, partij.Achternaam);
        }

        private static string ResolveKindNaam(string code, DossierData data)
        {
            var idPart = code.Replace("kind_", "");
            if (int.TryParse(idPart, out var kindId))
            {
                var kind = data.Kinderen?.FirstOrDefault(k => k.DossierKindId == kindId || k.Id == kindId);
                if (kind != null)
                    return kind.Naam ?? "het kind";
            }
            return "het kind";
        }

        private static string FormatKinderenNamen(IEnumerable<ChildData>? kinderen, string fallback)
        {
            var namen = kinderen?
                .Select(k => k.Naam)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            if (namen != null && namen.Any())
                return DutchLanguageHelper.FormatList(namen!);

            return fallback;
        }

        /// <summary>
        /// Parst een ISO-datum string uit een JSON property en formatteert deze als Nederlandse datum.
        /// </summary>
        private static string FormatDateFromJson(JsonElement item, string propertyName)
        {
            var value = GetString(item, propertyName);
            if (string.IsNullOrEmpty(value)) return "";
            if (DateTime.TryParse(value, out var date))
                return DataFormatter.FormatDateDutchLong(date);
            return value;
        }

        private static string PrefixLidwoord(string bankNaam)
        {
            if (string.IsNullOrEmpty(bankNaam))
                return "";

            if (bankNaam.StartsWith("de ", StringComparison.OrdinalIgnoreCase) ||
                bankNaam.StartsWith("het ", StringComparison.OrdinalIgnoreCase))
                return bankNaam;

            return $"de {bankNaam}";
        }

        #endregion

        #region Per-Collection Mappers

        private static Dictionary<string, string> MapBankrekeningKinderen(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BANKREKENING_IBAN"] = FormatIBAN(GetString(item, "iban")),
                ["BANKREKENING_TENAAMSTELLING"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["BANKREKENING_BANKNAAM"] = PrefixLidwoord(GetString(item, "bankNaam"))
            };
        }

        private static Dictionary<string, string> MapBankrekening(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BANKREKENING_IBAN"] = FormatIBAN(GetString(item, "iban")),
                ["BANKREKENING_TENAAMSTELLING"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["BANKREKENING_BANKNAAM"] = PrefixLidwoord(GetString(item, "bankNaam")),
                ["BANKREKENING_SALDO"] = DataFormatter.FormatCurrency(GetDecimal(item, "saldo")),
                ["BANKREKENING_STATUS"] = HumanizeSnakeCase(GetString(item, "statusVermogen")),
                // Template-vriendelijke aliases (voor artikel templates die andere namen gebruiken)
                ["BANK_NAAM"] = GetString(item, "bankNaam"),
                ["REKENINGNUMMER"] = FormatIBAN(GetString(item, "iban")),
                ["SALDO"] = DataFormatter.FormatCurrency(GetDecimal(item, "saldo")),
                ["TOEGEDEELD_AAN"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["PEILDATUM"] = FormatDateFromJson(item, "datumSaldo")
            };
        }

        private static Dictionary<string, string> MapBelegging(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BELEGGING_SOORT"] = ResolveEffectiveValue(item, "soort", "soortAnders"),
                ["BELEGGING_INSTITUUT"] = ResolveEffectiveValue(item, "instituut", "instituutAnders"),
                ["BELEGGING_TENAAMSTELLING"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["BELEGGING_STATUS"] = HumanizeSnakeCase(GetString(item, "statusVermogen")),
                // Nieuwe velden
                ["BELEGGING_NUMMER"] = GetString(item, "nummer"),
                ["BELEGGING_WAARDE"] = DataFormatter.FormatCurrency(GetDecimal(item, "waarde")),
                // Template-vriendelijke aliases
                ["BELEGGING_INSTELLING"] = ResolveEffectiveValue(item, "instituut", "instituutAnders"),
                ["TOEGEDEELD_AAN"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["PEILDATUM"] = FormatDateFromJson(item, "datumWaarde")
            };
        }

        private static Dictionary<string, string> MapVoertuig(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VOERTUIG_SOORT"] = ResolveEffectiveValue(item, "soort", "soortAnders"),
                ["VOERTUIG_KENTEKEN"] = GetString(item, "kenteken"),
                ["VOERTUIG_TENAAMSTELLING"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["VOERTUIG_MERK"] = GetString(item, "merk"),
                ["VOERTUIG_MODEL"] = GetString(item, "handelsbenaming"),
                ["VOERTUIG_STATUS"] = HumanizeSnakeCase(GetString(item, "statusVermogen"))
            };
        }

        private static Dictionary<string, string> MapVerzekering(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VERZEKERING_SOORT"] = ResolveEffectiveValue(item, "soort", "soortAnders"),
                ["VERZEKERING_MAATSCHAPPIJ"] = ResolveEffectiveValue(item, "verzekeringsmaatschappij", "verzekeringsmaatschappijAnders"),
                ["VERZEKERING_NEMER"] = TranslateTenaamstelling(item, "verzekeringnemer", "verzekeringnemerAnders", data),
                ["VERZEKERING_STATUS"] = HumanizeSnakeCase(GetString(item, "statusVermogen"))
            };
        }

        private static Dictionary<string, string> MapSchuld(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SCHULD_SOORT"] = ResolveEffectiveValue(item, "soort", "soortAnders"),
                ["SCHULD_OMSCHRIJVING"] = GetString(item, "omschrijving"),
                ["SCHULD_BEDRAG"] = DataFormatter.FormatCurrency(GetDecimal(item, "bedrag")),
                ["SCHULD_TENAAMSTELLING"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["SCHULD_DRAAGPLICHTIG"] = TranslateTenaamstelling(item, "draagplichtig", "draagplichtigAnders", data),
                ["SCHULD_STATUS"] = HumanizeSnakeCase(GetString(item, "statusVermogen"))
            };
        }

        private static Dictionary<string, string> MapVordering(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["VORDERING_SOORT"] = ResolveEffectiveValue(item, "soort", "soortAnders"),
                ["VORDERING_OMSCHRIJVING"] = GetString(item, "omschrijving"),
                ["VORDERING_BEDRAG"] = DataFormatter.FormatCurrency(GetDecimal(item, "bedrag")),
                ["VORDERING_TENAAMSTELLING"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["VORDERING_STATUS"] = HumanizeSnakeCase(GetString(item, "statusVermogen"))
            };
        }

        private static Dictionary<string, string> MapPensioen(JsonElement item, DossierData data)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["PENSIOEN_MAATSCHAPPIJ"] = ResolveEffectiveValue(item, "pensioenmaatschappij", "pensioenmaatschappijAnders"),
                ["PENSIOEN_TENAAMSTELLING"] = TranslateTenaamstelling(item, "tenaamstelling", "tenaamstellingAnders", data),
                ["PENSIOEN_VERDELING"] = ResolveEffectiveValue(item, "verdeling", "verdelingAnders"),
                ["PENSIOEN_BIJZONDER_PARTNERPENSIOEN"] = ResolveEffectiveValue(item, "bijzonderPartnerpensioen", "bijzonderPartnerpensioensAnders", "afwijken")
            };
        }

        #endregion
    }
}

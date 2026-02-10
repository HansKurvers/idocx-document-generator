using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Verwerkt loop secties in artikel tekst.
    /// Syntax: [[#COLLECTIE]]...[[/COLLECTIE]]
    ///
    /// Gedrag:
    /// - Collectie leeg → blok verwijderen
    /// - Blok bevat per-kind variabelen (KIND_*) → itereren per kind
    /// - Blok bevat GEEN per-kind variabelen → blok eenmalig tonen (conditioneel gedrag)
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

                    var kinderen = ResolveCollection(collectieNaam, dossierData);

                    // Collectie leeg → blok verwijderen
                    if (kinderen == null || kinderen.Count == 0)
                        return "";

                    // Check of het blok per-kind variabelen bevat
                    var heeftKindVariabelen = KindVariabelPattern.IsMatch(blokInhoud);

                    if (heeftKindVariabelen)
                    {
                        // Itereren per kind
                        return ExpandPerKind(blokInhoud, kinderen, dossierData);
                    }
                    else
                    {
                        // Conditioneel: collectie is niet leeg, toon blok eenmalig
                        return blokInhoud.Trim();
                    }
                });
                iteration++;
            }

            // Verwijder opeenvolgende lege regels die kunnen ontstaan
            currentTekst = Regex.Replace(currentTekst, @"(\r?\n){3,}", "\n\n");

            return currentTekst;
        }

        /// <summary>
        /// Resolveert een collectienaam naar een lijst kinderen uit de dossierdata.
        /// </summary>
        private static List<ChildData>? ResolveCollection(string collectieNaam, DossierData dossierData)
        {
            var kinderen = dossierData.Kinderen;
            if (kinderen == null || kinderen.Count == 0)
                return null;

            switch (collectieNaam.ToUpperInvariant())
            {
                case "KINDEREN_UIT_HUWELIJK":
                    return FilterKinderenUitHuwelijk(kinderen, dossierData);

                case "KINDEREN_VOOR_HUWELIJK":
                    return FilterKinderenVoorHuwelijk(kinderen, dossierData);

                case "MINDERJARIGE_KINDEREN":
                    return kinderen.Where(k => k.Leeftijd.HasValue && k.Leeftijd.Value < 18).ToList();

                case "ALLE_KINDEREN":
                    return kinderen;

                default:
                    // Onbekende collectie → null (blok verwijderen)
                    return null;
            }
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
    }
}

using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.Artikel
{
    /// <summary>
    /// Service voor het verwerken van artikelen uit de bibliotheek
    /// Bevat logica voor conditionele filtering en placeholder vervanging
    /// </summary>
    public class ArtikelService : IArtikelService
    {
        private readonly ILogger<ArtikelService> _logger;
        private readonly IConditieEvaluator _conditieEvaluator;

        // Regex patronen voor conditionele blokken en placeholders
        private static readonly Regex IfEndIfPattern = new Regex(
            @"\[\[IF:(\w+)\]\](.*?)\[\[ENDIF:\1\]\]",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex PlaceholderPattern = new Regex(
            @"\[\[([^\]]+)\]\]",
            RegexOptions.IgnoreCase);

        public ArtikelService(ILogger<ArtikelService> logger, IConditieEvaluator conditieEvaluator)
        {
            _logger = logger;
            _conditieEvaluator = conditieEvaluator;
        }

        /// <summary>
        /// Filtert conditionele artikelen op basis van beschikbare data
        /// Prioriteit: conditieConfig (JSON) > conditieVeld (simpele string) > altijd zichtbaar
        /// </summary>
        public List<ArtikelData> FilterConditioneleArtikelen(
            List<ArtikelData> artikelen,
            Dictionary<string, string> replacements,
            DossierData? dossierData = null)
        {
            if (artikelen == null || artikelen.Count == 0)
                return new List<ArtikelData>();

            // Bouw evaluatie context als dossierData beschikbaar is (voor geavanceerde condities)
            Dictionary<string, object>? evaluationContext = null;
            if (dossierData != null)
            {
                evaluationContext = _conditieEvaluator.BuildEvaluationContext(dossierData, replacements);
            }

            var result = new List<ArtikelData>();

            foreach (var artikel in artikelen)
            {
                // Niet-conditionele artikelen altijd toevoegen
                if (!artikel.IsConditioneel)
                {
                    result.Add(artikel);
                    continue;
                }

                // Prioriteit 1: Geavanceerde conditieConfig (AND/OR JSON)
                if (artikel.ConditieConfig != null && evaluationContext != null)
                {
                    var zichtbaar = _conditieEvaluator.EvaluateConditie(artikel.ConditieConfig, evaluationContext);
                    if (zichtbaar)
                    {
                        result.Add(artikel);
                        _logger.LogDebug("Artikel '{Code}' toegevoegd (geavanceerde conditie is waar)", artikel.ArtikelCode);
                    }
                    else
                    {
                        _logger.LogDebug("Artikel '{Code}' gefilterd (geavanceerde conditie is onwaar)", artikel.ArtikelCode);
                    }
                    continue;
                }

                // Prioriteit 2: Simpele conditieVeld string
                if (!string.IsNullOrEmpty(artikel.ConditieVeld))
                {
                    if (EvalueerConditie(artikel.ConditieVeld, replacements))
                    {
                        result.Add(artikel);
                        _logger.LogDebug("Artikel '{Code}' toegevoegd (conditie '{Conditie}' is waar)", artikel.ArtikelCode, artikel.ConditieVeld);
                    }
                    else
                    {
                        _logger.LogDebug("Artikel '{Code}' gefilterd (conditie '{Conditie}' is onwaar)", artikel.ArtikelCode, artikel.ConditieVeld);
                    }
                    continue;
                }

                // Geen conditie gevonden maar IsConditioneel=true, voeg toe
                result.Add(artikel);
            }

            return result;
        }

        /// <summary>
        /// Vervangt [[Placeholder]] syntax met waarden uit replacements.
        /// Ondersteunt modifiers: [[caps:Placeholder]] voor hoofdletter aan begin van zin.
        /// </summary>
        public string VervangPlaceholders(string tekst, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(tekst))
                return tekst;

            return PlaceholderPattern.Replace(tekst, match =>
            {
                var placeholder = match.Groups[1].Value;

                // Controleer op modifier prefix (bijv. "caps:Placeholder")
                var modifier = ExtractModifier(ref placeholder);

                // Probeer exacte match
                if (replacements.TryGetValue(placeholder, out var value))
                    return ApplyModifier(value, modifier);

                // Probeer case-insensitive match
                var key = replacements.Keys.FirstOrDefault(k =>
                    k.Equals(placeholder, StringComparison.OrdinalIgnoreCase));

                if (key != null)
                    return ApplyModifier(replacements[key], modifier);

                // Placeholder niet gevonden, laat staan voor debugging
                _logger.LogWarning($"Placeholder niet gevonden: [[{placeholder}]]");
                return match.Value;
            });
        }

        /// <summary>
        /// Extraheert een modifier prefix uit de placeholder naam.
        /// Bijv. "caps:Partij1Benaming" → modifier="caps", placeholder wordt "Partij1Benaming"
        /// </summary>
        private static string? ExtractModifier(ref string placeholder)
        {
            var colonIndex = placeholder.IndexOf(':');
            if (colonIndex > 0)
            {
                var prefix = placeholder.Substring(0, colonIndex).ToLowerInvariant();
                if (prefix == "caps" || prefix == "upper" || prefix == "lower")
                {
                    placeholder = placeholder.Substring(colonIndex + 1);
                    return prefix;
                }
            }
            return null;
        }

        /// <summary>
        /// Past de modifier toe op een waarde.
        /// - caps: eerste letter hoofdletter (voor begin van zin)
        /// - upper: alles hoofdletters
        /// - lower: alles kleine letters
        /// </summary>
        private static string ApplyModifier(string value, string? modifier)
        {
            if (modifier == null || string.IsNullOrEmpty(value))
                return value;

            return modifier switch
            {
                "caps" => char.ToUpper(value[0]) + value.Substring(1),
                "upper" => value.ToUpperInvariant(),
                "lower" => value.ToLowerInvariant(),
                _ => value
            };
        }

        /// <summary>
        /// Verwerkt [[IF:Veld]]...[[ENDIF:Veld]] blokken binnen tekst
        /// </summary>
        public string VerwerkConditioneleBlokken(string tekst, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(tekst))
                return tekst;

            // Blijf verwerken totdat er geen matches meer zijn (voor geneste blokken)
            var previousTekst = "";
            var currentTekst = tekst;
            int maxIterations = 10; // Voorkom oneindige loops
            int iteration = 0;

            while (currentTekst != previousTekst && iteration < maxIterations)
            {
                previousTekst = currentTekst;
                currentTekst = IfEndIfPattern.Replace(currentTekst, match =>
                {
                    var veldNaam = match.Groups[1].Value;
                    var inhoud = match.Groups[2].Value;

                    if (EvalueerConditie(veldNaam, replacements))
                    {
                        // Conditie waar: behoud inhoud (zonder tags)
                        return inhoud.Trim();
                    }
                    else
                    {
                        // Conditie onwaar: verwijder hele blok
                        return "";
                    }
                });
                iteration++;
            }

            // Verwijder lege regels die kunnen ontstaan na verwijdering van conditionele blokken
            currentTekst = Regex.Replace(currentTekst, @"(\r?\n){3,}", "\n\n");

            return currentTekst.Trim();
        }

        /// <summary>
        /// Past alle transformaties toe op een artikel tekst
        /// </summary>
        public string VerwerkArtikelTekst(ArtikelData artikel, Dictionary<string, string> replacements)
        {
            var tekst = artikel.EffectieveTekst;

            // 1. Verwerk eerst conditionele blokken
            tekst = VerwerkConditioneleBlokken(tekst, replacements);

            // 2. Vervang daarna placeholders
            tekst = VervangPlaceholders(tekst, replacements);

            return tekst;
        }

        /// <summary>
        /// Past alle transformaties toe inclusief loop secties
        /// </summary>
        public string VerwerkArtikelTekst(ArtikelData artikel, Dictionary<string, string> replacements, DossierData? dossierData)
        {
            var tekst = artikel.EffectieveTekst;

            // 1. Verwerk eerst loop secties (expandeer collecties)
            tekst = LoopSectionProcessor.Process(tekst, dossierData);

            // 2. Verwerk conditionele blokken
            tekst = VerwerkConditioneleBlokken(tekst, replacements);

            // 3. Vervang daarna placeholders
            tekst = VervangPlaceholders(tekst, replacements);

            return tekst;
        }

        /// <summary>
        /// Evalueert of een conditie waar is op basis van de replacements
        /// Ondersteunt:
        /// - "VeldNaam" → veld heeft een waarde (niet leeg)
        /// - "!VeldNaam" → veld is leeg
        /// - "VeldNaam=waarde" → veld is gelijk aan waarde
        /// - "VeldNaam!=waarde" → veld is niet gelijk aan waarde
        /// </summary>
        private bool EvalueerConditie(string conditie, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(conditie))
                return true;

            // Check voor != operator (moet voor = komen vanwege string matching)
            if (conditie.Contains("!="))
            {
                var parts = conditie.Split(new[] { "!=" }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var veldNaam = parts[0].Trim();
                    var verwachteWaarde = parts[1].Trim();
                    var actueleWaarde = GetWaarde(veldNaam, replacements);
                    return !string.Equals(actueleWaarde, verwachteWaarde, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Check voor = operator
            if (conditie.Contains("="))
            {
                var parts = conditie.Split(new[] { "=" }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var veldNaam = parts[0].Trim();
                    var verwachteWaarde = parts[1].Trim();
                    var actueleWaarde = GetWaarde(veldNaam, replacements);
                    return string.Equals(actueleWaarde, verwachteWaarde, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Bestaande NOT operator logica (bijv. "!HeeftKinderrekening")
            bool isNegated = conditie.StartsWith("!");
            var veldNaamSimple = isNegated ? conditie.Substring(1) : conditie;

            // Zoek waarde in replacements
            var heeftWaarde = HeeftWaarde(veldNaamSimple, replacements);

            return isNegated ? !heeftWaarde : heeftWaarde;
        }

        /// <summary>
        /// Haalt de waarde van een veld op uit de replacements dictionary
        /// </summary>
        private string GetWaarde(string veldNaam, Dictionary<string, string> replacements)
        {
            // Exacte match
            if (replacements.TryGetValue(veldNaam, out var value))
                return value ?? "";

            // Case-insensitive match
            var key = replacements.Keys.FirstOrDefault(k =>
                k.Equals(veldNaam, StringComparison.OrdinalIgnoreCase));

            return key != null ? replacements[key] ?? "" : "";
        }

        /// <summary>
        /// Controleert of een veld een niet-lege waarde heeft
        /// </summary>
        private bool HeeftWaarde(string veldNaam, Dictionary<string, string> replacements)
        {
            // Exacte match
            if (replacements.TryGetValue(veldNaam, out var value))
            {
                return !string.IsNullOrWhiteSpace(value) && value != "0" && value.ToLower() != "false";
            }

            // Case-insensitive match
            var key = replacements.Keys.FirstOrDefault(k =>
                k.Equals(veldNaam, StringComparison.OrdinalIgnoreCase));

            if (key != null)
            {
                var val = replacements[key];
                return !string.IsNullOrWhiteSpace(val) && val != "0" && val.ToLower() != "false";
            }

            return false;
        }
    }
}

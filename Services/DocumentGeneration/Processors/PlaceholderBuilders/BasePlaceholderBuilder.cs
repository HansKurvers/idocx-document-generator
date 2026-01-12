using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Base class met gedeelde helper methods voor placeholder builders.
    /// </summary>
    public abstract class BasePlaceholderBuilder : IPlaceholderBuilder
    {
        protected readonly ILogger _logger;

        protected BasePlaceholderBuilder(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// De volgorde waarin deze builder wordt uitgevoerd.
        /// </summary>
        public abstract int Order { get; }

        /// <summary>
        /// Voegt placeholders toe aan de replacements dictionary.
        /// </summary>
        public abstract void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules);

        /// <summary>
        /// Voegt een placeholder toe met null-check.
        /// </summary>
        protected void AddPlaceholder(
            Dictionary<string, string> replacements,
            string key,
            string? value)
        {
            replacements[key] = value ?? string.Empty;
        }

        /// <summary>
        /// Voegt een placeholder toe met fallback waarde.
        /// </summary>
        protected void AddPlaceholderWithFallback(
            Dictionary<string, string> replacements,
            string key,
            string? value,
            string fallback)
        {
            replacements[key] = !string.IsNullOrEmpty(value) ? value : fallback;
        }

        /// <summary>
        /// Get appropriate text for children based on count and names
        /// </summary>
        protected string GetKinderenTekst(List<ChildData> kinderen)
        {
            if (kinderen.Count == 0)
                return "De kinderen";

            if (kinderen.Count == 1)
            {
                return kinderen[0].Roepnaam ?? kinderen[0].Voornamen ?? "Het kind";
            }

            var roepnamen = kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList();
            return DutchLanguageHelper.FormatList(roepnamen);
        }

        /// <summary>
        /// Gets the appropriate designation for a party based on gender.
        /// Always returns "de vader" or "de moeder" based on gender.
        /// If useRoepnaam is true, returns roepnaam instead.
        /// </summary>
        protected string GetPartijBenaming(PersonData? person, bool? useRoepnaam)
        {
            if (person == null) return "";

            // If explicitly requested to use roepnaam
            if (useRoepnaam == true)
            {
                return person.Naam; // Uses existing property that handles roepnaam fallback
            }

            // Default: use parental role-based designation (de vader/de moeder)
            var geslacht = person.Geslacht?.Trim().ToLowerInvariant();
            return geslacht switch
            {
                "m" or "man" => "de vader",
                "v" or "vrouw" => "de moeder",
                _ => person.Naam // Fallback to roepnaam for unknown gender
            };
        }

        /// <summary>
        /// Get bezittelijk voornaamwoord based on gender
        /// </summary>
        protected string GetBezittelijkVoornaamwoord(string? geslacht)
        {
            return geslacht?.ToLowerInvariant() switch
            {
                "man" or "m" or "jongen" => "zijn",
                "vrouw" or "v" or "meisje" => "haar",
                _ => "zijn/haar"  // fallback als geslacht onbekend is
            };
        }

        /// <summary>
        /// Get bezittelijk voornaamwoord for children (singular/plural aware)
        /// </summary>
        protected string GetKinderenBezittelijkVoornaamwoord(List<ChildData> kinderen)
        {
            if (kinderen.Count == 0)
                return "hun";

            if (kinderen.Count == 1)
                return GetBezittelijkVoornaamwoord(kinderen[0].Geslacht);

            return "hun";
        }

        /// <summary>
        /// Gets the roepnaam with tussenvoegsel and achternaam for a child
        /// Example: "Jan de Vries" (when roepnaam is "Jan")
        /// </summary>
        protected string GetKindRoepnaamAchternaam(ChildData? child)
        {
            if (child == null) return "";

            var parts = new List<string>();

            // Use roepnaam, or fall back to first name from voornamen, or fall back to achternaam
            var roepnaam = child.Roepnaam ?? child.Voornamen?.Split(' ').FirstOrDefault() ?? "";
            if (!string.IsNullOrWhiteSpace(roepnaam))
                parts.Add(roepnaam.Trim());

            if (!string.IsNullOrWhiteSpace(child.Tussenvoegsel))
                parts.Add(child.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(child.Achternaam))
                parts.Add(child.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Gets the full name with middle name (tussenvoegsel): voornamen + tussenvoegsel + achternaam
        /// Example: "Jan Peter de Vries"
        /// </summary>
        protected string GetVolledigeNaamMetTussenvoegsel(PersonData? person)
        {
            if (person == null) return "";

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(person.Voornamen))
                parts.Add(person.Voornamen.Trim());

            if (!string.IsNullOrWhiteSpace(person.Tussenvoegsel))
                parts.Add(person.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(person.Achternaam))
                parts.Add(person.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Gets the full last name with middle name (tussenvoegsel): tussenvoegsel + achternaam
        /// Example: "de Vries"
        /// </summary>
        protected string GetVolledigeAchternaam(PersonData? person)
        {
            if (person == null) return "";

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(person.Tussenvoegsel))
                parts.Add(person.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(person.Achternaam))
                parts.Add(person.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Gets voorletters + tussenvoegsel + achternaam
        /// Example: "J.P. de Vries" (for Jan Peter de Vries)
        /// </summary>
        protected string GetVoorlettersAchternaam(PersonData? person)
        {
            if (person == null) return "";

            var parts = new List<string>();

            // Use voorletters if available, otherwise create from voornamen
            if (!string.IsNullOrWhiteSpace(person.Voorletters))
            {
                parts.Add(person.Voorletters.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(person.Voornamen))
            {
                // Create voorletters from voornamen if not available
                var voorletters = string.Join(".", person.Voornamen
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.FirstOrDefault())
                    .Where(c => c != default(char))) + ".";
                parts.Add(voorletters);
            }

            if (!string.IsNullOrWhiteSpace(person.Tussenvoegsel))
                parts.Add(person.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(person.Achternaam))
                parts.Add(person.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Get party designation (de vader/de moeder) based on party number
        /// </summary>
        protected string GetPartijNaam(int? partijNummer, PersonData? partij1, PersonData? partij2)
        {
            var persoon = partijNummer switch
            {
                1 => partij1,
                2 => partij2,
                _ => null
            };
            return GetPartijBenaming(persoon, false); // Always use benaming, not roepnaam
        }

        /// <summary>
        /// Gets the party designation (de vader/de moeder) based on the party identifier
        /// </summary>
        protected string GetPartyName(string? partyIdentifier, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(partyIdentifier))
                return "";

            var persoon = partyIdentifier.ToLower() switch
            {
                "partij1" => partij1,
                "partij2" => partij2,
                _ => null
            };
            return GetPartijBenaming(persoon, false);
        }

        /// <summary>
        /// Gets the party designation (de vader/de moeder) or "Kinderrekening" based on the party identifier
        /// </summary>
        protected string GetPartyNameOrKinderrekening(string? partyIdentifier, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(partyIdentifier))
                return "";

            if (partyIdentifier.ToLower() == "kinderrekening")
                return "Kinderrekening";

            var persoon = partyIdentifier.ToLower() switch
            {
                "partij1" => partij1,
                "partij2" => partij2,
                _ => null
            };
            return GetPartijBenaming(persoon, false);
        }

        /// <summary>
        /// Formats a list of kostensoorten as a bulleted list
        /// </summary>
        protected string FormatKostensoortenList(List<string> kostensoorten)
        {
            if (kostensoorten == null || !kostensoorten.Any())
                return "";

            return string.Join("\n", kostensoorten.Select(k => $"- {k}"));
        }
    }
}

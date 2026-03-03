using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Builder voor partij (ouder) placeholders.
    /// Verantwoordelijk voor alle Partij1* en Partij2* placeholders.
    /// </summary>
    public class PartijPlaceholderBuilder : BasePlaceholderBuilder
    {
        public override int Order => 20;

        public PartijPlaceholderBuilder(ILogger<PartijPlaceholderBuilder> logger)
            : base(logger)
        {
        }

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogDebug("Building partij placeholders for dossier {DossierId}", data.Id);

            if (data.Partij1 != null)
            {
                AddPersonReplacements(replacements, "Partij1", data.Partij1, data.IsAnoniem);
            }

            if (data.Partij2 != null)
            {
                AddPersonReplacements(replacements, "Partij2", data.Partij2, data.IsAnoniem);
            }

            // Partij aanduiding: altijd beschikbaar, ongeacht document type
            BuildPartijAanduidingPlaceholders(replacements, data);

            _logger.LogDebug("Added partij placeholders for Partij1={P1} and Partij2={P2}",
                data.Partij1?.VolledigeNaam ?? "null",
                data.Partij2?.VolledigeNaam ?? "null");
        }

        /// <summary>
        /// Add person-related replacements for a party
        /// </summary>
        private void AddPersonReplacements(
            Dictionary<string, string> replacements,
            string prefix,
            PersonData person,
            bool? isAnoniem)
        {
            // Basic name fields
            AddPlaceholder(replacements, $"{prefix}Naam", person.VolledigeNaam);
            AddPlaceholder(replacements, $"{prefix}Voornaam", person.Voornamen);
            AddPlaceholder(replacements, $"{prefix}Roepnaam",
                person.Roepnaam ?? person.Voornamen?.Split(' ')[0]);
            AddPlaceholder(replacements, $"{prefix}Achternaam", person.Achternaam);
            AddPlaceholder(replacements, $"{prefix}Tussenvoegsel", person.Tussenvoegsel);

            // Address fields
            AddPlaceholder(replacements, $"{prefix}Adres", person.Adres);
            AddPlaceholder(replacements, $"{prefix}Postcode", person.Postcode);
            AddPlaceholder(replacements, $"{prefix}Plaats", person.Plaats);
            AddPlaceholder(replacements, $"{prefix}Geboorteplaats", person.GeboortePlaats);

            // Contact fields
            AddPlaceholder(replacements, $"{prefix}Telefoon", person.Telefoon);
            AddPlaceholder(replacements, $"{prefix}Email", person.Email);
            AddPlaceholder(replacements, $"{prefix}Geboortedatum", DataFormatter.FormatDate(person.GeboorteDatum));

            // Gender
            AddPlaceholder(replacements, $"{prefix}Geslacht", FormatGeslacht(person.Geslacht));

            // Combined address
            AddPlaceholder(replacements, $"{prefix}VolledigAdres",
                DataFormatter.FormatAddress(person.Adres, person.Postcode, person.Plaats));

            // Full name with middle name (tussenvoegsel)
            AddPlaceholder(replacements, $"{prefix}VolledigeNaamMetTussenvoegsel",
                GetVolledigeNaamMetTussenvoegsel(person));

            // Full last name with middle name
            AddPlaceholder(replacements, $"{prefix}VolledigeAchternaam",
                GetVolledigeAchternaam(person));

            // Benaming placeholder (contextual party designation)
            // Anoniem: de vader/de moeder, Niet-anoniem: roepnaam
            var benaming = GetBenaming(person, isAnoniem == true);
            AddPlaceholder(replacements, $"{prefix}Benaming", benaming);
            AddPlaceholder(replacements, $"{prefix}BenamingHoofdletter", Capitalize(benaming));

            // Voorletters + tussenvoegsel + achternaam
            AddPlaceholder(replacements, $"{prefix}VoorlettersAchternaam",
                GetVoorlettersAchternaam(person));

            // Nationaliteit (basisvorm en bijvoeglijke vorm)
            AddPlaceholder(replacements, $"{prefix}Nationaliteit1", person.Nationaliteit1);
            AddPlaceholder(replacements, $"{prefix}Nationaliteit2", person.Nationaliteit2);
            AddPlaceholder(replacements, $"{prefix}Nationaliteit1Bijvoeglijk",
                DutchLanguageHelper.ToNationalityAdjective(person.Nationaliteit1));
            AddPlaceholder(replacements, $"{prefix}Nationaliteit2Bijvoeglijk",
                DutchLanguageHelper.ToNationalityAdjective(person.Nationaliteit2));
        }

        /// <summary>
        /// Gets the party designation based on anonymity setting.
        /// Anoniem: de vader/de moeder (based on gender)
        /// Niet-anoniem: roepnaam
        /// </summary>
        private string GetBenaming(PersonData person, bool isAnoniem)
        {
            if (isAnoniem)
            {
                // Anonymous: use de vader / de moeder
                var geslacht = person.Geslacht?.Trim().ToLowerInvariant();
                return geslacht switch
                {
                    "m" or "man" => "de vader",
                    "v" or "vrouw" => "de moeder",
                    _ => "de ouder"
                };
            }
            else
            {
                // Not anonymous: use roepnaam
                return person.Roepnaam ?? person.Voornamen?.Split(' ')[0] ?? "";
            }
        }

        /// <summary>
        /// Builds partij aanduiding placeholders based on IsAnoniem setting.
        /// For convenant/ouderschapsplan: de man/de vrouw when anonymous, roepnaam + achternaam when named.
        /// Also creates capitalized variants for use at the start of sentences.
        /// </summary>
        private void BuildPartijAanduidingPlaceholders(Dictionary<string, string> replacements, DossierData data)
        {
            var partij1 = data.Partij1;
            var partij2 = data.Partij2;
            var isAnoniem = data.IsAnoniem == true;

            // If both parties have the same gender, always use names to avoid ambiguity
            // ("de man" / "de man" is not distinguishable)
            var zelfdeGeslacht = partij1 != null && partij2 != null &&
                NormalizeGeslacht(partij1.Geslacht) == NormalizeGeslacht(partij2.Geslacht);

            // Determine aanduidingen based on anonymity
            string partij1Aanduiding, partij2Aanduiding;

            if (isAnoniem && !zelfdeGeslacht)
            {
                // Anonymous with different genders: use de man / de vrouw
                partij1Aanduiding = GetGeslachtAanduiding(partij1?.Geslacht);
                partij2Aanduiding = GetGeslachtAanduiding(partij2?.Geslacht);
            }
            else
            {
                // Named, or same gender (always use names to avoid ambiguity)
                partij1Aanduiding = GetRoepnaamAchternaam(partij1);
                partij2Aanduiding = GetRoepnaamAchternaam(partij2);
            }

            // Regular (lowercase for mid-sentence use)
            AddPlaceholder(replacements, "PARTIJ1_AANDUIDING", partij1Aanduiding);
            AddPlaceholder(replacements, "PARTIJ2_AANDUIDING", partij2Aanduiding);

            // Capitalized (for start of sentence)
            AddPlaceholder(replacements, "PARTIJ1_AANDUIDING_HOOFDLETTER", Capitalize(partij1Aanduiding));
            AddPlaceholder(replacements, "PARTIJ2_AANDUIDING_HOOFDLETTER", Capitalize(partij2Aanduiding));
        }

        private static string NormalizeGeslacht(string? geslacht)
        {
            var g = geslacht?.Trim().ToLowerInvariant();
            return g switch
            {
                "m" or "man" => "m",
                "v" or "vrouw" => "v",
                _ => "x"
            };
        }

        private string GetGeslachtAanduiding(string? geslacht)
        {
            var g = geslacht?.Trim().ToLowerInvariant();

            return g switch
            {
                "m" or "man" => "de man",
                "v" or "vrouw" => "de vrouw",
                _ => "de partij"
            };
        }

        /// <summary>
        /// Gets roepnaam + tussenvoegsel + achternaam for a person.
        /// Example: "Jan de Vries"
        /// </summary>
        private string GetRoepnaamAchternaam(PersonData? person)
        {
            if (person == null) return "";

            var parts = new List<string>();

            // Use roepnaam, or fall back to first name from voornamen
            var roepnaam = person.Roepnaam ?? person.Voornamen?.Split(' ').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(roepnaam))
                parts.Add(roepnaam.Trim());

            if (!string.IsNullOrWhiteSpace(person.Tussenvoegsel))
                parts.Add(person.Tussenvoegsel.Trim());

            if (!string.IsNullOrWhiteSpace(person.Achternaam))
                parts.Add(person.Achternaam.Trim());

            return string.Join(" ", parts);
        }

        private static string FormatGeslacht(string? geslacht)
        {
            var g = geslacht?.Trim().ToLowerInvariant();
            return g switch
            {
                "m" or "man" => "Man",
                "v" or "vrouw" => "Vrouw",
                _ => "Anders"
            };
        }

        private string Capitalize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}

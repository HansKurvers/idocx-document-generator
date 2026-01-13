using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Builder voor relatie, gezag en woonplaats placeholders.
    /// Verantwoordelijk voor OuderschapsplanInfo gerelateerde placeholders.
    /// </summary>
    public class RelatiePlaceholderBuilder : BasePlaceholderBuilder
    {
        public override int Order => 40;

        public RelatiePlaceholderBuilder(ILogger<RelatiePlaceholderBuilder> logger)
            : base(logger)
        {
        }

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogDebug("Building relatie placeholders for dossier {DossierId}", data.Id);

            var kinderen = data.Kinderen ?? new List<ChildData>();

            if (data.OuderschapsplanInfo != null)
            {
                AddOuderschapsplanInfoReplacements(
                    replacements,
                    data.OuderschapsplanInfo,
                    data.Partij1,
                    data.Partij2,
                    kinderen,
                    data);
            }
            else
            {
                // Add default values for required placeholders when no OuderschapsplanInfo
                AddDefaultReplacements(replacements, kinderen);
            }
        }

        private void AddOuderschapsplanInfoReplacements(
            Dictionary<string, string> replacements,
            OuderschapsplanInfoData info,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen,
            DossierData data)
        {
            // Basic relationship info
            AddPlaceholder(replacements, "SoortRelatie", info.SoortRelatie);
            AddPlaceholder(replacements, "DatumAanvangRelatie", DataFormatter.FormatDate(info.DatumAanvangRelatie));
            AddPlaceholder(replacements, "PlaatsRelatie", info.PlaatsRelatie);
            AddPlaceholder(replacements, "BetrokkenheidKind", info.BetrokkenheidKind);
            AddPlaceholder(replacements, "Kiesplan", info.Kiesplan);
            AddPlaceholder(replacements, "KiesplanZin", GetKiesplanZin(info.Kiesplan, kinderen));

            // Derived placeholders
            AddPlaceholder(replacements, "SoortRelatieVoorwaarden", GetRelatieVoorwaarden(info.SoortRelatie));
            AddPlaceholder(replacements, "SoortRelatieVerbreking", GetRelatieVerbreking(info.SoortRelatie));

            // Generated sentences
            AddPlaceholder(replacements, "RelatieAanvangZin",
                GetRelatieAanvangZin(info.SoortRelatie, info.DatumAanvangRelatie, info.PlaatsRelatie));
            AddPlaceholder(replacements, "OuderschapsplanDoelZin",
                GetOuderschapsplanDoelZin(info.SoortRelatie, kinderen.Count));

            // Gezag (parental authority)
            var gezagRegeling = GetGezagRegeling(info.GezagPartij, info.GezagTermijnWeken, partij1, partij2, kinderen);
            AddPlaceholder(replacements, "GezagRegeling", gezagRegeling);
            AddPlaceholder(replacements, "GezagZin", gezagRegeling); // Alias
            AddPlaceholder(replacements, "GezagPartij", info.GezagPartij?.ToString());
            AddPlaceholder(replacements, "GezagTermijnWeken", info.GezagTermijnWeken?.ToString());

            // Woonplaats (residence)
            AddPlaceholder(replacements, "WoonplaatsRegeling",
                GetWoonplaatsRegeling(info.WoonplaatsOptie, info.WoonplaatsPartij1, info.WoonplaatsPartij2,
                    partij1, partij2, info.SoortRelatie));
            AddPlaceholder(replacements, "WoonplaatsOptie", info.WoonplaatsOptie?.ToString());
            AddPlaceholder(replacements, "WoonplaatsPartij1", info.WoonplaatsPartij1);
            AddPlaceholder(replacements, "WoonplaatsPartij2", info.WoonplaatsPartij2);
            AddPlaceholder(replacements, "HuidigeWoonplaatsPartij1", partij1?.Plaats);
            AddPlaceholder(replacements, "HuidigeWoonplaatsPartij2", partij2?.Plaats);

            // Party choices
            AddPlaceholder(replacements, "WaOpNaamVan", GetPartijNaam(info.WaOpNaamVanPartij, partij1, partij2));
            AddPlaceholder(replacements, "ZorgverzekeringOpNaamVan", GetPartijNaam(info.ZorgverzekeringOpNaamVanPartij, partij1, partij2));
            AddPlaceholder(replacements, "KinderbijslagOntvanger", GetKinderbijslagOntvanger(info.KinderbijslagPartij, partij1, partij2));

            // Other fields
            AddPlaceholder(replacements, "KeuzeDevices", info.KeuzeDevices);
            AddPlaceholder(replacements, "Hoofdverblijf", GetHoofdverblijfText(info.Hoofdverblijf, partij1, partij2, kinderen, data.IsAnoniem));
            AddPlaceholder(replacements, "Zorgverdeling", info.Zorgverdeling);
            AddPlaceholder(replacements, "OpvangKinderen", info.OpvangKinderen);
            AddPlaceholder(replacements, "BankrekeningnummersKind", info.BankrekeningnummersOpNaamVanKind);
            AddPlaceholder(replacements, "ParentingCoordinator", info.ParentingCoordinator);

            _logger.LogDebug("Added relatie placeholders: SoortRelatie={Relatie}, GezagPartij={Gezag}",
                info.SoortRelatie, info.GezagPartij);
        }

        private void AddDefaultReplacements(Dictionary<string, string> replacements, List<ChildData> kinderen)
        {
            // Default relationship sentence
            AddPlaceholder(replacements, "RelatieAanvangZin", "Wij hebben een relatie met elkaar gehad.");

            // Default parenting plan purpose sentence
            var kindTekst = kinderen.Count == 1 ? "ons kind" : "onze kinderen";
            AddPlaceholder(replacements, "OuderschapsplanDoelZin",
                $"In dit ouderschapsplan hebben we afspraken gemaakt over {kindTekst}.");

            // Default gezag text with actual children names
            string kinderenNamen;
            if (kinderen.Count == 0)
            {
                kinderenNamen = "de kinderen";
            }
            else if (kinderen.Count == 1)
            {
                kinderenNamen = kinderen[0].Roepnaam ?? kinderen[0].Voornamen ?? "het kind";
            }
            else
            {
                kinderenNamen = DutchLanguageHelper.FormatList(
                    kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());
            }
            var defaultGezagText = $"De ouders hebben gezamenlijk gezag over {kinderenNamen}.";
            AddPlaceholder(replacements, "GezagZin", defaultGezagText);
            AddPlaceholder(replacements, "GezagRegeling", defaultGezagText);

            // Default woonplaats text
            AddPlaceholder(replacements, "WoonplaatsRegeling",
                "Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij uit elkaar gaan.");

            // Default hoofdverblijf text
            AddPlaceholder(replacements, "Hoofdverblijf", "");
        }

        #region Helper Methods

        private string GetRelatieVoorwaarden(string? soortRelatie)
        {
            if (string.IsNullOrEmpty(soortRelatie))
                return "";

            return soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => "huwelijkse voorwaarden",
                "geregistreerd_partnerschap" => "partnerschapsvoorwaarden",
                "samenwonend" => "samenlevingsovereenkomst",
                _ => "overeenkomst"
            };
        }

        private string GetRelatieVerbreking(string? soortRelatie)
        {
            if (string.IsNullOrEmpty(soortRelatie))
                return "";

            return soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => "echtscheiding",
                "geregistreerd_partnerschap" => "ontbinding van het geregistreerd partnerschap",
                "samenwonend" => "beëindiging van de samenleving",
                _ => ""
            };
        }

        private string GetRelatieAanvangZin(string? soortRelatie, DateTime? datumAanvangRelatie, string? plaatsRelatie)
        {
            if (string.IsNullOrEmpty(soortRelatie))
            {
                return "Wij hebben een relatie met elkaar gehad.";
            }

            var datum = DataFormatter.FormatDate(datumAanvangRelatie);
            var plaats = !string.IsNullOrEmpty(plaatsRelatie) ? $" te {plaatsRelatie}" : "";

            return soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => $"Wij zijn op {datum}{plaats} met elkaar gehuwd.",
                "geregistreerd_partnerschap" => $"Wij zijn op {datum}{plaats} met elkaar een geregistreerd partnerschap aangegaan.",
                "samenwonend" => "Wij hebben een affectieve relatie gehad.",
                "lat_relatie" or "lat-relatie" => "Wij hebben een affectieve relatie gehad.",
                "ex_partners" or "ex-partners" => "Wij hebben een affectieve relatie gehad.",
                "anders" => "Wij hebben een affectieve relatie gehad.",
                _ => "Wij hebben een affectieve relatie gehad."
            };
        }

        private string GetOuderschapsplanDoelZin(string? soortRelatie, int aantalKinderen)
        {
            var kindTekst = aantalKinderen == 1 ? "ons kind" : "onze kinderen";

            if (string.IsNullOrEmpty(soortRelatie))
            {
                return $"In dit ouderschapsplan hebben we afspraken gemaakt over {kindTekst}.";
            }

            var redenTekst = soortRelatie.ToLowerInvariant() switch
            {
                "gehuwd" => " omdat we gaan scheiden",
                "geregistreerd_partnerschap" => " omdat we ons geregistreerd partnerschap willen laten ontbinden",
                "samenwonend" => " omdat we onze samenleving willen beëindigen",
                _ => ""
            };

            return $"In dit ouderschapsplan hebben we afspraken gemaakt over {kindTekst}{redenTekst}.";
        }

        private string GetGezagRegeling(
            int? gezagPartij,
            int? gezagTermijnWeken,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen)
        {
            if (kinderen.Count == 0)
                return "";

            // Default to shared custody if gezag_partij is not set
            if (!gezagPartij.HasValue)
            {
                var defaultKinderenTekst = kinderen.Count == 1
                    ? kinderen[0].Roepnaam ?? kinderen[0].Voornamen ?? "het kind"
                    : DutchLanguageHelper.FormatList(kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());
                return $"De ouders hebben gezamenlijk gezag over {defaultKinderenTekst}.";
            }

            var partij1Naam = GetPartijBenaming(partij1, false);
            var partij2Naam = GetPartijBenaming(partij2, false);

            var kinderenTekst = kinderen.Count == 1
                ? kinderen[0].Roepnaam ?? kinderen[0].Voornamen ?? "het kind"
                : DutchLanguageHelper.FormatList(kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var weken = gezagTermijnWeken ?? 2;

            return gezagPartij.Value switch
            {
                1 => $"{Capitalize(partij1Naam)} en {partij2Naam} hebben samen het ouderlijk gezag over {kinderenTekst}. Na de scheiding blijft dit zo.",
                2 => $"{Capitalize(partij1Naam)} heeft alleen het ouderlijk gezag over {kinderenTekst}. Dit blijft zo.",
                3 => $"{Capitalize(partij2Naam)} heeft alleen het ouderlijk gezag over {kinderenTekst}. Dit blijft zo.",
                4 => $"{Capitalize(partij1Naam)} heeft alleen het ouderlijk gezag over {kinderenTekst}. Partijen spreken af dat zij binnen {weken} weken na ondertekening van dit ouderschapsplan gezamenlijk gezag zullen regelen.",
                5 => $"{Capitalize(partij2Naam)} heeft alleen het ouderlijk gezag over {kinderenTekst}. Partijen spreken af dat zij binnen {weken} weken na ondertekening van dit ouderschapsplan gezamenlijk gezag zullen regelen.",
                _ => ""
            };
        }

        private string GetKiesplanZin(string? kiesplan, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(kiesplan) || kiesplan == "nee")
                return "";

            if (kinderen.Count == 0)
                return "";

            var kinderenNamen = DutchLanguageHelper.FormatList(
                kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var isEnkelvoud = kinderen.Count == 1;
            var isZijn = isEnkelvoud ? "is" : "zijn";
            var heeftHebben = isEnkelvoud ? "heeft" : "hebben";
            var hunZijnHaar = isEnkelvoud
                ? GetBezittelijkVoornaamwoord(kinderen[0].Geslacht)
                : "hun";

            var kindplanTekst = isEnkelvoud
                ? "Het door ons ondertekende KIES Kindplan is"
                : "De door ons ondertekende KIES Kindplannen zijn";

            return kiesplan.ToLowerInvariant() switch
            {
                "kindplan" => $"Bij het maken van de afspraken in dit ouderschapsplan hebben we {kinderenNamen} gevraagd een KIES Kindplan te maken dat door ons is ondertekend, zodat wij rekening kunnen houden met {hunZijnHaar} wensen. Het KIES Kindplan van {kinderenNamen} is opgenomen als bijlage van dit ouderschapsplan.",
                "kies_professional" => $"Bij het maken van de afspraken in dit ouderschapsplan {isZijn} {kinderenNamen} ondersteund door een KIES professional met een KIES kindgesprek om {hunZijnHaar} vragen te kunnen stellen en behoeftes en wensen aan te geven, zodat wij hiermee rekening kunnen houden. {kindplanTekst} daarbij gemaakt en bijlage van dit ouderschapsplan.",
                "kindbehartiger" => $"Bij het maken van de afspraken in dit ouderschapsplan {heeftHebben} {kinderenNamen} hulp gekregen van een Kindbehartiger om {hunZijnHaar} wensen in kaart te brengen zodat wij hiermee rekening kunnen houden.",
                _ => ""
            };
        }

        private string GetWoonplaatsRegeling(
            int? woonplaatsOptie,
            string? woonplaatsPartij1,
            string? woonplaatsPartij2,
            PersonData? partij1,
            PersonData? partij2,
            string? soortRelatie = null)
        {
            var partij1Naam = GetPartijBenaming(partij1, false);
            var partij2Naam = GetPartijBenaming(partij2, false);
            var huidigeWoonplaatsPartij1 = partij1?.Plaats ?? "onbekend";
            var huidigeWoonplaatsPartij2 = partij2?.Plaats ?? "onbekend";

            var relatieVerbreking = GetRelatieVerbreking(soortRelatie);

            if (!woonplaatsOptie.HasValue)
            {
                return $"Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij {relatieVerbreking}.";
            }

            return woonplaatsOptie.Value switch
            {
                1 => $"De woonplaatsen van partijen blijven hetzelfde. {partij1Naam} blijft wonen in {huidigeWoonplaatsPartij1} en {partij2Naam} blijft wonen in {huidigeWoonplaatsPartij2}.",
                2 => $"{partij1Naam} gaat verhuizen naar {woonplaatsPartij1 ?? "een nieuwe woonplaats"}. {partij2Naam} blijft wonen in {huidigeWoonplaatsPartij2}.",
                3 => $"{partij1Naam} blijft wonen in {huidigeWoonplaatsPartij1}. {partij2Naam} gaat verhuizen naar {woonplaatsPartij2 ?? "een nieuwe woonplaats"}.",
                4 => $"{partij1Naam} gaat verhuizen naar {woonplaatsPartij1 ?? "een nieuwe woonplaats"} en {partij2Naam} gaat verhuizen naar {woonplaatsPartij2 ?? "een nieuwe woonplaats"}.",
                5 => $"Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij {relatieVerbreking}.",
                _ => $"Het is nog onduidelijk waar de ouders zullen gaan wonen nadat zij {relatieVerbreking}."
            };
        }

        private string GetKinderbijslagOntvanger(int? partijNummer, PersonData? partij1, PersonData? partij2)
        {
            return partijNummer switch
            {
                1 => GetPartijBenaming(partij1, false),
                2 => GetPartijBenaming(partij2, false),
                3 => "Kinderrekening",
                _ => ""
            };
        }

        private string GetHoofdverblijfText(string? hoofdverblijf, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen, bool? isAnoniem)
        {
            if (string.IsNullOrEmpty(hoofdverblijf))
                return "";

            // Try to parse the hoofdverblijf as a person ID
            if (int.TryParse(hoofdverblijf, out int personId))
            {
                // Check if it matches partij1
                if (partij1 != null && partij1.Id == personId)
                {
                    var partij1Benaming = GetPartijBenaming(partij1, false);
                    var kinderenTekst = GetKinderenTekst(kinderen);
                    return $"{kinderenTekst} {(kinderen.Count == 1 ? "heeft" : "hebben")} {(kinderen.Count == 1 ? "het" : "hun")} hoofdverblijf bij {partij1Benaming}.";
                }
                // Check if it matches partij2
                else if (partij2 != null && partij2.Id == personId)
                {
                    var partij2Benaming = GetPartijBenaming(partij2, false);
                    var kinderenTekst = GetKinderenTekst(kinderen);
                    return $"{kinderenTekst} {(kinderen.Count == 1 ? "heeft" : "hebben")} {(kinderen.Count == 1 ? "het" : "hun")} hoofdverblijf bij {partij2Benaming}.";
                }
            }

            // If not a valid person ID or doesn't match, return the raw value
            return hoofdverblijf;
        }

        private static string Capitalize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            return char.ToUpper(text[0]) + text[1..];
        }

        #endregion
    }
}

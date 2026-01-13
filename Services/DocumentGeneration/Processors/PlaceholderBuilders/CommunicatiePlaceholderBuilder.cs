using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Builder voor communicatie afspraken placeholders.
    /// Verantwoordelijk voor Villa Pinedo, social media, devices, verzekeringen, etc.
    /// </summary>
    public class CommunicatiePlaceholderBuilder : BasePlaceholderBuilder
    {
        public override int Order => 60;

        public CommunicatiePlaceholderBuilder(ILogger<CommunicatiePlaceholderBuilder> logger)
            : base(logger)
        {
        }

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogDebug("Building communicatie placeholders for dossier {DossierId}", data.Id);

            var kinderen = data.Kinderen ?? new List<ChildData>();

            // Initialize all placeholders with empty values first
            InitializeEmptyPlaceholders(replacements);

            if (data.CommunicatieAfspraken == null)
            {
                _logger.LogDebug("No communicatie afspraken data available, placeholders set to empty strings");
                return;
            }

            AddCommunicatieAfsprakenReplacements(
                replacements,
                data.CommunicatieAfspraken,
                data.Partij1,
                data.Partij2,
                kinderen);
        }

        private void InitializeEmptyPlaceholders(Dictionary<string, string> replacements)
        {
            var placeholders = new[]
            {
                "VillaPinedoKinderen", "VillaPinedoZin", "KinderenBetrokkenheid", "BetrokkenheidKindZin",
                "KiesMethode", "OmgangTekstOfSchema", "OmgangsregelingBeschrijving",
                "Opvang", "OpvangBeschrijving", "InformatieUitwisseling", "InformatieUitwisselingBeschrijving",
                "BijlageBeslissingen", "SocialMedia", "SocialMediaKeuze", "SocialMediaLeeftijd", "SocialMediaBeschrijving",
                "MobielTablet", "DeviceSmartphone", "DeviceTablet", "DeviceSmartwatch", "DeviceLaptop", "DevicesBeschrijving",
                "ToezichtApps", "ToezichtAppsBeschrijving", "LocatieDelen", "LocatieDelenBeschrijving",
                "IdBewijzen", "IdBewijzenBeschrijving", "Aansprakelijkheidsverzekering", "AansprakelijkheidsverzekeringBeschrijving",
                "Ziektekostenverzekering", "ZiektekostenverzekeringBeschrijving",
                "ToestemmingReizen", "ToestemmingReizenBeschrijving",
                "Jongmeerderjarige", "JongmeerderjarigeBeschrijving", "Studiekosten", "StudiekostenBeschrijving",
                "BankrekeningKinderen", "Evaluatie", "ParentingCoordinator", "MediationClausule"
            };

            foreach (var placeholder in placeholders)
            {
                AddPlaceholder(replacements, placeholder, "");
            }

            AddPlaceholder(replacements, "BankrekeningenCount", "0");
        }

        private void AddCommunicatieAfsprakenReplacements(
            Dictionary<string, string> replacements,
            CommunicatieAfsprakenData comm,
            PersonData? partij1,
            PersonData? partij2,
            List<ChildData> kinderen)
        {
            // Villa Pinedo
            AddPlaceholder(replacements, "VillaPinedoKinderen", comm.VillaPinedoKinderen);
            AddPlaceholder(replacements, "VillaPinedoZin", GetVillaPinedoZin(comm.VillaPinedoKinderen, kinderen));

            // Betrokkenheid
            AddPlaceholder(replacements, "KinderenBetrokkenheid", comm.KinderenBetrokkenheid);
            AddPlaceholder(replacements, "BetrokkenheidKindZin", GetBetrokkenheidKindZin(comm.KinderenBetrokkenheid, kinderen));
            AddPlaceholder(replacements, "KiesMethode", comm.KiesMethode);

            // Omgangsregeling
            AddPlaceholder(replacements, "OmgangTekstOfSchema", comm.OmgangTekstOfSchema);
            AddPlaceholder(replacements, "OmgangsregelingBeschrijving",
                GetOmgangsregelingBeschrijving(comm.OmgangTekstOfSchema, comm.OmgangBeschrijving, kinderen.Count));

            // Opvang
            AddPlaceholder(replacements, "Opvang", comm.Opvang);
            AddPlaceholder(replacements, "OpvangBeschrijving", GetOpvangBeschrijving(comm.Opvang));

            // Informatie uitwisseling
            AddPlaceholder(replacements, "InformatieUitwisseling", comm.InformatieUitwisseling);
            AddPlaceholder(replacements, "InformatieUitwisselingBeschrijving",
                GetInformatieUitwisselingBeschrijving(comm.InformatieUitwisseling, kinderen));
            AddPlaceholder(replacements, "BijlageBeslissingen", comm.BijlageBeslissingen);

            // Social media
            if (!string.IsNullOrEmpty(comm.SocialMedia))
            {
                var (keuze, leeftijd) = ParseSocialMediaValue(comm.SocialMedia);
                AddPlaceholder(replacements, "SocialMedia", comm.SocialMedia);
                AddPlaceholder(replacements, "SocialMediaKeuze", keuze);
                AddPlaceholder(replacements, "SocialMediaLeeftijd", leeftijd);
            }
            AddPlaceholder(replacements, "SocialMediaBeschrijving", GetSocialMediaBeschrijving(comm.SocialMedia, kinderen));

            // Devices
            if (!string.IsNullOrEmpty(comm.MobielTablet))
            {
                var deviceAfspraken = ParseDeviceAfspraken(comm.MobielTablet);
                AddPlaceholder(replacements, "MobielTablet", FormatDeviceAfspraken(deviceAfspraken));
                AddPlaceholder(replacements, "DeviceSmartphone", deviceAfspraken.Smartphone?.ToString());
                AddPlaceholder(replacements, "DeviceTablet", deviceAfspraken.Tablet?.ToString());
                AddPlaceholder(replacements, "DeviceSmartwatch", deviceAfspraken.Smartwatch?.ToString());
                AddPlaceholder(replacements, "DeviceLaptop", deviceAfspraken.Laptop?.ToString());
                AddPlaceholder(replacements, "DevicesBeschrijving", GetDevicesBeschrijving(deviceAfspraken, kinderen));
            }

            // Toezicht apps
            AddPlaceholder(replacements, "ToezichtApps", comm.ToezichtApps);
            AddPlaceholder(replacements, "ToezichtAppsBeschrijving", GetToezichtAppsBeschrijving(comm.ToezichtApps));

            // Locatie delen
            AddPlaceholder(replacements, "LocatieDelen", comm.LocatieDelen);
            AddPlaceholder(replacements, "LocatieDelenBeschrijving", GetLocatieDelenBeschrijving(comm.LocatieDelen));

            // Documenten
            AddPlaceholder(replacements, "IdBewijzen", comm.IdBewijzen);
            AddPlaceholder(replacements, "IdBewijzenBeschrijving", GetIdBewijzenBeschrijving(comm.IdBewijzen, partij1, partij2, kinderen));

            // Verzekeringen
            AddPlaceholder(replacements, "Aansprakelijkheidsverzekering", comm.Aansprakelijkheidsverzekering);
            AddPlaceholder(replacements, "AansprakelijkheidsverzekeringBeschrijving",
                GetAansprakelijkheidsverzekeringBeschrijving(comm.Aansprakelijkheidsverzekering, partij1, partij2, kinderen));
            AddPlaceholder(replacements, "Ziektekostenverzekering", comm.Ziektekostenverzekering);
            AddPlaceholder(replacements, "ZiektekostenverzekeringBeschrijving",
                GetZiektekostenverzekeringBeschrijving(comm.Ziektekostenverzekering, partij1, partij2, kinderen));

            // Reizen
            AddPlaceholder(replacements, "ToestemmingReizen", comm.ToestemmingReizen);
            AddPlaceholder(replacements, "ToestemmingReizenBeschrijving", GetToestemmingReizenBeschrijving(comm.ToestemmingReizen, kinderen));

            // Toekomst
            AddPlaceholder(replacements, "Jongmeerderjarige", comm.Jongmeerderjarige);
            AddPlaceholder(replacements, "JongmeerderjarigeBeschrijving", GetJongmeerderjarigeBeschrijving(comm.Jongmeerderjarige, partij1, partij2));
            AddPlaceholder(replacements, "Studiekosten", comm.Studiekosten);
            AddPlaceholder(replacements, "StudiekostenBeschrijving", GetStudiekostenBeschrijving(comm.Studiekosten, partij1, partij2));

            // Bankrekeningen
            if (!string.IsNullOrEmpty(comm.BankrekeningKinderen))
            {
                var bankrekeningen = ParseBankrekeningen(comm.BankrekeningKinderen);
                AddPlaceholder(replacements, "BankrekeningKinderen", FormatBankrekeningen(bankrekeningen, partij1, partij2, kinderen));
                AddPlaceholder(replacements, "BankrekeningenCount", bankrekeningen.Count.ToString());

                for (int i = 0; i < bankrekeningen.Count; i++)
                {
                    var rek = bankrekeningen[i];
                    AddPlaceholder(replacements, $"Bankrekening{i + 1}IBAN", FormatIBAN(rek.Iban));
                    AddPlaceholder(replacements, $"Bankrekening{i + 1}Tenaamstelling", TranslateTenaamstelling(rek.Tenaamstelling, partij1, partij2, kinderen));
                    AddPlaceholder(replacements, $"Bankrekening{i + 1}BankNaam", rek.BankNaam);
                }
            }

            // Evaluatie
            AddPlaceholder(replacements, "Evaluatie", comm.Evaluatie);
            AddPlaceholder(replacements, "ParentingCoordinator", comm.ParentingCoordinator);
            AddPlaceholder(replacements, "MediationClausule", comm.MediationClausule);

            _logger.LogDebug("Added communicatie afspraken data: VillaPinedo={VillaPinedo}, BankrekeningenCount={BankCount}",
                replacements["VillaPinedoKinderen"],
                replacements["BankrekeningenCount"]);
        }

        #region Helper Methods

        private string GetVillaPinedoZin(string? villaPinedo, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(villaPinedo) || kinderen.Count == 0)
                return "";

            var kinderenNamen = DutchLanguageHelper.FormatList(
                kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var isEnkelvoud = kinderen.Count == 1;
            var hunZijnHaar = isEnkelvoud ? GetBezittelijkVoornaamwoord(kinderen[0].Geslacht) : "hun";

            var zijHijZij = isEnkelvoud
                ? (kinderen[0].Geslacht?.ToLowerInvariant() switch
                {
                    "man" or "m" or "jongen" => "hij",
                    "vrouw" or "v" or "meisje" => "zij",
                    _ => "hij/zij"
                })
                : "zij";

            var henHemHaar = isEnkelvoud
                ? (kinderen[0].Geslacht?.ToLowerInvariant() switch
                {
                    "man" or "m" or "jongen" => "hem",
                    "vrouw" or "v" or "meisje" => "haar",
                    _ => "hem/haar"
                })
                : "hen";

            return villaPinedo.ToLowerInvariant() switch
            {
                "ja" => $"Wij hebben {kinderenNamen} op de hoogte gebracht van Villa Pinedo, waar {zijHijZij} terecht kan met {hunZijnHaar} vragen, voor het delen van ervaringen, het krijgen van tips en steun om met de scheiding om te gaan.",
                "nee" => $"Wij hebben {kinderenNamen} nog niet op de hoogte gebracht van Villa Pinedo, waar {zijHijZij} terecht kan met {hunZijnHaar} vragen, voor het delen van ervaringen, het krijgen van tips en steun om met de scheiding om te gaan. Als daar aanleiding toe is zullen wij {henHemHaar} daar zeker op attenderen.",
                _ => ""
            };
        }

        private string GetBetrokkenheidKindZin(string? betrokkenheid, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(betrokkenheid) || kinderen.Count == 0)
                return "";

            var kinderenNamen = DutchLanguageHelper.FormatList(
                kinderen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList());

            var isEnkelvoud = kinderen.Count == 1;
            var isZijn = isEnkelvoud ? "is" : "zijn";
            var hunZijnHaar = isEnkelvoud ? GetBezittelijkVoornaamwoord(kinderen[0].Geslacht) : "hun";

            return betrokkenheid.ToLowerInvariant() switch
            {
                "samen" => $"Wij hebben samen met {kinderenNamen} gesproken zodat wij rekening kunnen houden met {hunZijnHaar} wensen.",
                "los_van_elkaar" => $"Wij hebben los van elkaar met {kinderenNamen} gesproken zodat wij rekening kunnen houden met {hunZijnHaar} wensen.",
                "jonge_leeftijd" => $"{kinderenNamen} {isZijn} gezien de jonge leeftijd niet betrokken bij het opstellen van het ouderschapsplan.",
                "niet_betrokken" => $"{kinderenNamen} {isZijn} niet betrokken bij het opstellen van het ouderschapsplan.",
                _ => ""
            };
        }

        private string GetOmgangsregelingBeschrijving(string? omgangTekstOfSchema, string? omgangBeschrijving, int aantalKinderen)
        {
            var kinderenTekst = aantalKinderen == 1 ? "ons kind" : "onze kinderen";
            var keuze = omgangTekstOfSchema?.Trim().ToLowerInvariant() ?? "";

            return keuze switch
            {
                "tekst" => $"Wij verdelen de zorg en opvoeding van {kinderenTekst} op de volgende manier: {omgangBeschrijving}",
                "beiden" => $"Wij verdelen de zorg en opvoeding van {kinderenTekst} op de volgende manier: {omgangBeschrijving} Daarnaast is er ook een vast schema toegevoegd in de bijlage van het ouderschapsplan.",
                _ => $"Wij verdelen de zorg en opvoeding van {kinderenTekst} volgens het vaste schema van bijlage 1."
            };
        }

        private string GetOpvangBeschrijving(string? opvang)
        {
            if (string.IsNullOrEmpty(opvang))
                return "";

            return opvang.Trim() switch
            {
                "1" => "Wij blijven ieder zelf verantwoordelijk voor de opvang van onze kinderen op de dagen dat ze volgens het schema bij ieder van ons verblijven.",
                "2" => "Als opvang of een afwijking van het schema nodig is, vragen wij altijd eerst aan de andere ouder of die beschikbaar is, voordat wij anderen vragen voor de opvang van onze kinderen.",
                _ => ""
            };
        }

        private string GetInformatieUitwisselingBeschrijving(string? informatieUitwisseling, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(informatieUitwisseling))
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = informatieUitwisseling.Trim().ToLowerInvariant();

            return keuze switch
            {
                "email" => $"Wij delen de informatie over {kinderenTekst} met elkaar via de e-mail.",
                "telefoon" => $"Wij delen de informatie over {kinderenTekst} met elkaar telefonisch.",
                "app" => $"Wij delen de informatie over {kinderenTekst} met elkaar via een app (zoals WhatsApp).",
                "oudersapp" => $"Wij delen de informatie over {kinderenTekst} met elkaar via een speciale ouders-app.",
                "persoonlijk" => $"Wij delen de informatie over {kinderenTekst} met elkaar in een persoonlijk gesprek.",
                "combinatie" => $"Wij delen de informatie over {kinderenTekst} met elkaar via een combinatie van methoden (e-mail, telefonisch, app en mondeling).",
                _ => ""
            };
        }

        private (string keuze, string leeftijd) ParseSocialMediaValue(string socialMedia)
        {
            if (string.IsNullOrEmpty(socialMedia))
                return ("", "");

            if (socialMedia.StartsWith("wel_"))
            {
                var parts = socialMedia.Split('_');
                if (parts.Length == 2)
                    return ("wel", parts[1]);
            }

            return (socialMedia, "");
        }

        private string GetSocialMediaBeschrijving(string? socialMedia, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(socialMedia) || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var zijnHun = kinderen.Count == 1
                ? (kinderen[0].Geslacht?.ToLowerInvariant() == "m" ? "zijn" : "haar")
                : "hun";

            var waarde = socialMedia.Trim().ToLowerInvariant();

            if (waarde.StartsWith("wel_"))
            {
                var leeftijd = waarde.Substring(4);
                return $"Wij spreken als ouders af dat {kinderenTekst} social media mogen gebruiken vanaf {zijnHun} {leeftijd}e jaar, op voorwaarde dat het op een veilige manier gebeurt.";
            }

            return waarde switch
            {
                "geen" => $"Wij spreken als ouders af dat {kinderenTekst} geen social media mogen gebruiken.",
                "wel" => $"Wij spreken als ouders af dat {kinderenTekst} social media mogen gebruiken, op voorwaarde dat het op een veilige manier gebeurt.",
                "later" => $"Wij maken als ouders later afspraken over het gebruik van social media door {kinderenTekst}.",
                _ => ""
            };
        }

        private DeviceAfspraken ParseDeviceAfspraken(string jsonString)
        {
            try
            {
                return JsonSerializer.Deserialize<DeviceAfspraken>(jsonString) ?? new DeviceAfspraken();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse device afspraken JSON: {Json}", jsonString);
                return new DeviceAfspraken();
            }
        }

        private string FormatDeviceAfspraken(DeviceAfspraken afspraken)
        {
            var lines = new List<string>();

            if (afspraken.Smartphone.HasValue)
                lines.Add($"- Smartphone: {afspraken.Smartphone} jaar");
            if (afspraken.Tablet.HasValue)
                lines.Add($"- Tablet: {afspraken.Tablet} jaar");
            if (afspraken.Smartwatch.HasValue)
                lines.Add($"- Smartwatch: {afspraken.Smartwatch} jaar");
            if (afspraken.Laptop.HasValue)
                lines.Add($"- Laptop: {afspraken.Laptop} jaar");

            return string.Join("\n", lines);
        }

        private string GetDevicesBeschrijving(DeviceAfspraken afspraken, List<ChildData> kinderen)
        {
            if (kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var krijgtKrijgen = kinderen.Count == 1 ? "krijgt" : "krijgen";
            var zijnHun = kinderen.Count == 1
                ? (kinderen[0].Geslacht?.ToLowerInvariant() == "m" ? "zijn" : "haar")
                : "hun";

            var zinnen = new List<string>();

            if (afspraken.Smartphone.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een smartphone vanaf {zijnHun} {afspraken.Smartphone}e jaar.");
            if (afspraken.Tablet.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een tablet vanaf {zijnHun} {afspraken.Tablet}e jaar.");
            if (afspraken.Smartwatch.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een smartwatch vanaf {zijnHun} {afspraken.Smartwatch}e jaar.");
            if (afspraken.Laptop.HasValue)
                zinnen.Add($"{kinderenTekst} {krijgtKrijgen} een laptop vanaf {zijnHun} {afspraken.Laptop}e jaar.");

            return string.Join("\n", zinnen);
        }

        private string GetToezichtAppsBeschrijving(string? toezichtApps)
        {
            if (string.IsNullOrEmpty(toezichtApps))
                return "";

            return toezichtApps.Trim().ToLowerInvariant() switch
            {
                "wel" => "Wij spreken als ouders af wel ouderlijk toezichtapps te gebruiken.",
                "geen" => "Wij spreken als ouders af geen ouderlijk toezichtapps te gebruiken.",
                _ => ""
            };
        }

        private string GetLocatieDelenBeschrijving(string? locatieDelen)
        {
            if (string.IsNullOrEmpty(locatieDelen))
                return "";

            return locatieDelen.Trim().ToLowerInvariant() switch
            {
                "wel" => "Wij spreken als ouders af om de locatie van onze kinderen wel te delen via digitale apparaten.",
                "geen" => "Wij spreken als ouders af om de locatie van onze kinderen niet te delen via digitale apparaten.",
                _ => ""
            };
        }

        private string GetIdBewijzenBeschrijving(string? idBewijzen, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(idBewijzen) || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = idBewijzen.Trim().ToLowerInvariant();
            var partij1Naam = GetPartijBenaming(partij1, false);
            var partij2Naam = GetPartijBenaming(partij2, false);

            return keuze switch
            {
                "ouder_1" or "partij1" => $"De identiteitsbewijzen van {kinderenTekst} worden bewaard door {partij1Naam}.",
                "ouder_2" or "partij2" => $"De identiteitsbewijzen van {kinderenTekst} worden bewaard door {partij2Naam}.",
                "beide_ouders" or "beiden" => $"De identiteitsbewijzen van {kinderenTekst} worden bewaard door beide ouders.",
                "kinderen_zelf" or "kinderen" => $"{kinderenTekst} {(kinderen.Count == 1 ? "bewaart" : "bewaren")} {(kinderen.Count == 1 ? "zijn/haar" : "hun")} eigen identiteitsbewijs.",
                "nvt" or "niet_van_toepassing" => "Niet van toepassing.",
                _ => ""
            };
        }

        private string GetAansprakelijkheidsverzekeringBeschrijving(string? aansprakelijkheidsverzekering, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(aansprakelijkheidsverzekering) || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = aansprakelijkheidsverzekering.Trim().ToLowerInvariant();
            var partij1Naam = GetPartijBenaming(partij1, false);
            var partij2Naam = GetPartijBenaming(partij2, false);

            return keuze switch
            {
                "beiden" or "beide_ouders" => $"Wij zorgen ervoor dat {kinderenTekst} bij ons beiden tegen wettelijke aansprakelijkheid {(kinderen.Count == 1 ? "is" : "zijn")} verzekerd.",
                "ouder_1" or "partij1" => $"{Capitalize(partij1Naam)} zorgt ervoor dat {kinderenTekst} tegen wettelijke aansprakelijkheid {(kinderen.Count == 1 ? "is" : "zijn")} verzekerd.",
                "ouder_2" or "partij2" => $"{Capitalize(partij2Naam)} zorgt ervoor dat {kinderenTekst} tegen wettelijke aansprakelijkheid {(kinderen.Count == 1 ? "is" : "zijn")} verzekerd.",
                "nvt" or "niet_van_toepassing" => "Niet van toepassing.",
                _ => ""
            };
        }

        private string GetZiektekostenverzekeringBeschrijving(string? ziektekostenverzekering, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(ziektekostenverzekering) || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = ziektekostenverzekering.Trim().ToLowerInvariant();
            var partij1Naam = GetPartijBenaming(partij1, false);
            var partij2Naam = GetPartijBenaming(partij2, false);
            var isZijn = kinderen.Count == 1 ? "is" : "zijn";
            var zijHun = kinderen.Count == 1
                ? (kinderen[0].Geslacht?.ToLowerInvariant() == "m" ? "hij zijn" : "zij haar")
                : "zij hun";

            return keuze switch
            {
                "ouder_1" or "partij1" => $"{kinderenTekst} {isZijn} verzekerd op de ziektekostenverzekering van {partij1Naam}.",
                "ouder_2" or "partij2" => $"{kinderenTekst} {isZijn} verzekerd op de ziektekostenverzekering van {partij2Naam}.",
                "hoofdverblijf" => $"{kinderenTekst} {isZijn} verzekerd op de ziektekostenverzekering van de ouder waar {zijHun} hoofdverblijf {(kinderen.Count == 1 ? "heeft" : "hebben")}.",
                "nvt" or "niet_van_toepassing" => "Niet van toepassing.",
                _ => ""
            };
        }

        private string GetToestemmingReizenBeschrijving(string? toestemmingReizen, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(toestemmingReizen) || kinderen.Count == 0)
                return "";

            var kinderenTekst = GetKinderenTekst(kinderen);
            var keuze = toestemmingReizen.Trim().ToLowerInvariant();

            return keuze switch
            {
                "altijd_overleggen" or "altijd" => $"Voor reizen met {kinderenTekst} is altijd vooraf overleg tussen de ouders vereist.",
                "eu_vrij" => $"Met {kinderenTekst} mag binnen de EU vrij worden gereisd. Voor reizen buiten de EU is vooraf overleg tussen de ouders vereist.",
                "vrij" => $"Met {kinderenTekst} mag vrij worden gereisd zonder vooraf overleg.",
                "schriftelijk" => $"Voor reizen met {kinderenTekst} is schriftelijke toestemming van de andere ouder vereist.",
                _ => ""
            };
        }

        private string GetJongmeerderjarigeBeschrijving(string? jongmeerderjarige, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(jongmeerderjarige))
                return "";

            var ouder1Naam = GetPartijBenaming(partij1, false);
            var ouder2Naam = GetPartijBenaming(partij2, false);
            var keuze = jongmeerderjarige.Trim().ToLowerInvariant();

            return keuze switch
            {
                "bijdrage_rechtstreeks_kind" => "De ouders betalen een bijdrage rechtstreeks aan het kind.",
                "bijdrage_rechtstreeks_uitwonend" => "De ouders betalen een bijdrage rechtstreeks aan het kind als het kind niet meer thuiswoont.",
                "bijdrage_beiden" => "Beide ouders blijven bijdragen aan de kosten voor het jongmeerderjarige kind.",
                "ouder1" => $"{Capitalize(ouder1Naam)} blijft bijdragen aan de kosten voor het jongmeerderjarige kind.",
                "ouder2" => $"{Capitalize(ouder2Naam)} blijft bijdragen aan de kosten voor het jongmeerderjarige kind.",
                "geen_bijdrage" => "Er is geen bijdrage meer verschuldigd als het kind voldoende eigen inkomen heeft.",
                "nvt" => "",
                _ => ""
            };
        }

        private string GetStudiekostenBeschrijving(string? studiekosten, PersonData? partij1, PersonData? partij2)
        {
            if (string.IsNullOrEmpty(studiekosten))
                return "";

            var ouder1Naam = GetPartijBenaming(partij1, false);
            var ouder2Naam = GetPartijBenaming(partij2, false);
            var keuze = studiekosten.Trim().ToLowerInvariant();

            return keuze switch
            {
                "draagkracht_rato" => "Beide ouders dragen naar rato van hun draagkracht bij aan de studiekosten.",
                "netto_inkomen_rato" => "De ouders dragen naar rato van hun netto inkomen bij aan de studiekosten.",
                "beide_helft" => "Beide ouders dragen voor de helft bij aan de studiekosten.",
                "evenredig" => "De ouders dragen evenredig naar inkomen bij aan de studiekosten.",
                "ouder1" => $"{Capitalize(ouder1Naam)} betaalt de studiekosten.",
                "ouder2" => $"{Capitalize(ouder2Naam)} betaalt de studiekosten.",
                "kind_zelf" => "Het kind betaalt de studiekosten zelf (via lening en/of werk).",
                "nvt" => "",
                _ => ""
            };
        }

        private List<Kinderrekening> ParseBankrekeningen(string jsonString)
        {
            try
            {
                return JsonSerializer.Deserialize<List<Kinderrekening>>(jsonString) ?? new List<Kinderrekening>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse bankrekeningen JSON: {Json}", jsonString);
                return new List<Kinderrekening>();
            }
        }

        private string FormatBankrekeningen(List<Kinderrekening> rekeningen, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen)
        {
            if (!rekeningen.Any())
                return "";

            var lines = new List<string>();

            for (int i = 0; i < rekeningen.Count; i++)
            {
                var rek = rekeningen[i];
                lines.Add($"Rekening {i + 1}:");
                lines.Add($"  IBAN: {FormatIBAN(rek.Iban)}");
                lines.Add($"  Bank: {rek.BankNaam}");
                lines.Add($"  Ten name van: {TranslateTenaamstelling(rek.Tenaamstelling, partij1, partij2, kinderen)}");
                if (i < rekeningen.Count - 1)
                    lines.Add("");
            }

            return string.Join("\n", lines);
        }

        private string FormatIBAN(string iban)
        {
            if (string.IsNullOrEmpty(iban))
                return "";

            iban = iban.Replace(" ", "");

            var formatted = "";
            for (int i = 0; i < iban.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    formatted += " ";
                formatted += iban[i];
            }

            return formatted;
        }

        private string TranslateTenaamstelling(string code, PersonData? partij1, PersonData? partij2, List<ChildData> kinderen)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            if (code == "ouder_1" && partij1 != null)
                return $"Op naam van {GetPartijBenaming(partij1, false)}";

            if (code == "ouder_2" && partij2 != null)
                return $"Op naam van {GetPartijBenaming(partij2, false)}";

            if (code == "ouders_gezamenlijk" && partij1 != null && partij2 != null)
            {
                var naam1 = GetPartijBenaming(partij1, false);
                var naam2 = GetPartijBenaming(partij2, false);
                return $"Op gezamenlijke naam van {naam1} en {naam2}";
            }

            if (code == "kinderen_alle")
            {
                var minderjarigen = kinderen.Where(k => CalculateAge(k.GeboorteDatum) < 18).ToList();
                if (minderjarigen.Any())
                {
                    var namen = minderjarigen.Select(k => k.Roepnaam ?? k.Voornamen ?? k.Achternaam).ToList();
                    return $"Op naam van {DutchLanguageHelper.FormatList(namen)}";
                }
                return "Op naam van alle minderjarige kinderen";
            }

            if (code.StartsWith("kind_"))
            {
                var kindIdStr = code.Substring(5);
                if (int.TryParse(kindIdStr, out int kindId))
                {
                    var kind = kinderen.FirstOrDefault(k => k.Id == kindId);
                    if (kind != null)
                    {
                        var kindNaam = kind.Roepnaam ?? kind.Voornamen ?? kind.Achternaam;
                        return $"Op naam van {kindNaam}";
                    }
                }
            }

            return code;
        }

        private int CalculateAge(DateTime? geboorteDatum)
        {
            if (!geboorteDatum.HasValue)
                return 0;

            var today = DateTime.Today;
            var age = today.Year - geboorteDatum.Value.Year;

            if (geboorteDatum.Value.Date > today.AddYears(-age))
                age--;

            return age;
        }

        private static string Capitalize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            return char.ToUpper(text[0]) + text[1..];
        }

        #endregion
    }
}

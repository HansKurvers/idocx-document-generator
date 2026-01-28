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
    /// Placeholder builder for Convenant-specific placeholders.
    /// Handles partneralimentatie, woning, vermogensverdeling, and other convenant fields.
    /// Executed after FiscaalPlaceholderBuilder (Order: 80).
    /// </summary>
    public class ConvenantPlaceholderBuilder : BasePlaceholderBuilder
    {
        public ConvenantPlaceholderBuilder(ILogger<ConvenantPlaceholderBuilder> logger) : base(logger)
        {
        }

        public override int Order => 80;

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogInformation("Building convenant placeholders");

            var convenantInfo = data.ConvenantInfo;
            if (convenantInfo == null)
            {
                _logger.LogInformation("No convenant info found for dossier, skipping convenant placeholders");
                return;
            }

            var partij1 = data.Partij1;
            var partij2 = data.Partij2;

            // Build placeholders for each section
            BuildPartijAanduidingPlaceholders(replacements, data);
            BuildPartneralimentatiePlaceholders(replacements, convenantInfo, partij1, partij2);
            BuildWoningPlaceholders(replacements, convenantInfo);
            BuildKadastraalPlaceholders(replacements, convenantInfo);
            BuildHypotheekPlaceholders(replacements, convenantInfo);
            BuildVermogensverdelingPlaceholders(replacements, convenantInfo);
            BuildPensioenPlaceholders(replacements, convenantInfo);
            BuildKwijtingPlaceholders(replacements, convenantInfo);
            BuildOndertekeningPlaceholders(replacements, convenantInfo);
            BuildConsideransPlaceholders(replacements, data, convenantInfo);

            _logger.LogInformation("Completed building convenant placeholders");
        }

        /// <summary>
        /// Builds partij aanduiding placeholders based on IsAnoniem setting.
        /// For convenant: de man/de vrouw when anonymous, roepnaam + achternaam when named.
        /// Also creates capitalized variants for use at the start of sentences.
        /// </summary>
        private void BuildPartijAanduidingPlaceholders(Dictionary<string, string> replacements, DossierData data)
        {
            var partij1 = data.Partij1;
            var partij2 = data.Partij2;
            var isAnoniem = data.IsAnoniem == true;

            // Determine aanduidingen based on anonymity
            string partij1Aanduiding, partij2Aanduiding;

            if (isAnoniem)
            {
                // Anonymous: use de man / de vrouw
                partij1Aanduiding = GetGeslachtAanduiding(partij1?.Geslacht);
                partij2Aanduiding = GetGeslachtAanduiding(partij2?.Geslacht);
            }
            else
            {
                // Named: use roepnaam + achternaam
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

        private string Capitalize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return char.ToUpper(text[0]) + text.Substring(1);
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

        private void BuildPartneralimentatiePlaceholders(
            Dictionary<string, string> replacements,
            ConvenantInfoData info,
            PersonData? partij1,
            PersonData? partij2)
        {
            // Determine alimentatieplichtige en alimentatiegerechtigde
            var alimentatieplichtige = info.PartneralimentatieBetaler?.ToLower() == "partij1" ? partij1 : partij2;
            var alimentatiegerechtigde = info.PartneralimentatieBetaler?.ToLower() == "partij1" ? partij2 : partij1;

            AddPlaceholder(replacements, "ALIMENTATIEPLICHTIGE", GetPartijBenaming(alimentatieplichtige, false));
            AddPlaceholder(replacements, "ALIMENTATIEGERECHTIGDE", GetPartijBenaming(alimentatiegerechtigde, false));

            // Duurzaam gescheiden
            AddPlaceholder(replacements, "DUURZAAM_GESCHEIDEN_DATUM", FormatDate(info.DuurzaamGescheidenDatum));

            // Bedragen en behoeften
            AddPlaceholder(replacements, "NETTO_GEZINSINKOMEN", FormatCurrency(info.NettoGezinsinkomen));
            AddPlaceholder(replacements, "KOSTEN_KINDEREN_PARTNERALIMENTATIE", FormatCurrency(info.KostenKinderenPartneralimentatie));
            AddPlaceholder(replacements, "NETTO_BEHOEFTE", FormatCurrency(info.NettoBehoefte));
            AddPlaceholder(replacements, "BRUTO_AANVULLENDE_BEHOEFTE", FormatCurrency(info.BrutoAanvullendeBehoefte));

            // Draagkracht partij 1
            AddPlaceholder(replacements, "BRUTO_JAARINKOMEN_PARTIJ1", FormatCurrency(info.BrutoJaarinkomenPartij1));
            AddPlaceholder(replacements, "DRAAGKRACHTLOOS_INKOMEN_PARTIJ1", FormatCurrency(info.DraagkrachtloosInkomenPartij1));
            AddPlaceholder(replacements, "DRAAGKRACHT_PARTIJ1", FormatCurrency(info.DraagkrachtPartij1));

            // Draagkracht partij 2
            AddPlaceholder(replacements, "BRUTO_JAARINKOMEN_PARTIJ2", FormatCurrency(info.BrutoJaarinkomenPartij2));
            AddPlaceholder(replacements, "DRAAGKRACHTLOOS_INKOMEN_PARTIJ2", FormatCurrency(info.DraagkrachtloosInkomenPartij2));
            AddPlaceholder(replacements, "DRAAGKRACHT_PARTIJ2", FormatCurrency(info.DraagkrachtPartij2));

            // Eigen inkomsten
            AddPlaceholder(replacements, "EIGEN_INKOMSTEN_BEDRAG", FormatCurrency(info.EigenInkomstenBedrag));
            AddPlaceholder(replacements, "VERDIENCAPACITEIT_BEDRAG", FormatCurrency(info.VerdiencapaciteitBedrag));

            // Alimentatie bedrag
            AddPlaceholder(replacements, "HOOGTE_PARTNERALIMENTATIE", FormatCurrency(info.HoogtePartneralimentatie));
            AddPlaceholder(replacements, "PARTNERALIMENTATIE_INGANGSDATUM", FormatDate(info.PartneralimentatieIngangsdatum));
            AddPlaceholder(replacements, "VOORLOPIGE_ALIMENTATIE_BEDRAG", FormatCurrency(info.HoogtePartneralimentatie));

            // Afkoop
            AddPlaceholder(replacements, "AFKOOP_BEDRAG", FormatCurrency(info.AfkoopBedrag));

            // Bijdrage hypotheekrente
            AddPlaceholder(replacements, "BIJDRAGE_HYPOTHEEKRENTE_BEDRAG", FormatCurrency(info.BijdrageHypotheekrenteBedrag));
            AddPlaceholder(replacements, "BIJDRAGE_HYPOTHEEKRENTE_INGANGSDATUM", FormatDate(info.BijdrageHypotheekrenteIngangsdatum));
            AddPlaceholder(replacements, "BIJDRAGE_HYPOTHEEKRENTE_EINDDATUM", FormatDate(info.BijdrageHypotheekrenteEinddatum));

            // Indexering
            AddPlaceholder(replacements, "INDEXERING_EERSTE_JAAR", info.IndexeringEersteJaar?.ToString() ?? DateTime.Now.AddYears(1).Year.ToString());

            // Afstand tenzij omstandigheid
            AddPlaceholder(replacements, "AFSTAND_TENZIJ_OMSTANDIGHEID", info.AfstandTenzijOmstandigheid ?? "");

            // Wijzigingsomstandigheden
            AddPlaceholder(replacements, "WIJZIGINGSOMSTANDIGHEDEN", info.Wijzigingsomstandigheden ?? "");
            AddPlaceholder(replacements, "GEEN_WIJZIGINGSOMSTANDIGHEDEN", info.GeenWijzigingsomstandigheden ?? "");

            // Contractuele termijn
            AddPlaceholder(replacements, "CONTRACTUELE_TERMIJN_JAREN", info.ContractueleTermijnJaren?.ToString() ?? "");
            AddPlaceholder(replacements, "CONTRACTUELE_TERMIJN_INGANGSDATUM", FormatDate(info.ContractueleTermijnIngangsdatum));

            // Artikel 1:160 afwijking
            AddPlaceholder(replacements, "PERIODE_DOORBETALEN_1160", info.PeriodeDoorbetalen1160 ?? "zes maanden");

            // Condition fields for article selection
            AddPlaceholder(replacements, "duurzaam_gescheiden", info.DuurzaamGescheiden == true ? "true" : "");
            AddPlaceholder(replacements, "alimentatie_berekening_aanhechten", info.AlimentatieBerekeningAanhechten == true ? "true" : "");
            AddPlaceholder(replacements, "berekening_methode", info.BerekeningMethode ?? "");
            AddPlaceholder(replacements, "verdiencapaciteit_type", info.VerdiencapaciteitType ?? "");
            AddPlaceholder(replacements, "partneralimentatie_betaler", info.PartneralimentatieBetaler ?? "");
            AddPlaceholder(replacements, "partneralimentatie_van_toepassing", !string.IsNullOrEmpty(info.PartneralimentatieBetaler) && info.PartneralimentatieBetaler != "geen" ? "true" : "");
            AddPlaceholder(replacements, "afstand_recht", info.AfstandRecht ?? "");
            AddPlaceholder(replacements, "jusvergelijking", info.Jusvergelijking == true ? "true" : "");
            AddPlaceholder(replacements, "bijdrage_hypotheekrente", info.BijdrageHypotheekrente == true ? "true" : "");
            AddPlaceholder(replacements, "partneralimentatie_afkopen", info.PartneralimentatieAfkopen == true ? "true" : "");
            AddPlaceholder(replacements, "afkoop_type", info.AfkoopType ?? "");
            AddPlaceholder(replacements, "niet_wijzigingsbeding", info.NietWijzigingsbeding ?? "");
            AddPlaceholder(replacements, "indexering_type", info.IndexeringType ?? "");
            AddPlaceholder(replacements, "wettelijke_termijn", info.WettelijkeTermijn ?? "");
            AddPlaceholder(replacements, "verlenging_termijn", info.VerlengingTermijn ?? "");
            AddPlaceholder(replacements, "afwijking_1160", info.Afwijking1160 == true ? "true" : "");
            AddPlaceholder(replacements, "hoe_afwijken_1160", info.HoeAfwijken1160 ?? "");
        }

        private void BuildWoningPlaceholders(Dictionary<string, string> replacements, ConvenantInfoData info)
        {
            // Woning adres
            AddPlaceholder(replacements, "WONING_ADRES", info.WoningAdres ?? "");
            AddPlaceholder(replacements, "WONING_STRAAT", info.WoningStraat ?? "");
            AddPlaceholder(replacements, "WONING_HUISNUMMER", info.WoningHuisnummer ?? "");
            AddPlaceholder(replacements, "WONING_POSTCODE", info.WoningPostcode ?? "");
            AddPlaceholder(replacements, "WONING_PLAATS", info.WoningPlaats ?? "");

            // Volledig adres
            var volledigAdres = BuildVolledigWoningAdres(info);
            AddPlaceholder(replacements, "WONING_VOLLEDIG_ADRES", volledigAdres);

            // Woning toedeling
            var woningToegedeeldAan = info.KoopToedeling?.ToLower() switch
            {
                "partij1" => replacements.GetValueOrDefault("PARTIJ1_AANDUIDING", "partij 1"),
                "partij2" => replacements.GetValueOrDefault("PARTIJ2_AANDUIDING", "partij 2"),
                _ => info.KoopToedeling ?? ""
            };
            AddPlaceholder(replacements, "WONING_TOEGEDEELD_AAN", woningToegedeeldAan);

            // Waarden
            AddPlaceholder(replacements, "WONING_WOZ_WAARDE", FormatCurrency(info.KoopWozWaarde));
            AddPlaceholder(replacements, "WONING_TOEDELING_WAARDE", FormatCurrency(info.KoopToedelingWaarde));
            AddPlaceholder(replacements, "WONING_LAATPRIJS", FormatCurrency(info.KoopLaatprijs));
            AddPlaceholder(replacements, "WONING_OVERBEDELING", FormatCurrency(info.KoopOverbedelingWoning));
            AddPlaceholder(replacements, "WONING_OVERBEDELING_SPAARPRODUCTEN", FormatCurrency(info.KoopOverbedelingSpaarproducten));

            // Notaris
            AddPlaceholder(replacements, "NOTARIS_MR", info.KoopNotarisMr ?? "");
            AddPlaceholder(replacements, "NOTARIS_STANDPLAATS", info.KoopNotarisStandplaats ?? "");
            AddPlaceholder(replacements, "NOTARIS_LEVERING_DATUM", FormatDate(info.KoopNotarisLeveringDatum));

            // Makelaar
            AddPlaceholder(replacements, "MAKELAAR_VERKOOP", info.KoopMakelaarVerkoop ?? "");

            // Ontslag hoofdelijkheid
            AddPlaceholder(replacements, "ONTSLAG_HOOFDELIJKHEID_DATUM", FormatDate(info.KoopOntslagHoofdelijkheidDatum));

            // Huurwoning
            AddPlaceholder(replacements, "HUURRECHT_ANDERE_DATUM", FormatDate(info.HuurrechtAndereDatum));
            AddPlaceholder(replacements, "HUUR_VERPLICHTINGEN_OVERNAME_DATUM", FormatDate(info.HuurVerplichtingenOvernameDatum));

            // Condition fields
            AddPlaceholder(replacements, "woning_soort", info.WoningSoort ?? "");
            AddPlaceholder(replacements, "koop_toedeling", info.KoopToedeling ?? "");
        }

        private string BuildVolledigWoningAdres(ConvenantInfoData info)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(info.WoningStraat))
            {
                var straat = info.WoningStraat;
                if (!string.IsNullOrWhiteSpace(info.WoningHuisnummer))
                    straat += " " + info.WoningHuisnummer;
                if (!string.IsNullOrWhiteSpace(info.WoningToevoeging))
                    straat += info.WoningToevoeging;
                parts.Add(straat);
            }

            if (!string.IsNullOrWhiteSpace(info.WoningPostcode))
                parts.Add(info.WoningPostcode);

            if (!string.IsNullOrWhiteSpace(info.WoningPlaats))
                parts.Add(info.WoningPlaats);

            return string.Join(", ", parts);
        }

        private void BuildKadastraalPlaceholders(Dictionary<string, string> replacements, ConvenantInfoData info)
        {
            AddPlaceholder(replacements, "KADASTRAAL_GEMEENTE", info.KoopKadastraalGemeente ?? "");
            AddPlaceholder(replacements, "KADASTRAAL_SECTIE", info.KoopKadastraalSectie ?? "");
            AddPlaceholder(replacements, "KADASTRAAL_PERCEEL", info.KoopKadastraalPerceel ?? "");
            AddPlaceholder(replacements, "KADASTRAAL_ARE", info.KoopKadastraalAre?.ToString() ?? "");
            AddPlaceholder(replacements, "KADASTRAAL_CENTIARE", info.KoopKadastraalCentiare?.ToString() ?? "");
            AddPlaceholder(replacements, "KADASTRAAL_AANDUIDING", info.KoopKadastraalAanduiding ?? "");
            AddPlaceholder(replacements, "KADASTRAAL_OPPERVLAKTE", info.KoopKadastraalOppervlakte?.ToString() ?? "");

            // Kadastrale notatie volledige tekst
            var kadastraleNotatie = BuildKadastraleNotatie(info);
            AddPlaceholder(replacements, "KADASTRAAL_VOLLEDIGE_NOTATIE", kadastraleNotatie);
        }

        private string BuildKadastraleNotatie(ConvenantInfoData info)
        {
            if (string.IsNullOrWhiteSpace(info.KoopKadastraalGemeente))
                return "";

            var parts = new List<string>();
            parts.Add($"gemeente {info.KoopKadastraalGemeente}");

            if (!string.IsNullOrWhiteSpace(info.KoopKadastraalSectie))
                parts.Add($"sectie {info.KoopKadastraalSectie}");

            if (!string.IsNullOrWhiteSpace(info.KoopKadastraalPerceel))
                parts.Add($"nummer {info.KoopKadastraalPerceel}");

            if (info.KoopKadastraalAre.HasValue || info.KoopKadastraalCentiare.HasValue)
            {
                var oppervlakte = new List<string>();
                if (info.KoopKadastraalAre.HasValue)
                    oppervlakte.Add($"{info.KoopKadastraalAre} are");
                if (info.KoopKadastraalCentiare.HasValue)
                    oppervlakte.Add($"{info.KoopKadastraalCentiare} centiare");
                parts.Add($"groot {string.Join(" en ", oppervlakte)}");
            }

            return string.Join(", ", parts);
        }

        private void BuildHypotheekPlaceholders(Dictionary<string, string> replacements, ConvenantInfoData info)
        {
            // Hypotheek notaris indien anders
            AddPlaceholder(replacements, "HYPOTHEEK_NOTARIS_MR", info.KoopNotarisHypotheekMr ?? info.KoopNotarisMr ?? "");
            AddPlaceholder(replacements, "HYPOTHEEK_NOTARIS_STANDPLAATS", info.KoopNotarisHypotheekStandplaats ?? info.KoopNotarisStandplaats ?? "");
            AddPlaceholder(replacements, "HYPOTHEEK_NOTARIS_DATUM", FormatDate(info.KoopNotarisHypotheekDatum ?? info.KoopNotarisLeveringDatum));

            // Privevermogen vordering
            AddPlaceholder(replacements, "PRIVEVERMOGEN_VORDERING_BEDRAG", FormatCurrency(info.KoopPrivevermogenVorderingBedrag));
            AddPlaceholder(replacements, "PRIVEVERMOGEN_REDEN", info.KoopPrivevermogenReden ?? "");
        }

        private void BuildVermogensverdelingPlaceholders(Dictionary<string, string> replacements, ConvenantInfoData info)
        {
            // JSON data wordt door aparte generators verwerkt
            // Hier alleen de condition fields
            AddPlaceholder(replacements, "INBOEDEL", info.Inboedel ?? "");
            AddPlaceholder(replacements, "VERMOGENSVERDELING_OPMERKINGEN", info.VermogensverdelingOpmerkingen ?? "");
        }

        private void BuildPensioenPlaceholders(Dictionary<string, string> replacements, ConvenantInfoData info)
        {
            AddPlaceholder(replacements, "PENSIOEN_OPMERKINGEN", info.PensioenOpmerkingen ?? "");
            AddPlaceholder(replacements, "BIJZONDER_PARTNERPENSIOEN", info.BijzonderPartnerpensioen ?? "");
            AddPlaceholder(replacements, "BIJZONDER_PARTNERPENSIOEN_BEDRAG", info.BijzonderPartnerpensioenbedrag ?? "");
        }

        private void BuildKwijtingPlaceholders(Dictionary<string, string> replacements, ConvenantInfoData info)
        {
            AddPlaceholder(replacements, "HUWELIJKSGOEDERENREGIME", info.Huwelijksgoederenregime ?? "");
            AddPlaceholder(replacements, "HUWELIJKSGOEDERENREGIME_UITZONDERING", info.HuwelijksgoederenregimeUitzondering ?? "");
            AddPlaceholder(replacements, "HUWELIJKSGOEDERENREGIME_ANDERS", info.HuwelijksgoederenregimeAnders ?? "");
            AddPlaceholder(replacements, "HUWELIJKSVOORWAARDEN_DATUM", FormatDate(info.HuwelijksvoorwaardenDatum));
            AddPlaceholder(replacements, "HUWELIJKSVOORWAARDEN_NOTARIS", info.HuwelijksvoorwaardenNotaris ?? "");
            AddPlaceholder(replacements, "HUWELIJKSVOORWAARDEN_PLAATS", info.HuwelijksvoorwaardenNotarisPlaats ?? "");
            AddPlaceholder(replacements, "SLOTBEPALINGEN", info.Slotbepalingen ?? "");

            // Condition fields for article selection
            AddPlaceholder(replacements, "huwelijksgoederenregime", info.Huwelijksgoederenregime ?? "");
        }

        private void BuildOndertekeningPlaceholders(Dictionary<string, string> replacements, ConvenantInfoData info)
        {
            AddPlaceholder(replacements, "ONDERTEKEN_PLAATS_PARTIJ1", info.OndertekeningPlaatsPartij1 ?? "");
            AddPlaceholder(replacements, "ONDERTEKEN_PLAATS_PARTIJ2", info.OndertekeningPlaatsPartij2 ?? "");
            AddPlaceholder(replacements, "ONDERTEKEN_DATUM_PARTIJ1", FormatDate(info.OndertekeningDatumPartij1));
            AddPlaceholder(replacements, "ONDERTEKEN_DATUM_PARTIJ2", FormatDate(info.OndertekeningDatumPartij2));
        }

        private void BuildConsideransPlaceholders(Dictionary<string, string> replacements, DossierData data, ConvenantInfoData info)
        {
            // Huwelijksgegevens
            AddPlaceholder(replacements, "HUWELIJKSDATUM", FormatDate(info.Huwelijksdatum));
            AddPlaceholder(replacements, "HUWELIJKSPLAATS", info.Huwelijksplaats ?? "");

            // Mediation
            AddPlaceholder(replacements, "MEDIATOR_NAAM", info.MediatorNaam ?? "");
            AddPlaceholder(replacements, "MEDIATOR_PLAATS", info.MediatorPlaats ?? "");
            AddPlaceholder(replacements, "RECHTBANK", info.Rechtbank ?? "");
            AddPlaceholder(replacements, "ADVOCAAT_PARTIJ1", info.AdvocaatPartij1 ?? "");
            AddPlaceholder(replacements, "ADVOCAAT_PARTIJ2", info.AdvocaatPartij2 ?? "");

            // Spaarrekeningen kinderen
            AddPlaceholder(replacements, "SPAARREKENING_KINDEREN_NUMMERS", info.SpaarrekeningKinderenNummers ?? "");

            // Erkenning
            AddPlaceholder(replacements, "ERKENNINGSDATUM", FormatDate(info.Erkenningsdatum));

            // Condition fields
            AddPlaceholder(replacements, "is_mediation", info.IsMediation == true ? "true" : "false");
            AddPlaceholder(replacements, "heeft_vaststellingsovereenkomst", info.HeeftVaststellingsovereenkomst == true ? "true" : "");
            AddPlaceholder(replacements, "heeft_kinderen_uit_huwelijk", info.HeeftKinderenUitHuwelijk == true ? "true" : "");
            AddPlaceholder(replacements, "heeft_kinderen_voor_huwelijk", info.HeeftKinderenVoorHuwelijk == true ? "true" : "");
            AddPlaceholder(replacements, "heeft_spaarrekeningen_kinderen", info.HeeftSpaarrekeningenKinderen == true ? "true" : "");
            AddPlaceholder(replacements, "heeft_kinderen", data.Kinderen.Any() ? "true" : "");

            // Minderjarige kinderen
            var minderjarigen = data.Kinderen.Where(k => IsMinderjarig(k)).ToList();
            AddPlaceholder(replacements, "MINDERJARIGE_KINDEREN_NAMEN", DutchLanguageHelper.FormatList(
                minderjarigen.Select(k => k.Roepnaam ?? k.Voornamen?.Split(' ').FirstOrDefault() ?? k.Achternaam).ToList()));
            AddPlaceholder(replacements, "MINDERJARIGE_KINDEREN_ZIJN_IS", minderjarigen.Count == 1 ? "is" : "zijn");
        }

        private bool IsMinderjarig(ChildData kind)
        {
            if (!kind.GeboorteDatum.HasValue)
                return true; // Assume minderjarig if no date

            var leeftijd = DateTime.Now.Year - kind.GeboorteDatum.Value.Year;
            if (DateTime.Now < kind.GeboorteDatum.Value.AddYears(leeftijd))
                leeftijd--;

            return leeftijd < 18;
        }

        private string FormatDate(DateTime? date)
        {
            if (!date.HasValue)
                return "";

            var nlCulture = new CultureInfo("nl-NL");
            return date.Value.ToString("d MMMM yyyy", nlCulture);
        }

        private string FormatCurrency(decimal? amount)
        {
            if (!amount.HasValue)
                return "";

            return amount.Value.ToString("N2", new CultureInfo("nl-NL"));
        }
    }
}

using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using System.Collections.Generic;
using System.Text;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Builds placeholders for the fiscal regulation section (Article 6) of the convenant.
    /// </summary>
    public class FiscaalPlaceholderBuilder : BasePlaceholderBuilder
    {
        public FiscaalPlaceholderBuilder(ILogger<FiscaalPlaceholderBuilder> logger)
            : base(logger)
        {
        }

        public override int Order => 70; // After FinancieelPlaceholderBuilder (50)

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogDebug("Building fiscal placeholders");

            var fiscaal = data.ConvenantFiscaal;
            var partij1 = data.Partij1;
            var partij2 = data.Partij2;
            var isAnoniem = data.IsAnoniem ?? false;

            // Generate all fiscal paragraphs
            AddPlaceholder(replacements, "FiscaleToetsingTekst", GenerateFiscaleToetsingTekst(fiscaal));
            AddPlaceholder(replacements, "FiscaalPartnerschapTekst", GenerateFiscaalPartnerschapTekst(fiscaal));
            AddPlaceholder(replacements, "EigenWoningTekst", GenerateEigenWoningTekst(fiscaal));
            AddPlaceholder(replacements, "IbOndernemingTekst", GenerateIbOndernemingTekst(fiscaal));
            AddPlaceholder(replacements, "AanmerkelijkBelangTekst", GenerateAanmerkelijkBelangTekst(fiscaal));
            AddPlaceholder(replacements, "TerbeschikkingstellingTekst", GenerateTerbeschikkingstellingTekst(fiscaal));
            AddPlaceholder(replacements, "SchenkbelastingTekst", GenerateSchenkbelastingTekst(fiscaal));
            AddPlaceholder(replacements, "DraagplichtHeffingenTekst", GenerateDraagplichtHeffingenTekst(fiscaal, partij1, partij2, isAnoniem));
            AddPlaceholder(replacements, "VerrekeningLijfrentenTekst", GenerateVerrekeningLijfrentenTekst(fiscaal));
            AddPlaceholder(replacements, "AfkoopVerrekeningTekst", GenerateAfkoopVerrekeningTekst(fiscaal));
            AddPlaceholder(replacements, "OptimalisatieAangiftenTekst", GenerateOptimalisatieAangiftenTekst(fiscaal, partij1, partij2, isAnoniem));
            AddPlaceholder(replacements, "OverigeFiscaleBepalingenTekst", GenerateOverigeFiscaleBepalingenTekst());
        }

        /// <summary>
        /// Gets party designation for convenant context.
        /// Uses "de man"/"de vrouw" when anonymous, otherwise "de vader"/"de moeder".
        /// </summary>
        private string GetConvenantPartijBenaming(PersonData? person, bool isAnoniem)
        {
            if (person == null) return "";

            var geslacht = person.Geslacht?.Trim().ToLowerInvariant();

            if (isAnoniem)
            {
                // For anonymous convenant: use "de man" / "de vrouw"
                return geslacht switch
                {
                    "m" or "man" => "de man",
                    "v" or "vrouw" => "de vrouw",
                    _ => person.Naam
                };
            }
            else
            {
                // For named convenant: use "de vader" / "de moeder"
                return geslacht switch
                {
                    "m" or "man" => "de vader",
                    "v" or "vrouw" => "de moeder",
                    _ => person.Naam
                };
            }
        }

        /// <summary>
        /// Gets party name capitalized for start of sentence.
        /// </summary>
        private string GetConvenantPartijBenamingCap(PersonData? person, bool isAnoniem)
        {
            var benaming = GetConvenantPartijBenaming(person, isAnoniem);
            if (string.IsNullOrEmpty(benaming)) return "";
            return char.ToUpper(benaming[0]) + benaming.Substring(1);
        }

        private string GenerateFiscaleToetsingTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal == null || string.IsNullOrEmpty(fiscaal.FiscaalAdviesKeuze))
                return "";

            return fiscaal.FiscaalAdviesKeuze switch
            {
                "door_adviseur" => $"De in dit artikel opgenomen regeling is geen vervanging voor fiscaal advies. Partijen verklaren door ondertekening van dit convenant dat zij zich hebben laten informeren over de fiscale gevolgen van de regeling door {fiscaal.FiscaalAdviseurNaam ?? "[adviseur]"}.",
                "buiten_mediation" => "De in dit artikel opgenomen regeling is geen vervanging voor fiscaal advies. Partijen verklaren door ondertekening van dit convenant dat zij zich buiten de mediation om hebben laten informeren over de fiscale gevolgen van de regeling.",
                "geen_advies" => "De in dit artikel opgenomen regeling is geen vervanging voor fiscaal advies. Partijen verklaren door ondertekening van dit convenant dat zij ervoor kiezen geen advies in te winnen en dragen de gevolgen daarvan - eventuele nadelige consequenties en gemis aan fiscale voordelen - nadrukkelijk zelf.",
                _ => ""
            };
        }

        private string GenerateFiscaalPartnerschapTekst(ConvenantFiscaalData? fiscaal)
        {
            var sb = new StringBuilder();

            // 6.2.1 Einde fiscaal partnerschap
            sb.AppendLine("6.2.1 Einde fiscaal partnerschap");
            sb.AppendLine("Partijen stellen vast dat zij fiscaal partners blijven tot het moment dat het verzoek tot echtscheiding bij de rechtbank is ingediend én de gemeenschappelijke inschrijving in de Basisregistratie Personen is beëindigd.");

            if (fiscaal?.EigenWoningEinddatumBewust == true)
            {
                sb.AppendLine("Partijen zijn zich bewust van het feit dat in het kader van de fiscale behandeling van de eigen woning een andere einddatum kan gelden dan voor het fiscaal partnerschap.");
            }

            sb.AppendLine();

            // 6.2.2 Fiscaal partnerschap
            sb.AppendLine("6.2.2 Fiscaal partnerschap");
            if (fiscaal?.FiscaalPartnerschapKeuze == "zelfstandig")
            {
                sb.AppendLine("Partijen doen zelfstandig aangifte en maken geen gebruik van de mogelijkheid om het fiscaal partnerschap te verlengen. Er vindt derhalve geen toerekening van inkomensbestanddelen in de zin van artikel 2.17 Wet op de inkomstenbelasting 2001 (hierna: \"Wet IB 2001\") plaats. Partijen verplichten zich om hun aangifte op relevante onderdelen met elkaar af te stemmen. Hieronder wordt in ieder geval, maar niet uitsluitend, verstaan de betaalde en ontvangen partneralimentatie, de inkomsten uit eigen woning en de inkomsten uit vermogen voor zover dit onderlinge vorderingen of schulden betreft.");
            }
            else if (fiscaal?.FiscaalPartnerschapKeuze == "onderling_overleg")
            {
                var adviseur = !string.IsNullOrEmpty(fiscaal.FiscaalPartnerschapAdviseur)
                    ? fiscaal.FiscaalPartnerschapAdviseur
                    : "[adviseur]";
                sb.AppendLine($"Partijen komen overeen dat zij voor het kalenderjaar waarin het fiscaal partnerschap eindigt de aangiften in onderling overleg (laten) doen. Voor zover nodig maken zij in dit jaar gebruik van de mogelijkheid van verlengd fiscaal partnerschap in de zin van artikel 2.17 lid 7 Wet op de inkomstenbelasting 2001 (hierna: \"Wet IB 2001\"), tenzij zij gezamenlijk anders besluiten naar aanleiding van daartoe ingewonnen advies. Partijen zullen dit eventuele advies inwinnen bij en hun aangifte inkomstenbelasting over het betreffende jaar voorleggen aan {adviseur}.");
            }

            sb.AppendLine();

            // 6.2.3 Aangifte na jaar beëindiging fiscaal partnerschap
            sb.AppendLine("6.2.3 Aangifte na jaar beëindiging fiscaal partnerschap");
            sb.AppendLine("Met ingang van het kalenderjaar na verbreking van het fiscaal partnerschap verzorgt ieder de eigen aangifte en stemmen partijen alleen relevante onderdelen op elkaar af. Hieronder wordt in ieder geval, maar niet uitsluitend, verstaan de betaalde en ontvangen partneralimentatie, de inkomsten uit eigen woning en de inkomsten uit vermogen voor zover dit onderlinge vorderingen of schulden betreft.");

            return sb.ToString().TrimEnd();
        }

        private string GenerateEigenWoningTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal?.EigenWoningSectieOpnemen != true)
                return "";

            return "Zolang de voormalige echtelijke woning niet is verkocht of toegedeeld aan een van beide partijen, zullen partijen in hun aangifte zoveel mogelijk gebruik maken van de fiscale regelgeving van artikel 3.111, eerste tot en met vierde lid, van de Wet IB 2001. Uitgangspunt hierbij zal in ieder geval zijn dat een belastingteruggave ter zake van betaalde hypotheekrente materieel toekomt aan degene die de hypotheekrente daadwerkelijk heeft voldaan.";
        }

        private string GenerateIbOndernemingTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal?.IbOndernemingSectieOpnemen != true)
                return "";

            return "Partijen maken gebruik van de fiscaal geruisloze doorschuiving ex artikel 3.59 Wet IB 2001.";
        }

        private string GenerateAanmerkelijkBelangTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal?.AanmerkelijkBelangOpnemen != true)
                return "";

            var vanToepassing = fiscaal.AanmerkelijkBelangVanToepassing == "wel" ? "wel" : "niet";
            var afrekening = fiscaal.AanmerkelijkBelangAfrekening == "wel" ? "een" : "geen";

            return $"Partijen verklaren dat artikel 4.17 Wet inkomstenbelasting met betrekking tot hun situatie {vanToepassing} van toepassing is. Zij zullen {afrekening} beroep doen op de mogelijkheid van afrekening, als opgenomen in artikel 4.38 Wet IB 2001.";
        }

        private string GenerateTerbeschikkingstellingTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal?.TerbeschikkingstellingOpnemen != true)
                return "";

            return fiscaal.TerbeschikkingstellingKeuze switch
            {
                "directe_verschuldigdheid" => "Partijen zullen, voor zover mogelijk, een beroep doen op artikel 3.98d lid 2 Wet IB 2001 waardoor de vervreemding van het ter beschikking gestelde vermogen leidt tot directe verschuldigdheid van belastingheffing.",
                "uitgesteld" => "Partijen constateren dat de belastingheffing als gevolg van toedeling op grond van artikel 3.98d lid 1 Wet IB 2001 wordt uitgesteld/doorgeschoven.",
                _ => ""
            };
        }

        private string GenerateSchenkbelastingTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal?.SchenkbelastingOpnemen != true)
                return "";

            return "Partijen zullen gezamenlijk aangifte schenkbelasting doen en daarbij een beroep doen op de vrijstelling ex artikel 33 sub 5, 7, 12, dan wel artikel 33a, dan wel 35b Successiewet.";
        }

        private string GenerateDraagplichtHeffingenTekst(ConvenantFiscaalData? fiscaal, PersonData? partij1, PersonData? partij2, bool isAnoniem)
        {
            var sb = new StringBuilder();

            // 6.3.1 Periode tot kalenderjaar
            sb.AppendLine("6.3.1 Periode tot kalenderjaar waarin fiscaal partnerschap wordt beëindigd");
            sb.AppendLine(GetDraagplichtZin(fiscaal?.DraagplichtHeffingenTot, fiscaal?.DraagplichtHeffingenTotVerhouding1, fiscaal?.DraagplichtHeffingenTotVerhouding2, partij1, partij2, isAnoniem));
            sb.AppendLine();

            // 6.3.2 Jaar van beëindiging
            sb.AppendLine("6.3.2 Jaar van beëindiging fiscaal partnerschap");
            var draagplichtJaarZin = GetDraagplichtZin(fiscaal?.DraagplichtHeffingenJaar, fiscaal?.DraagplichtHeffingenJaarVerhouding1, fiscaal?.DraagplichtHeffingenJaarVerhouding2, partij1, partij2, isAnoniem);
            if (!string.IsNullOrEmpty(draagplichtJaarZin))
            {
                draagplichtJaarZin += ", tenzij in het kader van het opteren voor verlengd fiscaal partnerschap anders wordt overeengekomen.";
            }
            sb.AppendLine(draagplichtJaarZin);
            sb.AppendLine();

            // 6.3.3 Belastinglatentie
            sb.AppendLine("6.3.3");
            sb.AppendLine("Het voorgaande is niet van toepassing voor eventuele heffingen welke reeds in de vermogensafwikkeling tussen partijen in aanmerking zijn genomen als materiële belastingschuld voor de inkomstenbelasting/premies volksverzekeringen (belastinglatentie), zulks tot het bedrag waarvoor zij in de vermogensafwikkeling in aanmerking zijn genomen. Deze heffingen worden gedragen door de persoon bij wie de belastinglatentie in de vermogensafwikkeling in aanmerking is genomen.");
            sb.AppendLine();

            // 6.3.4 Jaren na beëindiging
            sb.AppendLine("6.3.4 Jaren na beëindiging fiscaal partnerschap");
            sb.AppendLine("De heffingen na het jaar waarin het fiscaal partnerschap wordt beëindigd worden zonder nadere verrekening gedragen door de persoon op wiens naam deze zijn gesteld.");
            sb.AppendLine();

            // 6.3.5 Niet aan periode te herleiden
            sb.AppendLine("6.3.5 Niet aan periode te herleiden");
            sb.AppendLine("Voor zover heffingen niet kunnen worden herleid naar een bepaalde datum, worden deze voor het doel van deze bepalingen pro rata toegerekend aan de hiervoor genoemde perioden.");

            return sb.ToString().TrimEnd();
        }

        private string GetDraagplichtZin(string? keuze, int? verhouding1, int? verhouding2, PersonData? partij1, PersonData? partij2, bool isAnoniem)
        {
            if (string.IsNullOrEmpty(keuze))
                return "De heffingen worden verdeeld zoals partijen overeenkomen.";

            var partij1Naam = GetConvenantPartijBenaming(partij1, isAnoniem);
            var partij2Naam = GetConvenantPartijBenaming(partij2, isAnoniem);

            return keuze switch
            {
                "partij1" => $"De heffingen komen toe aan {partij1Naam}, zulks zonder nadere verrekening.",
                "partij2" => $"De heffingen komen toe aan {partij2Naam}, zulks zonder nadere verrekening.",
                "gelijkelijk" => "De heffingen worden tussen partijen gelijkelijk gedeeld, zulks zonder nadere verrekening.",
                "verhouding" => $"De heffingen worden tussen partijen in de verhouding {verhouding1 ?? 1}:{verhouding2 ?? 1} gedeeld, zulks zonder nadere verrekening.",
                _ => "De heffingen worden verdeeld zoals partijen overeenkomen."
            };
        }

        private string GenerateVerrekeningLijfrentenTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal?.VerrekeningLijfrentenPensioenOpnemen != true)
                return "";

            var jaar = fiscaal.VerrekeningLijfrentenPensioenJaar?.ToString() ?? "[jaar]";

            return $"Met betrekking tot de fiscale gevolgen van de verdeling van de lijfrenteverzekering(en) zoals omschreven in artikel 4.6 van dit convenant en van de verrekening van pensioenaanspraken komen partijen overeen dat zij in het jaar {jaar} alleen met elkaar en dus met uitsluiting van andere belastingplichtigen, zullen opteren voor fiscaal partnerschap in de zin van artikel 2.17 lid 7 Wet IB 2001. De persoonsgebonden aftrek die één der partijen heeft als gevolg van de hiervoor genoemde afspraken wordt overgedragen aan de andere partij op zodanige wijze dat de heffing zo veel mogelijk wordt geneutraliseerd. Partijen zijn zich ervan bewust dat ook bij de overdracht van de persoonsgebonden aftrekpost heffingen verschuldigd zijn. Het bepaalde in lid 1 van dit artikel ten aanzien van de fiscale toetsing is hierop nadrukkelijk van toepassing.";
        }

        private string GenerateAfkoopVerrekeningTekst(ConvenantFiscaalData? fiscaal)
        {
            if (fiscaal?.AfkoopAlimentatieVerrekeningOpnemen != true)
                return "";

            var jaar = fiscaal.AfkoopAlimentatieVerrekeningJaar?.ToString() ?? "[jaar]";

            return $"Met betrekking tot de fiscale gevolgen van de afkoop partneralimentatie zoals omschreven in artikel 2.7 van dit convenant, van de verdeling van de lijfrenteverzekering(en) zoals omschreven in artikel 4.6 van dit convenant en van de verrekening van pensioenaanspraken komen partijen overeen dat zij in het jaar {jaar} alleen met elkaar en dus met uitsluiting van andere belastingplichtigen, zullen opteren voor fiscaal partnerschap in de zin van artikel 2.17 lid 7 Wet IB 2001. De persoonsgebonden aftrek die één der partijen heeft als gevolg van de hiervoor genoemde afspraken wordt overgedragen aan de andere partij op zodanige wijze dat de heffing zo veel mogelijk wordt geneutraliseerd. Partijen zijn zich ervan bewust dat ook bij de overdracht van de persoonsgebonden aftrekpost heffingen verschuldigd zijn. Het bepaalde in lid 1 van dit artikel ten aanzien van de fiscale toetsing is hierop nadrukkelijk van toepassing.";
        }

        private string GenerateOptimalisatieAangiftenTekst(ConvenantFiscaalData? fiscaal, PersonData? partij1, PersonData? partij2, bool isAnoniem)
        {
            if (fiscaal?.OptimalisatieAangiftenOpnemen != true)
                return "";

            var partij1Naam = GetConvenantPartijBenaming(partij1, isAnoniem);
            var partij2Naam = GetConvenantPartijBenaming(partij2, isAnoniem);

            var voordeelVerdeling = fiscaal.OptimalisatieVoordeelVerdeling switch
            {
                "gelijk" => "tussen partijen gelijk verdeeld",
                "partij1" => $"komt toe aan {partij1Naam}",
                "partij2" => $"komt toe aan {partij2Naam}",
                _ => "tussen partijen gelijk verdeeld"
            };

            return $"Partijen laten de mogelijkheid open om, binnen de grenzen van wet- en regelgeving, de aangiften Inkomstenbelasting/Premies Volksverzekeringen afwijkend van het bepaalde in de leden 2 en 3 van dit artikel in te dienen indien dit per saldo leidt tot een lagere heffing. Het voordeel van deze optimalisatie wordt {voordeelVerdeling}. Partijen gaan hierbij uit van de uitgangspunten en rekenmethodes zoals vastgelegd in de \"notitie fiscale optimalisatie aangiften bij scheiding\" welke zij voorafgaand aan de ondertekening van deze overeenkomst hebben ontvangen.";
        }

        private string GenerateOverigeFiscaleBepalingenTekst()
        {
            var sb = new StringBuilder();

            // 6.4.2 Onvoorziene omstandigheden
            sb.AppendLine("6.4.2 Onvoorziene omstandigheden");
            sb.AppendLine("Indien en voor zover de financiële regeling zoals omschreven in deze overeenkomst leidt tot onvoorziene gevolgen voor de heffingen, kan de partij die geconfronteerd wordt met deze onvoorziene gevolgen de andere partij verzoeken om opnieuw in overleg te treden teneinde de overeenkomst op zodanige wijze aan te passen dan wel aan te vullen, dat de onvoorziene gevolgen tot een minimum worden beperkt. De andere partij is gehouden om hieraan mee te werken voor zover deze hier geen financieel nadeel van ondervindt dan wel voor dit financiële nadeel wordt gecompenseerd.");
            sb.AppendLine();

            // 6.4.3 Discussie met de belastingdienst
            sb.AppendLine("6.4.3 Discussie met de belastingdienst");
            sb.AppendLine("Indien over aanslagen inkomstenbelasting over de jaren dat partijen fiscaal partner waren een geschil met de belastingdienst ontstaat, zullen partijen dit geschil in overleg, eventueel met behulp van een fiscaal adviseur, behandelen.");
            sb.AppendLine();
            sb.AppendLine("Indien op enig moment onherroepelijk blijkt dat enige bepaling dan wel een daarin opgenomen waarde van de onderhavige overeenkomst naar het oordeel van de belastingrechter en/of de Belastingdienst voor de toepassing van de fiscale regelgeving niet aanvaardbaar is, wordt de betreffende bepaling dan wel waarde in overleg tussen partijen met terugwerkende kracht vervangen door een naar het oordeel van de Belastingrechter c.q. de Belastingdienst binnen de geldende fiscale wetgeving wel als zodanig aanvaardbare bepaling dan wel waarde. Partijen verbinden zich in die situatie tot het verrichten van al hetgeen te dien aanzien nodig is, waaronder een onderlinge verrekening en medewerking tot aanpassing van onderhavige overeenkomst.");
            sb.AppendLine();

            // 6.4.4 Geen bevoordelingsbedoeling
            sb.AppendLine("6.4.4 Geen bevoordelingsbedoeling");
            sb.AppendLine("Partijen stellen vast dat zij, voor zover in dit echtscheidingsconvenant niet anders is bepaald, over en weer niet de bedoeling hebben om elkaar in het kader van de afwikkeling van hun huwelijk en de regeling omtrent de gevolgen van de scheiding te bevoordelen. Partijen zijn van mening dat de regeling zoals omschreven in deze overeenkomst in lijn is met de onderlinge wettelijke rechten en plichten. Indien en voor zover de regeling zoals beschreven in deze overeenkomst desondanks naar objectieve maatstaven als een bevoordeling van één der partijen wordt aangemerkt, is sprake van de voldoening van een natuurlijke verbintenis. Mocht de belastingdienst desondanks een belaste schenking constateren en tegen dit oordeel zijn alle rechtsmiddelen uitgeput, dan beroepen partijen zich op toepassing van het echtgenotentarief dat volgt uit onderdeel 2 van het besluit van de staatssecretaris van Financiën, de dato 5 juli 2010 (nummer DGB2010/872, laatst aangevuld bij besluit van 29 maart 2018).");

            return sb.ToString().TrimEnd();
        }
    }
}

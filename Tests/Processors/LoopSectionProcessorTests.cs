using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors;

namespace scheidingsdesk_document_generator.Tests.Processors;

public class LoopSectionProcessorTests
{
    #region Helper Methods

    private static DossierData CreateDossierWithPartijen()
    {
        return new DossierData
        {
            Partijen = new List<PersonData>
            {
                new PersonData { Voornamen = "Jan", Tussenvoegsel = "de", Achternaam = "Vries", RolId = 1 },
                new PersonData { Voornamen = "Maria", Achternaam = "Jansen", RolId = 2 }
            }
        };
    }

    private static DossierData CreateDossierWithKinderen()
    {
        var data = CreateDossierWithPartijen();
        data.Kinderen = new List<ChildData>
        {
            new ChildData
            {
                Id = 1, Voornamen = "Sophie", Achternaam = "de Vries", Tussenvoegsel = "de",
                GeboorteDatum = new DateTime(2015, 3, 20), GeboortePlaats = "Amsterdam"
            },
            new ChildData
            {
                Id = 2, Voornamen = "Thomas", Achternaam = "de Vries", Tussenvoegsel = "de",
                GeboorteDatum = new DateTime(2018, 8, 15), GeboortePlaats = "Utrecht"
            }
        };
        data.ConvenantInfo = new ConvenantInfoData
        {
            HeeftKinderenUitHuwelijk = true
        };
        return data;
    }

    #endregion

    #region Bestaande kinderen-logica (regressie)

    [Fact]
    public void Process_KinderenUitHuwelijk_ExpandsPerKind()
    {
        var data = CreateDossierWithKinderen();
        var tekst = "Begin\n[[#KINDEREN_UIT_HUWELIJK]][[SUBBULLET]][[KIND_VOORNAMEN]], geboren te [[KIND_GEBOORTEPLAATS]][[/KINDEREN_UIT_HUWELIJK]]\nEinde";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Sophie, geboren te Amsterdam", result);
        Assert.Contains("Thomas, geboren te Utrecht", result);
        Assert.Contains("Begin", result);
        Assert.Contains("Einde", result);
    }

    [Fact]
    public void Process_KinderenCollectie_LeegVerwijdertBlok()
    {
        var data = CreateDossierWithPartijen(); // Geen kinderen
        var tekst = "Begin\n[[#ALLE_KINDEREN]]Dit moet verdwijnen[[/ALLE_KINDEREN]]\nEinde";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.DoesNotContain("Dit moet verdwijnen", result);
        Assert.Contains("Begin", result);
        Assert.Contains("Einde", result);
    }

    [Fact]
    public void Process_OnbekendeCollectie_VerwijdertBlok()
    {
        var data = CreateDossierWithKinderen();
        var tekst = "Begin\n[[#ONBEKEND]]Verwijder mij[[/ONBEKEND]]\nEinde";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.DoesNotContain("Verwijder mij", result);
    }

    #endregion

    #region Bankrekeningen kinderen

    [Fact]
    public void Process_BankrekeningenKinderen_ExpandsPerItem()
    {
        var data = CreateDossierWithPartijen();
        data.CommunicatieAfspraken = new CommunicatieAfsprakenData
        {
            BankrekeningKinderen = @"[
                {""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""ouder_1"",""bankNaam"":""ABN AMRO""},
                {""iban"":""NL12RABO0123456789"",""tenaamstelling"":""ouders_gezamenlijk"",""bankNaam"":""Rabobank""}
            ]"
        };
        var tekst = "[[#BANKREKENINGEN_KINDEREN]]Rekening [[BANKREKENING_IBAN]] t.n.v. [[BANKREKENING_TENAAMSTELLING]] bij [[BANKREKENING_BANKNAAM]][[/BANKREKENINGEN_KINDEREN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("NL91 ABNA 0417 1643 00", result); // IBAN geformateerd
        Assert.Contains("t.n.v. Jan de Vries", result); // ouder_1 → partij1 naam
        Assert.Contains("bij de ABN AMRO", result);
        Assert.Contains("t.n.v. beide partijen", result); // ouders_gezamenlijk
        Assert.Contains("bij de Rabobank", result);
    }

    [Fact]
    public void Process_BankrekeningenKinderen_LeegVerwijdertBlok()
    {
        var data = CreateDossierWithPartijen();
        data.CommunicatieAfspraken = new CommunicatieAfsprakenData
        {
            BankrekeningKinderen = "[]"
        };
        var tekst = "Begin\n[[#BANKREKENINGEN_KINDEREN]]Dit verdwijnt[[/BANKREKENINGEN_KINDEREN]]\nEinde";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.DoesNotContain("Dit verdwijnt", result);
    }

    [Fact]
    public void Process_BankrekeningenKinderen_NullJsonVerwijdertBlok()
    {
        var data = CreateDossierWithPartijen();
        // Geen CommunicatieAfspraken
        var tekst = "[[#BANKREKENINGEN_KINDEREN]]Verdwijnt[[/BANKREKENINGEN_KINDEREN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Equal("", result.Trim());
    }

    #endregion

    #region Bankrekeningen (vermogensverdeling)

    [Fact]
    public void Process_Bankrekeningen_ExpandsMetSaldoEnStatus()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Bankrekeningen = @"[{""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""partij1"",""bankNaam"":""ABN AMRO"",""saldo"":5000.50,""statusVermogen"":""gemeenschappelijk""}]"
        };
        var tekst = "[[#BANKREKENINGEN]][[BANKREKENING_IBAN]] - [[BANKREKENING_TENAAMSTELLING]] - [[BANKREKENING_SALDO]] - [[BANKREKENING_STATUS]][[/BANKREKENINGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("NL91 ABNA 0417 1643 00", result);
        Assert.Contains("Jan de Vries", result);
        Assert.Contains("Gemeenschappelijk", result);
    }

    #endregion

    #region Voertuigen

    [Fact]
    public void Process_Voertuigen_ExpandsPerItem()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Voertuigen = @"[
                {""soort"":""personenauto"",""kenteken"":""12ABC3"",""tenaamstelling"":""partij1"",""merk"":""PEUGEOT"",""handelsbenaming"":""308 SW"",""statusVermogen"":""gemeenschappelijk""},
                {""soort"":""anders"",""soortAnders"":""Speedboot"",""kenteken"":""AB123CD"",""tenaamstelling"":""partij2"",""statusVermogen"":""uitgesloten""}
            ]"
        };
        var tekst = "[[#VOERTUIGEN]][[VOERTUIG_SOORT]] [[VOERTUIG_KENTEKEN]] ([[VOERTUIG_MERK]] [[VOERTUIG_MODEL]]) t.n.v. [[VOERTUIG_TENAAMSTELLING]][[/VOERTUIGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Personenauto", result);
        Assert.Contains("12ABC3", result);
        Assert.Contains("PEUGEOT", result);
        Assert.Contains("308 SW", result);
        Assert.Contains("Jan de Vries", result);
        Assert.Contains("Speedboot", result); // anders → soortAnders
        Assert.Contains("Maria Jansen", result);
    }

    #endregion

    #region Beleggingen

    [Fact]
    public void Process_Beleggingen_ExpandsPerItem()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Beleggingen = @"[{""soort"":""aandelen"",""instituut"":""degiro"",""tenaamstelling"":""gezamenlijk"",""statusVermogen"":""gemeenschappelijk""}]"
        };
        var tekst = "[[#BELEGGINGEN]][[BELEGGING_SOORT]] bij [[BELEGGING_INSTITUUT]], [[BELEGGING_TENAAMSTELLING]][[/BELEGGINGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Aandelen", result);
        Assert.Contains("Degiro", result);
        Assert.Contains("beide partijen", result);
    }

    #endregion

    #region Verzekeringen

    [Fact]
    public void Process_Verzekeringen_ExpandsPerItem()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Verzekeringen = @"[{""soort"":""lijfrente"",""verzekeringsmaatschappij"":""aegon"",""verzekeringnemer"":""partij2"",""statusVermogen"":""gemeenschappelijk""}]"
        };
        var tekst = "[[#VERZEKERINGEN]][[VERZEKERING_SOORT]] bij [[VERZEKERING_MAATSCHAPPIJ]], nemer: [[VERZEKERING_NEMER]][[/VERZEKERINGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Lijfrente", result);
        Assert.Contains("Aegon", result);
        Assert.Contains("Maria Jansen", result);
    }

    #endregion

    #region Schulden

    [Fact]
    public void Process_Schulden_ExpandsMetBedragEnDraagplichtig()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Schulden = @"[{""soort"":""studieschuld"",""omschrijving"":""DUO master"",""bedrag"":15000,""tenaamstelling"":""partij1"",""draagplichtig"":""partij1"",""statusVermogen"":""uitgesloten""}]"
        };
        var tekst = "[[#SCHULDEN]][[SCHULD_SOORT]]: [[SCHULD_OMSCHRIJVING]], [[SCHULD_BEDRAG]], draagplichtig: [[SCHULD_DRAAGPLICHTIG]][[/SCHULDEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Studieschuld", result);
        Assert.Contains("DUO master", result);
        Assert.Contains("Jan de Vries", result); // draagplichtig partij1
    }

    [Fact]
    public void Process_Schulden_DraagplichtigAflossen()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Schulden = @"[{""soort"":""persoonlijke_lening"",""bedrag"":5000,""tenaamstelling"":""gezamenlijk"",""draagplichtig"":""aflossen""}]"
        };
        var tekst = "[[#SCHULDEN]][[SCHULD_DRAAGPLICHTIG]][[/SCHULDEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("af te lossen", result);
    }

    #endregion

    #region Vorderingen

    [Fact]
    public void Process_Vorderingen_ExpandsPerItem()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Vorderingen = @"[{""soort"":""belastingteruggave"",""omschrijving"":""Teruggaaf 2025"",""bedrag"":1200,""tenaamstelling"":""partij2"",""statusVermogen"":""uitgesloten""}]"
        };
        var tekst = "[[#VORDERINGEN]][[VORDERING_SOORT]]: [[VORDERING_OMSCHRIJVING]][[/VORDERINGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Belastingteruggave", result);
        Assert.Contains("Teruggaaf 2025", result);
    }

    #endregion

    #region Pensioenen

    [Fact]
    public void Process_Pensioenen_ExpandsPerItem()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Pensioenen = @"[{""pensioenmaatschappij"":""abp"",""tenaamstelling"":""partij1"",""verdeling"":""verevenen"",""bijzonderPartnerpensioen"":""standaard""}]"
        };
        var tekst = "[[#PENSIOENEN]][[PENSIOEN_MAATSCHAPPIJ]] t.n.v. [[PENSIOEN_TENAAMSTELLING]], verdeling: [[PENSIOEN_VERDELING]], bpp: [[PENSIOEN_BIJZONDER_PARTNERPENSIOEN]][[/PENSIOENEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Abp", result);
        Assert.Contains("Jan de Vries", result);
        Assert.Contains("Verevenen", result);
        Assert.Contains("Standaard", result);
    }

    [Fact]
    public void Process_Pensioenen_AfwijkenGebruiktAndersVeld()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Pensioenen = @"[{""pensioenmaatschappij"":""pfzw"",""tenaamstelling"":""partij2"",""verdeling"":""conversie"",""bijzonderPartnerpensioen"":""afwijken"",""bijzonderPartnerpensioensAnders"":""Afstand vanaf 65e""}]"
        };
        var tekst = "[[#PENSIOENEN]][[PENSIOEN_BIJZONDER_PARTNERPENSIOEN]][[/PENSIOENEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Afstand vanaf 65e", result);
    }

    #endregion

    #region Conditioneel gedrag (geen per-item variabelen)

    [Fact]
    public void Process_JsonCollectie_ZonderVariabelen_ToontBlokEenmalig()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Bankrekeningen = @"[{""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""partij1"",""bankNaam"":""ABN AMRO""}]"
        };
        var tekst = "[[#BANKREKENINGEN]]Er zijn bankrekeningen gevonden.[[/BANKREKENINGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Er zijn bankrekeningen gevonden.", result);
    }

    #endregion

    #region ResolveEffectiveValue (anders-veld)

    [Fact]
    public void Process_Belegging_AndersVeldGebruikt()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Beleggingen = @"[{""soort"":""anders"",""soortAnders"":""Cryptocurrency wallet"",""instituut"":""anders"",""instituutAnders"":""Binance"",""tenaamstelling"":""partij1""}]"
        };
        var tekst = "[[#BELEGGINGEN]][[BELEGGING_SOORT]] bij [[BELEGGING_INSTITUUT]][[/BELEGGINGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Cryptocurrency wallet", result);
        Assert.Contains("Binance", result);
    }

    #endregion

    #region TranslateTenaamstelling

    [Fact]
    public void Process_Bankrekening_TenaamstellingAnders()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Bankrekeningen = @"[{""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""anders"",""tenaamstellingAnders"":""Op naam van moeder"",""bankNaam"":""ABN AMRO""}]"
        };
        var tekst = "[[#BANKREKENINGEN]][[BANKREKENING_TENAAMSTELLING]][[/BANKREKENINGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Op naam van moeder", result);
    }

    [Fact]
    public void Process_BankrekeningKinderen_KindTenaamstelling()
    {
        var data = CreateDossierWithKinderen();
        data.CommunicatieAfspraken = new CommunicatieAfsprakenData
        {
            BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""kind_1"",""bankNaam"":""ABN AMRO""}]"
        };
        var tekst = "[[#BANKREKENINGEN_KINDEREN]][[BANKREKENING_TENAAMSTELLING]][[/BANKREKENINGEN_KINDEREN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Sophie", result); // Kind met Id=1
    }

    [Fact]
    public void Process_BankrekeningKinderen_KinderenAlleToontAlleenMinderjarigen()
    {
        var data = CreateDossierWithKinderen();
        // Maak Thomas meerderjarig (19 jaar), Sophie blijft minderjarig
        data.Kinderen![1].GeboorteDatum = DateTime.Today.AddYears(-19);
        data.CommunicatieAfspraken = new CommunicatieAfsprakenData
        {
            BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""kinderen_alle"",""bankNaam"":""ABN AMRO""}]"
        };
        var tekst = "[[#BANKREKENINGEN_KINDEREN]][[BANKREKENING_TENAAMSTELLING]][[/BANKREKENINGEN_KINDEREN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Sophie", result);
        Assert.DoesNotContain("Thomas", result);
    }

    [Fact]
    public void Process_BankrekeningKinderen_KinderenAllemaalToontAlleKinderen()
    {
        var data = CreateDossierWithKinderen();
        // Maak Thomas meerderjarig (19 jaar), Sophie blijft minderjarig
        data.Kinderen![1].GeboorteDatum = DateTime.Today.AddYears(-19);
        data.CommunicatieAfspraken = new CommunicatieAfsprakenData
        {
            BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""kinderen_allemaal"",""bankNaam"":""ABN AMRO""}]"
        };
        var tekst = "[[#BANKREKENINGEN_KINDEREN]][[BANKREKENING_TENAAMSTELLING]][[/BANKREKENINGEN_KINDEREN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Sophie", result);
        Assert.Contains("Thomas", result);
    }

    #endregion

    #region IBAN formatting

    [Fact]
    public void Process_IBAN_AlMetSpaties_BlijftCorrect()
    {
        var data = CreateDossierWithPartijen();
        data.CommunicatieAfspraken = new CommunicatieAfsprakenData
        {
            BankrekeningKinderen = @"[{""iban"":""NL91 ABNA 0417 1643 00"",""tenaamstelling"":""ouder_1"",""bankNaam"":""ABN AMRO""}]"
        };
        var tekst = "[[#BANKREKENINGEN_KINDEREN]][[BANKREKENING_IBAN]][[/BANKREKENINGEN_KINDEREN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("NL91 ABNA 0417 1643 00", result);
    }

    #endregion

    #region HumanizeSnakeCase

    [Fact]
    public void Process_StatusVermogen_SnakeCaseNaarLeesbaar()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Schulden = @"[{""soort"":""doorlopend_krediet"",""bedrag"":1000,""tenaamstelling"":""partij1"",""statusVermogen"":""gemeenschappelijk""}]"
        };
        var tekst = "[[#SCHULDEN]][[SCHULD_SOORT]] - [[SCHULD_STATUS]][[/SCHULDEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Doorlopend krediet", result);
        Assert.Contains("Gemeenschappelijk", result);
    }

    #endregion

    #region Meerdere items

    [Fact]
    public void Process_MeerdereItems_AllemaalGeexpandeerd()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Voertuigen = @"[
                {""soort"":""personenauto"",""kenteken"":""12ABC3"",""tenaamstelling"":""partij1"",""merk"":""PEUGEOT"",""handelsbenaming"":""308""},
                {""soort"":""motor"",""kenteken"":""AB123CD"",""tenaamstelling"":""partij2"",""merk"":""BMW"",""handelsbenaming"":""R1200""},
                {""soort"":""bromfiets"",""kenteken"":""ZZ999AA"",""tenaamstelling"":""partij1"",""merk"":""Vespa"",""handelsbenaming"":""Primavera""}
            ]"
        };
        var tekst = "[[#VOERTUIGEN]][[VOERTUIG_MERK]] [[VOERTUIG_MODEL]][[/VOERTUIGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("PEUGEOT 308", result);
        Assert.Contains("BMW R1200", result);
        Assert.Contains("Vespa Primavera", result);
    }

    #endregion

    #region Ongeldige JSON

    [Fact]
    public void Process_OngeldigeJson_VerwijdertBlok()
    {
        var data = CreateDossierWithPartijen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            Bankrekeningen = "dit is geen json"
        };
        var tekst = "Begin\n[[#BANKREKENINGEN]]Inhoud[[/BANKREKENINGEN]]\nEinde";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.DoesNotContain("Inhoud", result);
        Assert.Contains("Begin", result);
        Assert.Contains("Einde", result);
    }

    #endregion

    #region Kinderen en JSON collecties samen

    [Fact]
    public void Process_KinderenEnJsonCollectiesSamen()
    {
        var data = CreateDossierWithKinderen();
        data.ConvenantInfo = new ConvenantInfoData
        {
            HeeftKinderenUitHuwelijk = true,
            Voertuigen = @"[{""soort"":""personenauto"",""kenteken"":""12ABC3"",""tenaamstelling"":""partij1"",""merk"":""PEUGEOT"",""handelsbenaming"":""308""}]"
        };
        var tekst = "Kinderen:\n[[#KINDEREN_UIT_HUWELIJK]][[KIND_VOORNAMEN]][[/KINDEREN_UIT_HUWELIJK]]\nVoertuigen:\n[[#VOERTUIGEN]][[VOERTUIG_MERK]][[/VOERTUIGEN]]";

        var result = LoopSectionProcessor.Process(tekst, data);

        Assert.Contains("Sophie", result);
        Assert.Contains("Thomas", result);
        Assert.Contains("PEUGEOT", result);
    }

    #endregion

    #region Null/empty input handling

    [Fact]
    public void Process_NullTekst_ReturnsNull()
    {
        var result = LoopSectionProcessor.Process(null!, new DossierData());
        Assert.Null(result);
    }

    [Fact]
    public void Process_EmptyTekst_ReturnsEmpty()
    {
        var result = LoopSectionProcessor.Process("", new DossierData());
        Assert.Equal("", result);
    }

    [Fact]
    public void Process_NullDossierData_ReturnsTekst()
    {
        var result = LoopSectionProcessor.Process("Onveranderd", null);
        Assert.Equal("Onveranderd", result);
    }

    #endregion
}

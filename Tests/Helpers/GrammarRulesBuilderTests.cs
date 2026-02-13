using Microsoft.Extensions.Logging;
using Moq;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;

namespace scheidingsdesk_document_generator.Tests.Helpers;

public class GrammarRulesBuilderTests
{
    private readonly Mock<ILogger<GrammarRulesBuilder>> _loggerMock;
    private readonly GrammarRulesBuilder _builder;

    public GrammarRulesBuilderTests()
    {
        _loggerMock = new Mock<ILogger<GrammarRulesBuilder>>();
        _builder = new GrammarRulesBuilder(_loggerMock.Object);
    }

    #region BuildRules with Children Tests

    [Fact]
    public void BuildRules_WithNoChildren_ReturnsGenericTerms()
    {
        var children = new List<ChildData>();

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("het kind", rules["KIND"]);
        Assert.Equal("de kinderen", rules["KINDEREN"]);
        Assert.Equal("ons kind", rules["ons kind/onze kinderen"]);
    }

    [Fact]
    public void BuildRules_WithOneMinorChild_ReturnsSingularForms()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Emma",
                GeboorteDatum = DateTime.Today.AddYears(-10) // 10 jaar oud
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("Emma", rules["KIND"]);
        Assert.Equal("ons kind", rules["ons kind/onze kinderen"]);
        Assert.Equal("het kind", rules["het kind/de kinderen"]);
        Assert.Equal("kind", rules["kind/kinderen"]);
        Assert.Equal("heeft", rules["heeft/hebben"]);
        Assert.Equal("is", rules["is/zijn"]);
        Assert.Equal("verblijft", rules["verblijft/verblijven"]);
    }

    [Fact]
    public void BuildRules_WithTwoMinorChildren_ReturnsPluralForms()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Emma",
                GeboorteDatum = DateTime.Today.AddYears(-10)
            },
            new ChildData
            {
                Roepnaam = "Lucas",
                GeboorteDatum = DateTime.Today.AddYears(-8)
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("Emma en Lucas", rules["KIND"]);
        Assert.Equal("Emma en Lucas", rules["KINDEREN"]);
        Assert.Equal("onze kinderen", rules["ons kind/onze kinderen"]);
        Assert.Equal("de kinderen", rules["het kind/de kinderen"]);
        Assert.Equal("kinderen", rules["kind/kinderen"]);
        Assert.Equal("hebben", rules["heeft/hebben"]);
        Assert.Equal("zijn", rules["is/zijn"]);
        Assert.Equal("verblijven", rules["verblijft/verblijven"]);
    }

    [Fact]
    public void BuildRules_WithThreeMinorChildren_FormatsListCorrectly()
    {
        var children = new List<ChildData>
        {
            new ChildData { Roepnaam = "Emma", GeboorteDatum = DateTime.Today.AddYears(-10) },
            new ChildData { Roepnaam = "Lucas", GeboorteDatum = DateTime.Today.AddYears(-8) },
            new ChildData { Roepnaam = "Sophie", GeboorteDatum = DateTime.Today.AddYears(-5) }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("Emma, Lucas en Sophie", rules["KIND"]);
        Assert.Equal("Emma, Lucas en Sophie", rules["KINDEREN"]);
    }

    [Fact]
    public void BuildRules_WithAdultChild_ExcludesFromCount()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Jan",
                GeboorteDatum = DateTime.Today.AddYears(-20) // 20 jaar = volwassen
            },
            new ChildData
            {
                Roepnaam = "Emma",
                GeboorteDatum = DateTime.Today.AddYears(-10) // 10 jaar = minderjarig
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        // Alleen Emma is minderjarig, dus enkelvoud
        Assert.Equal("Emma", rules["KIND"]);
        Assert.Equal("ons kind", rules["ons kind/onze kinderen"]);
        Assert.Equal("heeft", rules["heeft/hebben"]);
    }

    [Fact]
    public void BuildRules_WithMaleChild_ReturnsMalePronouns()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Lucas",
                Geslacht = "M",
                GeboorteDatum = DateTime.Today.AddYears(-10)
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("hem", rules["hem/haar/hen"]);
        Assert.Equal("hij", rules["hij/zij/ze"]);
    }

    [Fact]
    public void BuildRules_WithFemaleChild_ReturnsFemalePronouns()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Emma",
                Geslacht = "V",
                GeboorteDatum = DateTime.Today.AddYears(-10)
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("haar", rules["hem/haar/hen"]);
        Assert.Equal("zij", rules["hij/zij/ze"]);
    }

    [Fact]
    public void BuildRules_WithMultipleChildren_ReturnsPluralPronouns()
    {
        var children = new List<ChildData>
        {
            new ChildData { Roepnaam = "Emma", Geslacht = "V", GeboorteDatum = DateTime.Today.AddYears(-10) },
            new ChildData { Roepnaam = "Lucas", Geslacht = "M", GeboorteDatum = DateTime.Today.AddYears(-8) }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("hen", rules["hem/haar/hen"]);
        Assert.Equal("ze", rules["hij/zij/ze"]);
        Assert.Equal("hun", rules["zijn/haar/hun"]);
    }

    [Fact]
    public void BuildRules_UsesVoornamenWhenNoRoepnaam()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Voornamen = "Jan Peter",
                Roepnaam = null,
                GeboorteDatum = DateTime.Today.AddYears(-10)
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("Jan", rules["KIND"]); // Eerste voornaam
    }

    #endregion

    #region BuildRules "Alle" Rules Tests

    [Fact]
    public void BuildRules_AlleRules_WithOneMinorAndOneAdult_ReturnsPluralForAlleAndSingularForRegular()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Jan",
                GeboorteDatum = DateTime.Today.AddYears(-20) // volwassen
            },
            new ChildData
            {
                Roepnaam = "Emma",
                GeboorteDatum = DateTime.Today.AddYears(-10) // minderjarig
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        // Regular rules: 1 minderjarig = enkelvoud
        Assert.Equal("heeft", rules["heeft/hebben"]);
        Assert.Equal("is", rules["is/zijn"]);
        Assert.Equal("ons kind", rules["ons kind/onze kinderen"]);
        Assert.Equal("kind", rules["kind/kinderen"]);

        // "Alle" rules: 2 totaal = meervoud
        Assert.Equal("hebben", rules["alle heeft/hebben"]);
        Assert.Equal("zijn", rules["alle is/zijn"]);
        Assert.Equal("onze kinderen", rules["alle ons kind/onze kinderen"]);
        Assert.Equal("kinderen", rules["alle kind/kinderen"]);
        Assert.Equal("de kinderen", rules["alle het kind/de kinderen"]);
        Assert.Equal("verblijven", rules["alle verblijft/verblijven"]);
        Assert.Equal("kunnen", rules["alle kan/kunnen"]);
        Assert.Equal("zullen", rules["alle zal/zullen"]);
        Assert.Equal("moeten", rules["alle moet/moeten"]);
        Assert.Equal("worden", rules["alle wordt/worden"]);
    }

    [Fact]
    public void BuildRules_AlleRules_WithOneChildTotal_ReturnsSingular()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Emma",
                GeboorteDatum = DateTime.Today.AddYears(-10)
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        // Both regular and "alle" should be singular
        Assert.Equal("heeft", rules["heeft/hebben"]);
        Assert.Equal("heeft", rules["alle heeft/hebben"]);
        Assert.Equal("is", rules["alle is/zijn"]);
        Assert.Equal("ons kind", rules["alle ons kind/onze kinderen"]);
        Assert.Equal("het kind", rules["alle het kind/de kinderen"]);
        Assert.Equal("kind", rules["alle kind/kinderen"]);
    }

    [Fact]
    public void BuildRules_AllePronouns_WithMultipleChildren_ReturnsPluralPronouns()
    {
        var children = new List<ChildData>
        {
            new ChildData { Roepnaam = "Jan", Geslacht = "M", GeboorteDatum = DateTime.Today.AddYears(-20) },
            new ChildData { Roepnaam = "Emma", Geslacht = "V", GeboorteDatum = DateTime.Today.AddYears(-10) }
        };

        var rules = _builder.BuildRules(children, "test-123");

        // "Alle" pronouns: 2 total = plural
        Assert.Equal("hen", rules["alle hem/haar/hen"]);
        Assert.Equal("ze", rules["alle hij/zij/ze"]);
        Assert.Equal("hun", rules["alle zijn/haar/hun"]);
        Assert.Equal("hun", rules["alle diens/dier/hun"]);
    }

    [Fact]
    public void BuildRules_AllePronouns_WithOneMaleChild_ReturnsMalePronouns()
    {
        var children = new List<ChildData>
        {
            new ChildData
            {
                Roepnaam = "Lucas",
                Geslacht = "M",
                GeboorteDatum = DateTime.Today.AddYears(-10)
            }
        };

        var rules = _builder.BuildRules(children, "test-123");

        Assert.Equal("hem", rules["alle hem/haar/hen"]);
        Assert.Equal("hij", rules["alle hij/zij/ze"]);
    }

    #endregion

    #region BuildSimpleRules Tests

    [Fact]
    public void BuildSimpleRules_WithZeroChildren_ReturnsSingularForms()
    {
        var rules = _builder.BuildSimpleRules(0, "test-123");

        Assert.Equal("het kind", rules["KIND"]);
        Assert.Equal("ons kind", rules["ons kind/onze kinderen"]);
        Assert.Equal("heeft", rules["heeft/hebben"]);
    }

    [Fact]
    public void BuildSimpleRules_WithOneChild_ReturnsSingularForms()
    {
        var rules = _builder.BuildSimpleRules(1, "test-123");

        Assert.Equal("het kind", rules["KIND"]);
        Assert.Equal("ons kind", rules["ons kind/onze kinderen"]);
        Assert.Equal("heeft", rules["heeft/hebben"]);
        Assert.Equal("is", rules["is/zijn"]);
        Assert.Equal("hem/haar", rules["hem/haar/hen"]);
        Assert.Equal("hij/zij", rules["hij/zij/ze"]);
    }

    [Fact]
    public void BuildSimpleRules_WithTwoChildren_ReturnsPluralForms()
    {
        var rules = _builder.BuildSimpleRules(2, "test-123");

        Assert.Equal("de kinderen", rules["KIND"]);
        Assert.Equal("onze kinderen", rules["ons kind/onze kinderen"]);
        Assert.Equal("hebben", rules["heeft/hebben"]);
        Assert.Equal("zijn", rules["is/zijn"]);
        Assert.Equal("hen", rules["hem/haar/hen"]);
        Assert.Equal("ze", rules["hij/zij/ze"]);
        Assert.Equal("hun", rules["zijn/haar/hun"]);
    }

    [Fact]
    public void BuildSimpleRules_ContainsAllVerbForms()
    {
        var rules = _builder.BuildSimpleRules(1, "test-123");

        // Check all verb forms are present
        Assert.Contains("heeft/hebben", rules.Keys);
        Assert.Contains("is/zijn", rules.Keys);
        Assert.Contains("verblijft/verblijven", rules.Keys);
        Assert.Contains("kan/kunnen", rules.Keys);
        Assert.Contains("zal/zullen", rules.Keys);
        Assert.Contains("moet/moeten", rules.Keys);
        Assert.Contains("wordt/worden", rules.Keys);
        Assert.Contains("blijft/blijven", rules.Keys);
        Assert.Contains("gaat/gaan", rules.Keys);
        Assert.Contains("komt/komen", rules.Keys);
        Assert.Contains("zou/zouden", rules.Keys);
        Assert.Contains("wil/willen", rules.Keys);
        Assert.Contains("mag/mogen", rules.Keys);
        Assert.Contains("doet/doen", rules.Keys);
        Assert.Contains("krijgt/krijgen", rules.Keys);
        Assert.Contains("neemt/nemen", rules.Keys);
        Assert.Contains("brengt/brengen", rules.Keys);
        Assert.Contains("haalt/halen", rules.Keys);
    }

    [Theory]
    [InlineData(1, "zou")]
    [InlineData(2, "zouden")]
    [InlineData(1, "wil")]
    [InlineData(2, "willen")]
    [InlineData(1, "mag")]
    [InlineData(2, "mogen")]
    public void BuildSimpleRules_NewVerbForms_ReturnCorrectForm(int childCount, string expected)
    {
        var rules = _builder.BuildSimpleRules(childCount, "test-123");

        var key = childCount == 1 ? expected : expected.Replace("en", "").Replace("d", "");
        // Find the matching key
        var verbKey = rules.Keys.FirstOrDefault(k => rules[k] == expected);
        Assert.NotNull(verbKey);
        Assert.Equal(expected, rules[verbKey]);
    }

    [Fact]
    public void BuildSimpleRules_ContainsAllAlleVerbForms()
    {
        var rules = _builder.BuildSimpleRules(1, "test-123");

        Assert.Contains("alle ons kind/onze kinderen", rules.Keys);
        Assert.Contains("alle het kind/de kinderen", rules.Keys);
        Assert.Contains("alle kind/kinderen", rules.Keys);
        Assert.Contains("alle heeft/hebben", rules.Keys);
        Assert.Contains("alle is/zijn", rules.Keys);
        Assert.Contains("alle verblijft/verblijven", rules.Keys);
        Assert.Contains("alle kan/kunnen", rules.Keys);
        Assert.Contains("alle zal/zullen", rules.Keys);
        Assert.Contains("alle moet/moeten", rules.Keys);
        Assert.Contains("alle wordt/worden", rules.Keys);
        Assert.Contains("alle blijft/blijven", rules.Keys);
        Assert.Contains("alle gaat/gaan", rules.Keys);
        Assert.Contains("alle komt/komen", rules.Keys);
        Assert.Contains("alle zou/zouden", rules.Keys);
        Assert.Contains("alle wil/willen", rules.Keys);
        Assert.Contains("alle mag/mogen", rules.Keys);
        Assert.Contains("alle doet/doen", rules.Keys);
        Assert.Contains("alle krijgt/krijgen", rules.Keys);
        Assert.Contains("alle neemt/nemen", rules.Keys);
        Assert.Contains("alle brengt/brengen", rules.Keys);
        Assert.Contains("alle haalt/halen", rules.Keys);
        Assert.Contains("alle hem/haar/hen", rules.Keys);
        Assert.Contains("alle hij/zij/ze", rules.Keys);
        Assert.Contains("alle zijn/haar/hun", rules.Keys);
        Assert.Contains("alle diens/dier/hun", rules.Keys);
    }

    #endregion

    #region AddCollectionGrammarRules Tests

    [Fact]
    public void AddCollectionGrammarRules_EenBankrekening_ReturnsSingular()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300"",""tenaamstelling"":""ouder_1"",""bankNaam"":""ABN AMRO""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("bankrekening", rules["bankrekening/bankrekeningen"]);
        Assert.Equal("saldo", rules["saldo/saldi"]);
        Assert.Equal("het saldo", rules["het saldo/de saldi"]);
        Assert.Equal("rekeningnummer", rules["rekeningnummer/rekeningnummers"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_TweeBankrekeningen_ReturnsPlural()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300""},{""iban"":""NL12RABO0123456789""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("bankrekeningen", rules["bankrekening/bankrekeningen"]);
        Assert.Equal("saldi", rules["saldo/saldi"]);
        Assert.Equal("de saldi", rules["het saldo/de saldi"]);
        Assert.Equal("rekeningnummers", rules["rekeningnummer/rekeningnummers"]);
        Assert.Equal("vallen", rules["valt/vallen"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_EenBankrekening_ValtEnkelvoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("valt", rules["valt/vallen"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_LegeCollectie_VoegtGeenRegelssToe()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = "[]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.DoesNotContain("bankrekening/bankrekeningen", rules.Keys);
        Assert.DoesNotContain("saldo/saldi", rules.Keys);
    }

    [Fact]
    public void AddCollectionGrammarRules_NullJson_VoegtGeenRegelsToe()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData(); // Geen CommunicatieAfspraken

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.DoesNotContain("bankrekening/bankrekeningen", rules.Keys);
    }

    [Fact]
    public void AddCollectionGrammarRules_Voertuigen_EnkelvoudMeervoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            ConvenantInfo = new ConvenantInfoData
            {
                Voertuigen = @"[{""soort"":""personenauto""},{""soort"":""motor""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("voertuigen", rules["voertuig/voertuigen"]);
        Assert.Equal("de voertuigen", rules["het voertuig/de voertuigen"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_Pensioenen_EnkelvoudMeervoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            ConvenantInfo = new ConvenantInfoData
            {
                Pensioenen = @"[{""pensioenmaatschappij"":""abp""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("pensioen", rules["pensioen/pensioenen"]);
        Assert.Equal("het pensioen", rules["het pensioen/de pensioenen"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_Schulden_Meervoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            ConvenantInfo = new ConvenantInfoData
            {
                Schulden = @"[{""soort"":""studieschuld""},{""soort"":""lening_familie""},{""soort"":""creditcard""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("schulden", rules["schuld/schulden"]);
        Assert.Equal("de schulden", rules["de schuld/de schulden"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_Verzekeringen_MetPolissen()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            ConvenantInfo = new ConvenantInfoData
            {
                Verzekeringen = @"[{""soort"":""lijfrente""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("verzekering", rules["verzekering/verzekeringen"]);
        Assert.Equal("polis", rules["polis/polissen"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_MeerdereCollecties_Tegelijk()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA""}]"
            },
            ConvenantInfo = new ConvenantInfoData
            {
                Voertuigen = @"[{""soort"":""auto""},{""soort"":""motor""}]",
                Pensioenen = @"[{""maatschappij"":""abp""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("bankrekening", rules["bankrekening/bankrekeningen"]); // 1 item
        Assert.Equal("voertuigen", rules["voertuig/voertuigen"]); // 2 items
        Assert.Equal("pensioen", rules["pensioen/pensioenen"]); // 1 item
    }

    [Fact]
    public void AddCollectionGrammarRules_GedeeldeSleutel_EersteCollectieWint()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            // BANKREKENINGEN_KINDEREN: 1 item (enkelvoud)
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA""}]"
            },
            // BANKREKENINGEN: 3 items (meervoud)
            ConvenantInfo = new ConvenantInfoData
            {
                Bankrekeningen = @"[{""iban"":""A""},{""iban"":""B""},{""iban"":""C""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        // BANKREKENINGEN_KINDEREN staat eerst in de registry → enkelvoud wint
        Assert.Equal("bankrekening", rules["bankrekening/bankrekeningen"]);
        Assert.Equal("saldo", rules["saldo/saldi"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_OngeldigeJson_WerdtOvergeslagen()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            ConvenantInfo = new ConvenantInfoData
            {
                Voertuigen = "geen geldige json"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.DoesNotContain("voertuig/voertuigen", rules.Keys);
    }

    [Fact]
    public void AddCollectionGrammarRules_EenBankrekening_StaatEnkelvoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("staat", rules["staat/staan"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_TweeBankrekeningen_StaanMeervoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA""},{""iban"":""NL12RABO""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("staan", rules["staat/staan"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_EenBankrekening_RekeningBlijftEnkelvoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("rekening blijft", rules["rekening blijft/rekeningen blijven"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_TweeBankrekeningen_RekeningenBlijvenMeervoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA""},{""iban"":""NL12RABO""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("rekeningen blijven", rules["rekening blijft/rekeningen blijven"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_EenBankrekening_RekeningZalEnkelvoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA0417164300""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("rekening zal", rules["rekening zal/rekeningen zullen"]);
    }

    [Fact]
    public void AddCollectionGrammarRules_TweeBankrekeningen_RekeningenZullenMeervoud()
    {
        var rules = new Dictionary<string, string>();
        var data = new DossierData
        {
            CommunicatieAfspraken = new CommunicatieAfsprakenData
            {
                BankrekeningKinderen = @"[{""iban"":""NL91ABNA""},{""iban"":""NL12RABO""}]"
            }
        };

        _builder.AddCollectionGrammarRules(rules, data, "test-123");

        Assert.Equal("rekeningen zullen", rules["rekening zal/rekeningen zullen"]);
    }

    #endregion
}

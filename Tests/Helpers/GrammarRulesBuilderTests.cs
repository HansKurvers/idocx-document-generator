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

    #endregion
}

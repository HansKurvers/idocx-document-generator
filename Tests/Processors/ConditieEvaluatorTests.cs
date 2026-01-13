using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors;

namespace scheidingsdesk_document_generator.Tests.Processors;

public class ConditieEvaluatorTests
{
    private readonly Mock<ILogger<ConditieEvaluator>> _loggerMock;
    private readonly ConditieEvaluator _evaluator;

    public ConditieEvaluatorTests()
    {
        _loggerMock = new Mock<ILogger<ConditieEvaluator>>();
        _evaluator = new ConditieEvaluator(_loggerMock.Object);
    }

    #region Helper Methods

    private static Conditie CreateSimpleCondition(string veld, string op, object? waarde)
    {
        JsonElement? jsonWaarde = null;
        if (waarde != null)
        {
            var json = JsonSerializer.Serialize(waarde);
            jsonWaarde = JsonDocument.Parse(json).RootElement;
        }

        return new Conditie
        {
            Veld = veld,
            Operator = op,
            Waarde = jsonWaarde
        };
    }

    private static Conditie CreateGroupCondition(string logicalOp, params Conditie[] conditions)
    {
        return new Conditie
        {
            Operator = logicalOp,
            Voorwaarden = conditions.ToList()
        };
    }

    private static ConditieConfig CreateConfig(string defaultValue, params (Conditie conditie, string resultaat)[] rules)
    {
        return new ConditieConfig
        {
            Default = defaultValue,
            Regels = rules.Select(r => new ConditieRegel
            {
                Conditie = r.conditie,
                Resultaat = r.resultaat
            }).ToList()
        };
    }

    #endregion

    #region Equals Operator Tests

    [Fact]
    public void Evaluate_EqualsString_MatchesCorrectly()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Status", "=", "actief"), "Actief dossier")
        );
        var context = new Dictionary<string, object> { { "Status", "actief" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Actief dossier", result.RawResult);
    }

    [Fact]
    public void Evaluate_EqualsString_CaseInsensitive()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Status", "=", "ACTIEF"), "Match")
        );
        var context = new Dictionary<string, object> { { "Status", "actief" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Match", result.RawResult);
    }

    [Fact]
    public void Evaluate_EqualsNumber_MatchesCorrectly()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("AantalKinderen", "=", 2), "Twee kinderen")
        );
        var context = new Dictionary<string, object> { { "AantalKinderen", 2 } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Twee kinderen", result.RawResult);
    }

    [Fact]
    public void Evaluate_EqualsBool_MatchesCorrectly()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("IsAnoniem", "=", true), "Anoniem dossier")
        );
        var context = new Dictionary<string, object> { { "IsAnoniem", true } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Anoniem dossier", result.RawResult);
    }

    [Fact]
    public void Evaluate_EqualsBool_MatchesJaNee()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("IsAnoniem", "=", true), "Anoniem")
        );
        var context = new Dictionary<string, object> { { "IsAnoniem", "ja" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    #endregion

    #region NotEquals Operator Tests

    [Fact]
    public void Evaluate_NotEquals_MatchesWhenDifferent()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Status", "!=", "gesloten"), "Niet gesloten")
        );
        var context = new Dictionary<string, object> { { "Status", "actief" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Niet gesloten", result.RawResult);
    }

    [Fact]
    public void Evaluate_NotEquals_DoesNotMatchWhenSame()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Status", "!=", "actief"), "Niet actief")
        );
        var context = new Dictionary<string, object> { { "Status", "actief" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Null(result.MatchedRule);
        Assert.Equal("default", result.RawResult);
    }

    #endregion

    #region Comparison Operators Tests

    [Theory]
    [InlineData(5, ">", 3, true)]
    [InlineData(3, ">", 5, false)]
    [InlineData(3, ">", 3, false)]
    [InlineData(5, ">=", 3, true)]
    [InlineData(3, ">=", 3, true)]
    [InlineData(2, ">=", 3, false)]
    [InlineData(3, "<", 5, true)]
    [InlineData(5, "<", 3, false)]
    [InlineData(3, "<", 3, false)]
    [InlineData(3, "<=", 5, true)]
    [InlineData(3, "<=", 3, true)]
    [InlineData(5, "<=", 3, false)]
    public void Evaluate_ComparisonOperators_WorkCorrectly(int fieldValue, string op, int compareValue, bool shouldMatch)
    {
        var config = CreateConfig(
            "no match",
            (CreateSimpleCondition("Aantal", op, compareValue), "match")
        );
        var context = new Dictionary<string, object> { { "Aantal", fieldValue } };

        var result = _evaluator.Evaluate(config, context);

        if (shouldMatch)
        {
            Assert.Equal(1, result.MatchedRule);
            Assert.Equal("match", result.RawResult);
        }
        else
        {
            Assert.Null(result.MatchedRule);
            Assert.Equal("no match", result.RawResult);
        }
    }

    #endregion

    #region Empty/NotEmpty Tests

    [Fact]
    public void Evaluate_Empty_MatchesNullValue()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Opmerking", "leeg", null), "Leeg veld")
        );
        var context = new Dictionary<string, object>(); // Field not present

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Leeg veld", result.RawResult);
    }

    [Fact]
    public void Evaluate_Empty_MatchesEmptyString()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Opmerking", "leeg", null), "Leeg veld")
        );
        var context = new Dictionary<string, object> { { "Opmerking", "" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    [Fact]
    public void Evaluate_NotEmpty_MatchesFilledValue()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Naam", "niet_leeg", null), "Heeft naam")
        );
        var context = new Dictionary<string, object> { { "Naam", "Jan" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Heeft naam", result.RawResult);
    }

    [Fact]
    public void Evaluate_NotEmpty_DoesNotMatchEmptyValue()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Naam", "niet_leeg", null), "Heeft naam")
        );
        var context = new Dictionary<string, object> { { "Naam", "" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Null(result.MatchedRule);
    }

    #endregion

    #region String Operators Tests

    [Fact]
    public void Evaluate_Contains_MatchesSubstring()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Email", "bevat", "@gmail"), "Gmail gebruiker")
        );
        var context = new Dictionary<string, object> { { "Email", "jan@gmail.com" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    [Fact]
    public void Evaluate_StartsWith_MatchesPrefix()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Postcode", "begint_met", "10"), "Amsterdam")
        );
        var context = new Dictionary<string, object> { { "Postcode", "1012 AB" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    [Fact]
    public void Evaluate_EndsWith_MatchesSuffix()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("Email", "eindigt_met", ".nl"), "Nederlandse email")
        );
        var context = new Dictionary<string, object> { { "Email", "jan@bedrijf.nl" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    #endregion

    #region In/NotIn Operators Tests

    [Fact]
    public void Evaluate_In_MatchesValueInList()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("SoortRelatie", "in", new[] { "gehuwd", "geregistreerd_partnerschap" }), "Formele relatie")
        );
        var context = new Dictionary<string, object> { { "SoortRelatie", "gehuwd" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
        Assert.Equal("Formele relatie", result.RawResult);
    }

    [Fact]
    public void Evaluate_In_DoesNotMatchValueNotInList()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("SoortRelatie", "in", new[] { "gehuwd", "geregistreerd_partnerschap" }), "Formele relatie")
        );
        var context = new Dictionary<string, object> { { "SoortRelatie", "samenwonend" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Null(result.MatchedRule);
    }

    [Fact]
    public void Evaluate_NotIn_MatchesValueNotInList()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("SoortRelatie", "niet_in", new[] { "gehuwd", "geregistreerd_partnerschap" }), "Informele relatie")
        );
        var context = new Dictionary<string, object> { { "SoortRelatie", "samenwonend" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    #endregion

    #region Group Conditions (AND/OR) Tests

    [Fact]
    public void Evaluate_AndCondition_MatchesWhenAllTrue()
    {
        var andCondition = CreateGroupCondition("AND",
            CreateSimpleCondition("IsAnoniem", "=", false),
            CreateSimpleCondition("AantalKinderen", ">", 0)
        );
        var config = CreateConfig("default", (andCondition, "Niet anoniem met kinderen"));
        var context = new Dictionary<string, object>
        {
            { "IsAnoniem", false },
            { "AantalKinderen", 2 }
        };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    [Fact]
    public void Evaluate_AndCondition_DoesNotMatchWhenOneFalse()
    {
        var andCondition = CreateGroupCondition("AND",
            CreateSimpleCondition("IsAnoniem", "=", false),
            CreateSimpleCondition("AantalKinderen", ">", 0)
        );
        var config = CreateConfig("default", (andCondition, "Niet anoniem met kinderen"));
        var context = new Dictionary<string, object>
        {
            { "IsAnoniem", true }, // This is false condition
            { "AantalKinderen", 2 }
        };

        var result = _evaluator.Evaluate(config, context);

        Assert.Null(result.MatchedRule);
    }

    [Fact]
    public void Evaluate_OrCondition_MatchesWhenOneTrue()
    {
        var orCondition = CreateGroupCondition("OR",
            CreateSimpleCondition("SoortRelatie", "=", "gehuwd"),
            CreateSimpleCondition("SoortRelatie", "=", "geregistreerd_partnerschap")
        );
        var config = CreateConfig("default", (orCondition, "Formele relatie"));
        var context = new Dictionary<string, object> { { "SoortRelatie", "gehuwd" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    [Fact]
    public void Evaluate_OrCondition_DoesNotMatchWhenAllFalse()
    {
        var orCondition = CreateGroupCondition("OR",
            CreateSimpleCondition("SoortRelatie", "=", "gehuwd"),
            CreateSimpleCondition("SoortRelatie", "=", "geregistreerd_partnerschap")
        );
        var config = CreateConfig("default", (orCondition, "Formele relatie"));
        var context = new Dictionary<string, object> { { "SoortRelatie", "samenwonend" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Null(result.MatchedRule);
    }

    [Fact]
    public void Evaluate_NestedConditions_WorkCorrectly()
    {
        // (IsAnoniem = false AND (SoortRelatie = gehuwd OR SoortRelatie = geregistreerd_partnerschap))
        var innerOr = CreateGroupCondition("OR",
            CreateSimpleCondition("SoortRelatie", "=", "gehuwd"),
            CreateSimpleCondition("SoortRelatie", "=", "geregistreerd_partnerschap")
        );
        var outerAnd = CreateGroupCondition("AND",
            CreateSimpleCondition("IsAnoniem", "=", false),
            innerOr
        );
        var config = CreateConfig("default", (outerAnd, "Niet anoniem formele relatie"));
        var context = new Dictionary<string, object>
        {
            { "IsAnoniem", false },
            { "SoortRelatie", "gehuwd" }
        };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    #endregion

    #region Multiple Rules Tests

    [Fact]
    public void Evaluate_MultipleRules_ReturnsFirstMatch()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("AantalKinderen", "=", 0), "Geen kinderen"),
            (CreateSimpleCondition("AantalKinderen", "=", 1), "Een kind"),
            (CreateSimpleCondition("AantalKinderen", ">", 1), "Meerdere kinderen")
        );
        var context = new Dictionary<string, object> { { "AantalKinderen", 1 } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(2, result.MatchedRule); // Second rule matches
        Assert.Equal("Een kind", result.RawResult);
    }

    [Fact]
    public void Evaluate_NoRulesMatch_ReturnsDefault()
    {
        var config = CreateConfig(
            "Standaard waarde",
            (CreateSimpleCondition("Status", "=", "actief"), "Actief"),
            (CreateSimpleCondition("Status", "=", "gesloten"), "Gesloten")
        );
        var context = new Dictionary<string, object> { { "Status", "in_behandeling" } };

        var result = _evaluator.Evaluate(config, context);

        Assert.Null(result.MatchedRule);
        Assert.Equal("Standaard waarde", result.RawResult);
    }

    #endregion

    #region ResolveNestedPlaceholders Tests

    [Fact]
    public void ResolveNestedPlaceholders_ReplacesPlaceholders()
    {
        var text = "De kinderen van [[Partij1Naam]] en [[Partij2Naam]]";
        var replacements = new Dictionary<string, string>
        {
            { "Partij1Naam", "Jan" },
            { "Partij2Naam", "Maria" }
        };

        var result = _evaluator.ResolveNestedPlaceholders(text, replacements);

        Assert.Equal("De kinderen van Jan en Maria", result);
    }

    [Fact]
    public void ResolveNestedPlaceholders_HandlesNestedReplacements()
    {
        var text = "[[Zin]]";
        var replacements = new Dictionary<string, string>
        {
            { "Zin", "Hallo [[Naam]]" },
            { "Naam", "Jan" }
        };

        var result = _evaluator.ResolveNestedPlaceholders(text, replacements);

        Assert.Equal("Hallo Jan", result);
    }

    [Fact]
    public void ResolveNestedPlaceholders_IsCaseInsensitive()
    {
        var text = "[[naam]]";
        var replacements = new Dictionary<string, string>
        {
            { "Naam", "Jan" }
        };

        var result = _evaluator.ResolveNestedPlaceholders(text, replacements);

        Assert.Equal("Jan", result);
    }

    [Fact]
    public void ResolveNestedPlaceholders_LimitsDepth()
    {
        var text = "[[A]]";
        var replacements = new Dictionary<string, string>
        {
            { "A", "[[B]]" },
            { "B", "[[C]]" },
            { "C", "[[D]]" },
            { "D", "[[E]]" },
            { "E", "[[F]]" },
            { "F", "Done" }
        };

        // Default max depth is 5, so it should stop before reaching "Done"
        var result = _evaluator.ResolveNestedPlaceholders(text, replacements, maxDepth: 3);

        // After 3 iterations: A -> B -> C -> D, stops at [[D]]
        Assert.Contains("[[", result); // Should still have unresolved placeholder
    }

    [Fact]
    public void ResolveNestedPlaceholders_HandlesEmptyText()
    {
        var result = _evaluator.ResolveNestedPlaceholders("", new Dictionary<string, string>());
        Assert.Equal("", result);
    }

    [Fact]
    public void ResolveNestedPlaceholders_HandlesNullText()
    {
        var result = _evaluator.ResolveNestedPlaceholders(null!, new Dictionary<string, string>());
        Assert.Null(result);
    }

    #endregion

    #region Context Key Case Insensitivity Tests

    [Fact]
    public void Evaluate_ContextKey_IsCaseInsensitive()
    {
        var config = CreateConfig(
            "default",
            (CreateSimpleCondition("AantalKinderen", "=", 2), "Twee kinderen")
        );
        var context = new Dictionary<string, object> { { "aantalkinderen", 2 } }; // lowercase

        var result = _evaluator.Evaluate(config, context);

        Assert.Equal(1, result.MatchedRule);
    }

    #endregion
}

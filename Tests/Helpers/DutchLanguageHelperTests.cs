using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;

namespace scheidingsdesk_document_generator.Tests.Helpers;

public class DutchLanguageHelperTests
{
    #region FormatList Tests

    [Fact]
    public void FormatList_WithEmptyList_ReturnsEmptyString()
    {
        var result = DutchLanguageHelper.FormatList(new List<string>());
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatList_WithNull_ReturnsEmptyString()
    {
        var result = DutchLanguageHelper.FormatList(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatList_WithOneItem_ReturnsSingleItem()
    {
        var result = DutchLanguageHelper.FormatList(new List<string> { "Emma" });
        Assert.Equal("Emma", result);
    }

    [Fact]
    public void FormatList_WithTwoItems_ReturnsItemsWithEn()
    {
        var result = DutchLanguageHelper.FormatList(new List<string> { "Kees", "Emma" });
        Assert.Equal("Kees en Emma", result);
    }

    [Fact]
    public void FormatList_WithThreeItems_ReturnsCommaAndEn()
    {
        var result = DutchLanguageHelper.FormatList(new List<string> { "Bart", "Kees", "Emma" });
        Assert.Equal("Bart, Kees en Emma", result);
    }

    [Fact]
    public void FormatList_WithFourItems_ReturnsCorrectFormat()
    {
        var result = DutchLanguageHelper.FormatList(new List<string> { "Jan", "Piet", "Klaas", "Emma" });
        Assert.Equal("Jan, Piet, Klaas en Emma", result);
    }

    #endregion

    #region GetObjectPronoun Tests

    [Theory]
    [InlineData("M", false, "hem")]
    [InlineData("Man", false, "hem")]
    [InlineData("V", false, "haar")]
    [InlineData("Vrouw", false, "haar")]
    [InlineData("M", true, "hen")]
    [InlineData("V", true, "hen")]
    [InlineData(null, false, "hem/haar")]
    [InlineData("", false, "hem/haar")]
    [InlineData("Unknown", false, "hem/haar")]
    public void GetObjectPronoun_ReturnsCorrectPronoun(string? geslacht, bool isPlural, string expected)
    {
        var result = DutchLanguageHelper.GetObjectPronoun(geslacht, isPlural);
        Assert.Equal(expected, result);
    }

    #endregion

    #region GetSubjectPronoun Tests

    [Theory]
    [InlineData("M", false, "hij")]
    [InlineData("Man", false, "hij")]
    [InlineData("V", false, "zij")]
    [InlineData("Vrouw", false, "zij")]
    [InlineData("M", true, "ze")]
    [InlineData("V", true, "ze")]
    [InlineData(null, false, "hij/zij")]
    [InlineData("", false, "hij/zij")]
    public void GetSubjectPronoun_ReturnsCorrectPronoun(string? geslacht, bool isPlural, string expected)
    {
        var result = DutchLanguageHelper.GetSubjectPronoun(geslacht, isPlural);
        Assert.Equal(expected, result);
    }

    #endregion

    #region GetChildTerm Tests

    [Fact]
    public void GetChildTerm_WithSingular_ReturnsOnsKind()
    {
        var result = DutchLanguageHelper.GetChildTerm(isPlural: false);
        Assert.Equal("ons kind", result);
    }

    [Fact]
    public void GetChildTerm_WithPlural_ReturnsOnzeKinderen()
    {
        var result = DutchLanguageHelper.GetChildTerm(isPlural: true);
        Assert.Equal("onze kinderen", result);
    }

    #endregion

    #region ToNationalityAdjective Tests

    [Theory]
    [InlineData("Nederlands", "Nederlandse")]
    [InlineData("Belgisch", "Belgische")]
    [InlineData("Duits", "Duitse")]
    [InlineData("Turks", "Turkse")]
    [InlineData("Marokkaans", "Marokkaanse")]
    [InlineData("Surinaams", "Surinaamse")]
    public void ToNationalityAdjective_WithKnownNationality_ReturnsCorrectAdjective(string nationality, string expected)
    {
        var result = DutchLanguageHelper.ToNationalityAdjective(nationality);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToNationalityAdjective_WithNull_ReturnsEmptyString()
    {
        var result = DutchLanguageHelper.ToNationalityAdjective(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToNationalityAdjective_WithEmptyString_ReturnsEmptyString()
    {
        var result = DutchLanguageHelper.ToNationalityAdjective("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToNationalityAdjective_WithUnknownNationality_AddsSuffixE()
    {
        var result = DutchLanguageHelper.ToNationalityAdjective("Onbekend");
        Assert.Equal("Onbekende", result);
    }

    [Fact]
    public void ToNationalityAdjective_IsCaseInsensitive()
    {
        var result = DutchLanguageHelper.ToNationalityAdjective("NEDERLANDS");
        Assert.Equal("Nederlandse", result);
    }

    #endregion

    #region VerbForms Tests

    [Theory]
    [InlineData(false, "heeft")]
    [InlineData(true, "hebben")]
    public void VerbForms_Heeft_Hebben_ReturnsCorrectForm(bool isPlural, string expected)
    {
        var result = DutchLanguageHelper.VerbForms.Heeft_Hebben(isPlural);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false, "is")]
    [InlineData(true, "zijn")]
    public void VerbForms_Is_Zijn_ReturnsCorrectForm(bool isPlural, string expected)
    {
        var result = DutchLanguageHelper.VerbForms.Is_Zijn(isPlural);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false, "zal")]
    [InlineData(true, "zullen")]
    public void VerbForms_Zal_Zullen_ReturnsCorrectForm(bool isPlural, string expected)
    {
        var result = DutchLanguageHelper.VerbForms.Zal_Zullen(isPlural);
        Assert.Equal(expected, result);
    }

    #endregion
}

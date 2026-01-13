using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;

namespace scheidingsdesk_document_generator.Tests.Helpers;

public class DataFormatterTests
{
    #region FormatDate Tests

    [Fact]
    public void FormatDate_WithValidDate_ReturnsFormattedDate()
    {
        var date = new DateTime(2024, 1, 15);
        var result = DataFormatter.FormatDate(date);
        Assert.Equal("15 januari 2024", result);
    }

    [Fact]
    public void FormatDate_WithNull_ReturnsEmptyString()
    {
        var result = DataFormatter.FormatDate(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatDate_WithCustomFormat_UsesCustomFormat()
    {
        var date = new DateTime(2024, 1, 15);
        var result = DataFormatter.FormatDate(date, "dd-MM-yyyy");
        Assert.Equal("15-01-2024", result);
    }

    #endregion

    #region FormatFullName Tests

    [Fact]
    public void FormatFullName_WithAllParts_ReturnsFullName()
    {
        var result = DataFormatter.FormatFullName("Jan", "de", "Vries");
        Assert.Equal("Jan de Vries", result);
    }

    [Fact]
    public void FormatFullName_WithoutTussenvoegsel_ReturnsNameWithoutTussenvoegsel()
    {
        var result = DataFormatter.FormatFullName("Jan", null, "Bakker");
        Assert.Equal("Jan Bakker", result);
    }

    [Fact]
    public void FormatFullName_WithEmptyTussenvoegsel_ReturnsNameWithoutTussenvoegsel()
    {
        var result = DataFormatter.FormatFullName("Maria", "", "Jansen");
        Assert.Equal("Maria Jansen", result);
    }

    [Fact]
    public void FormatFullName_WithComplexTussenvoegsel_ReturnsCorrectName()
    {
        var result = DataFormatter.FormatFullName("Pieter", "van der", "Berg");
        Assert.Equal("Pieter van der Berg", result);
    }

    #endregion

    #region FormatAddress Tests

    [Fact]
    public void FormatAddress_WithAllParts_ReturnsFormattedAddress()
    {
        var result = DataFormatter.FormatAddress("Kerkstraat 1", "1234 AB", "Amsterdam");
        Assert.Equal("Kerkstraat 1, 1234 AB Amsterdam", result);
    }

    [Fact]
    public void FormatAddress_WithoutPostcode_ReturnsAddressWithCity()
    {
        var result = DataFormatter.FormatAddress("Kerkstraat 1", null, "Amsterdam");
        Assert.Equal("Kerkstraat 1, Amsterdam", result);
    }

    [Fact]
    public void FormatAddress_WithOnlyAddress_ReturnsOnlyAddress()
    {
        var result = DataFormatter.FormatAddress("Kerkstraat 1", null, null);
        Assert.Equal("Kerkstraat 1", result);
    }

    [Fact]
    public void FormatAddress_WithAllNull_ReturnsEmptyString()
    {
        var result = DataFormatter.FormatAddress(null, null, null);
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region FormatCurrency Tests

    [Fact]
    public void FormatCurrency_WithValidAmount_ReturnsFormattedCurrency()
    {
        var result = DataFormatter.FormatCurrency(1234.56m);
        Assert.Contains("1.234,56", result); // Dutch format uses . for thousands, , for decimals
    }

    [Fact]
    public void FormatCurrency_WithNull_ReturnsEmptyString()
    {
        var result = DataFormatter.FormatCurrency(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatCurrency_WithZero_ReturnsZeroCurrency()
    {
        var result = DataFormatter.FormatCurrency(0m);
        Assert.Contains("0,00", result);
    }

    #endregion

    #region FormatPhoneNumber Tests

    [Fact]
    public void FormatPhoneNumber_WithMobileNumber_FormatsCorrectly()
    {
        var result = DataFormatter.FormatPhoneNumber("0612345678");
        Assert.Equal("06-12345678", result);
    }

    [Fact]
    public void FormatPhoneNumber_WithLandlineNumber_FormatsCorrectly()
    {
        var result = DataFormatter.FormatPhoneNumber("0201234567");
        Assert.Equal("020-1234567", result);
    }

    [Fact]
    public void FormatPhoneNumber_WithNull_ReturnsEmptyString()
    {
        var result = DataFormatter.FormatPhoneNumber(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatPhoneNumber_WithInvalidFormat_ReturnsOriginal()
    {
        var result = DataFormatter.FormatPhoneNumber("+31612345678");
        Assert.Equal("+31612345678", result);
    }

    #endregion

    #region FormatInitials Tests

    [Fact]
    public void FormatInitials_WithSingleName_ReturnsSingleInitial()
    {
        var result = DataFormatter.FormatInitials("Jan");
        Assert.Equal("J.", result);
    }

    [Fact]
    public void FormatInitials_WithMultipleNames_ReturnsMultipleInitials()
    {
        var result = DataFormatter.FormatInitials("Jan Peter");
        Assert.Equal("J.P.", result);
    }

    [Fact]
    public void FormatInitials_WithThreeNames_ReturnsThreeInitials()
    {
        var result = DataFormatter.FormatInitials("Jan Peter Maria");
        Assert.Equal("J.P.M.", result);
    }

    [Fact]
    public void FormatInitials_WithNull_ReturnsEmptyString()
    {
        var result = DataFormatter.FormatInitials(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatInitials_WithEmptyString_ReturnsEmptyString()
    {
        var result = DataFormatter.FormatInitials("");
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region ConvertToString Tests

    [Fact]
    public void ConvertToString_WithNull_ReturnsEmptyString()
    {
        var result = DataFormatter.ConvertToString(null);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConvertToString_WithBoolTrue_ReturnsJa()
    {
        var result = DataFormatter.ConvertToString(true);
        Assert.Equal("Ja", result);
    }

    [Fact]
    public void ConvertToString_WithBoolFalse_ReturnsNee()
    {
        var result = DataFormatter.ConvertToString(false);
        Assert.Equal("Nee", result);
    }

    [Fact]
    public void ConvertToString_WithDateTime_ReturnsFormattedDate()
    {
        var date = new DateTime(2024, 3, 20);
        var result = DataFormatter.ConvertToString(date);
        Assert.Equal("20 maart 2024", result);
    }

    [Fact]
    public void ConvertToString_WithString_ReturnsString()
    {
        var result = DataFormatter.ConvertToString("test");
        Assert.Equal("test", result);
    }

    [Fact]
    public void ConvertToString_WithInteger_ReturnsIntegerString()
    {
        var result = DataFormatter.ConvertToString(42);
        Assert.Equal("42", result);
    }

    #endregion
}

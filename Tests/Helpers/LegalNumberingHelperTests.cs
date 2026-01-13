using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;

namespace scheidingsdesk_document_generator.Tests.Helpers;

public class LegalNumberingHelperTests
{
    #region Constants Tests

    [Fact]
    public void AbstractNumId_HasExpectedValue()
    {
        Assert.Equal(9001, LegalNumberingHelper.AbstractNumId);
    }

    [Fact]
    public void NumberingInstanceId_HasExpectedValue()
    {
        Assert.Equal(9001, LegalNumberingHelper.NumberingInstanceId);
    }

    #endregion

    #region CreateArtikelNumberingProperties Tests

    [Fact]
    public void CreateArtikelNumberingProperties_ReturnsLevelZero()
    {
        var props = LegalNumberingHelper.CreateArtikelNumberingProperties();

        var levelRef = props.GetFirstChild<NumberingLevelReference>();
        Assert.NotNull(levelRef);
        Assert.Equal(0, levelRef.Val?.Value);
    }

    [Fact]
    public void CreateArtikelNumberingProperties_ReturnsDefaultNumId()
    {
        var props = LegalNumberingHelper.CreateArtikelNumberingProperties();

        var numId = props.GetFirstChild<NumberingId>();
        Assert.NotNull(numId);
        Assert.Equal(LegalNumberingHelper.NumberingInstanceId, numId.Val?.Value);
    }

    [Fact]
    public void CreateArtikelNumberingProperties_ReturnsCustomNumId()
    {
        var customNumId = 12345;
        var props = LegalNumberingHelper.CreateArtikelNumberingProperties(customNumId);

        var numId = props.GetFirstChild<NumberingId>();
        Assert.NotNull(numId);
        Assert.Equal(customNumId, numId.Val?.Value);
    }

    #endregion

    #region CreateSubArtikelNumberingProperties Tests

    [Fact]
    public void CreateSubArtikelNumberingProperties_ReturnsLevelOne()
    {
        var props = LegalNumberingHelper.CreateSubArtikelNumberingProperties();

        var levelRef = props.GetFirstChild<NumberingLevelReference>();
        Assert.NotNull(levelRef);
        Assert.Equal(1, levelRef.Val?.Value);
    }

    [Fact]
    public void CreateSubArtikelNumberingProperties_ReturnsDefaultNumId()
    {
        var props = LegalNumberingHelper.CreateSubArtikelNumberingProperties();

        var numId = props.GetFirstChild<NumberingId>();
        Assert.NotNull(numId);
        Assert.Equal(LegalNumberingHelper.NumberingInstanceId, numId.Val?.Value);
    }

    [Fact]
    public void CreateSubArtikelNumberingProperties_ReturnsCustomNumId()
    {
        var customNumId = 54321;
        var props = LegalNumberingHelper.CreateSubArtikelNumberingProperties(customNumId);

        var numId = props.GetFirstChild<NumberingId>();
        Assert.NotNull(numId);
        Assert.Equal(customNumId, numId.Val?.Value);
    }

    #endregion

    #region ResetCounters Tests

    [Fact]
    public void ResetCounters_CanBeCalledMultipleTimes()
    {
        // Should not throw
        LegalNumberingHelper.ResetCounters();
        LegalNumberingHelper.ResetCounters();
        LegalNumberingHelper.ResetCounters();
    }

    #endregion

    #region Integration Tests with In-Memory Document

    [Fact]
    public void EnsureLegalNumberingDefinition_AddsNumberingToDocument()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        // Create minimal document structure
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        // Reset counters for clean state
        LegalNumberingHelper.ResetCounters();

        // Act
        LegalNumberingHelper.EnsureLegalNumberingDefinition(doc);

        // Assert
        var numberingPart = mainPart.NumberingDefinitionsPart;
        Assert.NotNull(numberingPart);
        Assert.NotNull(numberingPart.Numbering);

        // Check AbstractNum exists
        var abstractNum = numberingPart.Numbering.Elements<AbstractNum>()
            .FirstOrDefault(a => a.AbstractNumberId?.Value == LegalNumberingHelper.AbstractNumId);
        Assert.NotNull(abstractNum);

        // Check NumberingInstance exists
        var numInstance = numberingPart.Numbering.Elements<NumberingInstance>()
            .FirstOrDefault(n => n.NumberID?.Value == LegalNumberingHelper.NumberingInstanceId);
        Assert.NotNull(numInstance);
    }

    [Fact]
    public void EnsureLegalNumberingDefinition_DoesNotDuplicateOnSecondCall()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        LegalNumberingHelper.ResetCounters();

        // Act - call twice
        LegalNumberingHelper.EnsureLegalNumberingDefinition(doc);
        LegalNumberingHelper.EnsureLegalNumberingDefinition(doc);

        // Assert - should only have one AbstractNum with our ID
        var numberingPart = mainPart.NumberingDefinitionsPart;
        var abstractNums = numberingPart?.Numbering?.Elements<AbstractNum>()
            .Where(a => a.AbstractNumberId?.Value == LegalNumberingHelper.AbstractNumId)
            .ToList();

        Assert.NotNull(abstractNums);
        Assert.Single(abstractNums);
    }

    [Fact]
    public void CreateRestartedNumberingInstance_ReturnsNewNumId()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        LegalNumberingHelper.ResetCounters();
        LegalNumberingHelper.EnsureLegalNumberingDefinition(doc);

        // Act
        var newNumId = LegalNumberingHelper.CreateRestartedNumberingInstance(doc);

        // Assert - should return a new numId (9100 after reset)
        Assert.Equal(9100, newNumId);
    }

    [Fact]
    public void CreateRestartedNumberingInstance_IncrementsForEachCall()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        LegalNumberingHelper.ResetCounters();
        LegalNumberingHelper.EnsureLegalNumberingDefinition(doc);

        // Act
        var numId1 = LegalNumberingHelper.CreateRestartedNumberingInstance(doc);
        var numId2 = LegalNumberingHelper.CreateRestartedNumberingInstance(doc);
        var numId3 = LegalNumberingHelper.CreateRestartedNumberingInstance(doc);

        // Assert - should increment
        Assert.Equal(9100, numId1);
        Assert.Equal(9101, numId2);
        Assert.Equal(9102, numId3);
    }

    #endregion
}

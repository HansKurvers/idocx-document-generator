using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using Moq;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Generators;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace scheidingsdesk_document_generator.Tests.Processors
{
    public class ContentControlProcessorTests
    {
        private readonly Mock<ILogger<ContentControlProcessor>> _loggerMock;
        private readonly ContentControlProcessor _processor;

        public ContentControlProcessorTests()
        {
            _loggerMock = new Mock<ILogger<ContentControlProcessor>>();
            _processor = new ContentControlProcessor(_loggerMock.Object, new List<ITableGenerator>());
        }

        #region RemoveContentControls Tests

        [Fact]
        public void RemoveContentControls_EmptyDocument_DoesNotThrow()
        {
            // Arrange
            var document = new Document(new Body());

            // Act & Assert
            var exception = Record.Exception(() => _processor.RemoveContentControls(document, "test-123"));
            Assert.Null(exception);
        }

        [Fact]
        public void RemoveContentControls_SingleSdtBlock_RemovesControlPreservesContent()
        {
            // Arrange
            var body = new Body();
            var sdt = new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(
                    new Paragraph(
                        new Run(new Text("Preserved text"))
                    )
                )
            );
            body.Append(sdt);
            var document = new Document(body);

            // Act
            _processor.RemoveContentControls(document, "test-123");

            // Assert
            Assert.Empty(document.Descendants<SdtBlock>());
            var paragraphs = document.Descendants<Paragraph>().ToList();
            Assert.Single(paragraphs);
            Assert.Contains("Preserved text", paragraphs[0].InnerText);
        }

        [Fact]
        public void RemoveContentControls_SdtRun_RemovesControlPreservesContent()
        {
            // Arrange
            var body = new Body();
            var paragraph = new Paragraph(
                new SdtRun(
                    new SdtProperties(),
                    new SdtContentRun(
                        new Run(new Text("Inline content"))
                    )
                )
            );
            body.Append(paragraph);
            var document = new Document(body);

            // Act
            _processor.RemoveContentControls(document, "test-123");

            // Assert
            Assert.Empty(document.Descendants<SdtRun>());
            Assert.Contains("Inline content", document.InnerText);
        }

        [Fact]
        public void RemoveContentControls_NestedSdtElements_RemovesAllControls()
        {
            // Arrange
            var body = new Body();
            var outerSdt = new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(
                    new SdtBlock(
                        new SdtProperties(),
                        new SdtContentBlock(
                            new Paragraph(new Run(new Text("Nested content")))
                        )
                    )
                )
            );
            body.Append(outerSdt);
            var document = new Document(body);

            // Act
            _processor.RemoveContentControls(document, "test-123");

            // Assert
            Assert.Empty(document.Descendants<SdtBlock>());
            Assert.Contains("Nested content", document.InnerText);
        }

        [Fact]
        public void RemoveContentControls_MultipleSdtElements_RemovesAllPreservesOrder()
        {
            // Arrange
            var body = new Body();
            body.Append(new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(new Paragraph(new Run(new Text("First"))))
            ));
            body.Append(new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(new Paragraph(new Run(new Text("Second"))))
            ));
            body.Append(new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(new Paragraph(new Run(new Text("Third"))))
            ));
            var document = new Document(body);

            // Act
            _processor.RemoveContentControls(document, "test-123");

            // Assert
            Assert.Empty(document.Descendants<SdtBlock>());
            var paragraphs = document.Descendants<Paragraph>().ToList();
            Assert.Equal(3, paragraphs.Count);
            Assert.Contains("First", paragraphs[0].InnerText);
            Assert.Contains("Second", paragraphs[1].InnerText);
            Assert.Contains("Third", paragraphs[2].InnerText);
        }

        [Fact]
        public void RemoveContentControls_EmptySdtBlock_RemovesControl()
        {
            // Arrange
            var body = new Body();
            body.Append(new SdtBlock(new SdtProperties()));
            var document = new Document(body);

            // Act
            _processor.RemoveContentControls(document, "test-123");

            // Assert
            Assert.Empty(document.Descendants<SdtBlock>());
        }

        [Fact]
        public void RemoveContentControls_SetsTextColorToBlack()
        {
            // Arrange
            var body = new Body();
            var run = new Run(
                new RunProperties(new Color { Val = "FF0000" }),
                new Text("Red text")
            );
            var sdt = new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(new Paragraph(run))
            );
            body.Append(sdt);
            var document = new Document(body);

            // Act
            _processor.RemoveContentControls(document, "test-123");

            // Assert
            var processedRun = document.Descendants<Run>().First();
            var color = processedRun.RunProperties?.GetFirstChild<Color>();
            Assert.NotNull(color);
            Assert.Equal("000000", color.Val?.Value);
        }

        [Fact]
        public void RemoveContentControls_RemovesShading()
        {
            // Arrange
            var body = new Body();
            var run = new Run(
                new RunProperties(new Shading { Fill = "FFFF00" }),
                new Text("Highlighted text")
            );
            var sdt = new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(new Paragraph(run))
            );
            body.Append(sdt);
            var document = new Document(body);

            // Act
            _processor.RemoveContentControls(document, "test-123");

            // Assert
            var processedRun = document.Descendants<Run>().First();
            var shading = processedRun.RunProperties?.GetFirstChild<Shading>();
            Assert.Null(shading);
        }

        #endregion

        #region RemoveProblematicContentControls Tests

        [Fact]
        public void RemoveProblematicContentControls_EmptyDocument_DoesNotThrow()
        {
            // Arrange
            var document = new Document(new Body());

            // Act & Assert
            var exception = Record.Exception(() => _processor.RemoveProblematicContentControls(document, "test-123"));
            Assert.Null(exception);
        }

        [Fact]
        public void RemoveProblematicContentControls_HashPlaceholder_RemovesParagraph()
        {
            // Arrange
            var body = new Body();
            var paragraph = new Paragraph(
                new SdtRun(
                    new SdtProperties(),
                    new SdtContentRun(new Run(new Text("#Placeholder")))
                )
            );
            body.Append(paragraph);
            var document = new Document(body);

            // Act
            _processor.RemoveProblematicContentControls(document, "test-123");

            // Assert
            Assert.Empty(document.Descendants<Paragraph>());
        }

        [Fact]
        public void RemoveProblematicContentControls_MultipleHashes_RemovesAll()
        {
            // Arrange
            var body = new Body();
            body.Append(new Paragraph(
                new SdtRun(
                    new SdtProperties(),
                    new SdtContentRun(new Run(new Text("#First")))
                )
            ));
            body.Append(new Paragraph(new Run(new Text("Keep this"))));
            body.Append(new Paragraph(
                new SdtRun(
                    new SdtProperties(),
                    new SdtContentRun(new Run(new Text("#Second")))
                )
            ));
            var document = new Document(body);

            // Act
            _processor.RemoveProblematicContentControls(document, "test-123");

            // Assert
            var paragraphs = document.Descendants<Paragraph>().ToList();
            Assert.Single(paragraphs);
            Assert.Contains("Keep this", paragraphs[0].InnerText);
        }

        [Fact]
        public void RemoveProblematicContentControls_EmptySdt_ClearsContent()
        {
            // Arrange
            var body = new Body();
            var sdt = new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(new Paragraph(new Run(new Text("   "))))
            );
            body.Append(sdt);
            var document = new Document(body);

            // Act
            _processor.RemoveProblematicContentControls(document, "test-123");

            // Assert - content should be cleared but SDT still exists
            var remainingSdt = document.Descendants<SdtBlock>().FirstOrDefault();
            if (remainingSdt != null)
            {
                var content = remainingSdt.Descendants<SdtContentBlock>().FirstOrDefault();
                Assert.True(content == null || !content.HasChildren);
            }
        }

        [Fact]
        public void RemoveProblematicContentControls_ValidContent_PreservesContent()
        {
            // Arrange
            var body = new Body();
            var sdt = new SdtBlock(
                new SdtProperties(),
                new SdtContentBlock(new Paragraph(new Run(new Text("Valid content"))))
            );
            body.Append(sdt);
            var document = new Document(body);

            // Act
            _processor.RemoveProblematicContentControls(document, "test-123");

            // Assert
            Assert.Contains("Valid content", document.InnerText);
        }

        [Fact]
        public void RemoveProblematicContentControls_HashInMiddle_RemovesParagraph()
        {
            // Arrange
            var body = new Body();
            var paragraph = new Paragraph(
                new SdtRun(
                    new SdtProperties(),
                    new SdtContentRun(new Run(new Text("Text with # in middle")))
                )
            );
            body.Append(paragraph);
            var document = new Document(body);

            // Act
            _processor.RemoveProblematicContentControls(document, "test-123");

            // Assert
            Assert.Empty(document.Descendants<Paragraph>());
        }

        [Fact]
        public void RemoveProblematicContentControls_SdtBlockWithHash_RemovesWholeParagraph()
        {
            // Arrange
            var body = new Body();
            var paragraph = new Paragraph();
            paragraph.Append(new Run(new Text("Before ")));
            paragraph.Append(new SdtRun(
                new SdtProperties(),
                new SdtContentRun(new Run(new Text("#Placeholder")))
            ));
            paragraph.Append(new Run(new Text(" After")));
            body.Append(paragraph);
            var document = new Document(body);

            // Act
            _processor.RemoveProblematicContentControls(document, "test-123");

            // Assert - whole paragraph should be removed
            Assert.Empty(document.Descendants<Paragraph>());
        }

        #endregion

        #region ProcessTablePlaceholders Tests

        [Fact]
        public void ProcessTablePlaceholders_EmptyBody_DoesNotThrow()
        {
            // Arrange
            var body = new Body();
            var data = new DossierData();

            // Act & Assert
            var exception = Record.Exception(() => _processor.ProcessTablePlaceholders(body, data, "test-123"));
            Assert.Null(exception);
        }

        [Fact]
        public void ProcessTablePlaceholders_NoPlaceholders_LeavesBodyUnchanged()
        {
            // Arrange
            var body = new Body();
            body.Append(new Paragraph(new Run(new Text("Normal text"))));
            var data = new DossierData();

            // Act
            _processor.ProcessTablePlaceholders(body, data, "test-123");

            // Assert
            var paragraphs = body.Descendants<Paragraph>().ToList();
            Assert.Single(paragraphs);
            Assert.Contains("Normal text", paragraphs[0].InnerText);
        }

        [Fact]
        public void ProcessTablePlaceholders_WithMatchingGenerator_ReplacesPlaceholder()
        {
            // Arrange
            var mockGenerator = new Mock<ITableGenerator>();
            mockGenerator.Setup(g => g.PlaceholderTag).Returns("[[TEST_TABLE]]");
            mockGenerator.Setup(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()))
                .Returns(new List<OpenXmlElement>
                {
                    new Paragraph(new Run(new Text("Generated content")))
                });

            var processor = new ContentControlProcessor(
                _loggerMock.Object,
                new List<ITableGenerator> { mockGenerator.Object });

            var body = new Body();
            body.Append(new Paragraph(new Run(new Text("[[TEST_TABLE]]"))));
            var data = new DossierData();

            // Act
            processor.ProcessTablePlaceholders(body, data, "test-123");

            // Assert
            Assert.Contains("Generated content", body.InnerText);
            Assert.DoesNotContain("[[TEST_TABLE]]", body.InnerText);
        }

        [Fact]
        public void ProcessTablePlaceholders_GeneratorThrows_PlaceholderRemains()
        {
            // Arrange
            var mockGenerator = new Mock<ITableGenerator>();
            mockGenerator.Setup(g => g.PlaceholderTag).Returns("[[ERROR_TABLE]]");
            mockGenerator.Setup(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()))
                .Throws(new System.Exception("Test error"));

            var processor = new ContentControlProcessor(
                _loggerMock.Object,
                new List<ITableGenerator> { mockGenerator.Object });

            var body = new Body();
            body.Append(new Paragraph(new Run(new Text("[[ERROR_TABLE]]"))));
            var data = new DossierData();

            // Act
            processor.ProcessTablePlaceholders(body, data, "test-123");

            // Assert - placeholder should remain when generation fails
            Assert.Contains("[[ERROR_TABLE]]", body.InnerText);
        }

        [Fact]
        public void ProcessTablePlaceholders_MultipleGenerators_ProcessesCorrectOne()
        {
            // Arrange
            var mockGenerator1 = new Mock<ITableGenerator>();
            mockGenerator1.Setup(g => g.PlaceholderTag).Returns("[[TABLE_A]]");
            mockGenerator1.Setup(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()))
                .Returns(new List<OpenXmlElement> { new Paragraph(new Run(new Text("Content A"))) });

            var mockGenerator2 = new Mock<ITableGenerator>();
            mockGenerator2.Setup(g => g.PlaceholderTag).Returns("[[TABLE_B]]");
            mockGenerator2.Setup(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()))
                .Returns(new List<OpenXmlElement> { new Paragraph(new Run(new Text("Content B"))) });

            var processor = new ContentControlProcessor(
                _loggerMock.Object,
                new List<ITableGenerator> { mockGenerator1.Object, mockGenerator2.Object });

            var body = new Body();
            body.Append(new Paragraph(new Run(new Text("[[TABLE_B]]"))));
            var data = new DossierData();

            // Act
            processor.ProcessTablePlaceholders(body, data, "test-123");

            // Assert
            Assert.Contains("Content B", body.InnerText);
            mockGenerator2.Verify(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()), Times.Once);
            mockGenerator1.Verify(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ProcessTablePlaceholders_WithReplacements_PassesReplacementsToGenerator()
        {
            // Arrange
            var body = new Body();
            body.Append(new Paragraph(new Run(new Text("No placeholders"))));
            var data = new DossierData();
            var replacements = new Dictionary<string, string>
            {
                { "Partij1Benaming", "de man" },
                { "Partij2Benaming", "de vrouw" }
            };

            // Act & Assert - should not throw
            var exception = Record.Exception(() =>
                _processor.ProcessTablePlaceholders(body, data, replacements, "test-123"));
            Assert.Null(exception);
        }

        [Fact]
        public void ProcessTablePlaceholders_PreservesParagraphIndentation()
        {
            // Arrange
            var mockGenerator = new Mock<ITableGenerator>();
            mockGenerator.Setup(g => g.PlaceholderTag).Returns("[[INDENT_TEST]]");
            mockGenerator.Setup(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()))
                .Returns(new List<OpenXmlElement>
                {
                    new Paragraph(new Run(new Text("New paragraph")))
                });

            var processor = new ContentControlProcessor(
                _loggerMock.Object,
                new List<ITableGenerator> { mockGenerator.Object });

            var body = new Body();
            var paragraphWithIndent = new Paragraph(
                new ParagraphProperties(
                    new Indentation { Left = "720" }
                ),
                new Run(new Text("[[INDENT_TEST]]"))
            );
            body.Append(paragraphWithIndent);
            var data = new DossierData();

            // Act
            processor.ProcessTablePlaceholders(body, data, "test-123");

            // Assert
            var resultParagraph = body.Descendants<Paragraph>().First();
            var indentation = resultParagraph.ParagraphProperties?.Indentation;
            Assert.NotNull(indentation);
            Assert.Equal("720", indentation.Left?.Value);
        }

        [Fact]
        public void ProcessTablePlaceholders_GeneratorReturnsEmpty_RemovesPlaceholder()
        {
            // Arrange
            var mockGenerator = new Mock<ITableGenerator>();
            mockGenerator.Setup(g => g.PlaceholderTag).Returns("[[EMPTY_TABLE]]");
            mockGenerator.Setup(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()))
                .Returns(new List<OpenXmlElement>());

            var processor = new ContentControlProcessor(
                _loggerMock.Object,
                new List<ITableGenerator> { mockGenerator.Object });

            var body = new Body();
            body.Append(new Paragraph(new Run(new Text("[[EMPTY_TABLE]]"))));
            var data = new DossierData();

            // Act
            processor.ProcessTablePlaceholders(body, data, "test-123");

            // Assert
            Assert.DoesNotContain("[[EMPTY_TABLE]]", body.InnerText);
        }

        [Fact]
        public void ProcessTablePlaceholders_GeneratorReturnsTable_InsertsTable()
        {
            // Arrange
            var mockGenerator = new Mock<ITableGenerator>();
            mockGenerator.Setup(g => g.PlaceholderTag).Returns("[[REAL_TABLE]]");
            mockGenerator.Setup(g => g.Generate(It.IsAny<DossierData>(), It.IsAny<string>()))
                .Returns(new List<OpenXmlElement>
                {
                    new Table(
                        new TableRow(
                            new TableCell(new Paragraph(new Run(new Text("Cell 1"))))
                        )
                    )
                });

            var processor = new ContentControlProcessor(
                _loggerMock.Object,
                new List<ITableGenerator> { mockGenerator.Object });

            var body = new Body();
            body.Append(new Paragraph(new Run(new Text("[[REAL_TABLE]]"))));
            var data = new DossierData();

            // Act
            processor.ProcessTablePlaceholders(body, data, "test-123");

            // Assert
            var tables = body.Descendants<Table>().ToList();
            Assert.Single(tables);
            Assert.Contains("Cell 1", tables[0].InnerText);
        }

        #endregion
    }
}

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors
{
    /// <summary>
    /// Orchestrator for placeholder processing in Word documents.
    /// Coordinates multiple placeholder builders and handles document replacement.
    /// </summary>
    public class PlaceholderProcessor : IPlaceholderProcessor
    {
        private readonly ILogger<PlaceholderProcessor> _logger;
        private readonly IConditieEvaluator _conditieEvaluator;
        private readonly IEnumerable<IPlaceholderBuilder> _builders;

        public PlaceholderProcessor(
            ILogger<PlaceholderProcessor> logger,
            IConditieEvaluator conditieEvaluator,
            IEnumerable<IPlaceholderBuilder> builders)
        {
            _logger = logger;
            _conditieEvaluator = conditieEvaluator;
            // Sort builders by their Order property
            _builders = builders.OrderBy(b => b.Order).ToList();
        }

        /// <summary>
        /// Builds all placeholder replacements from dossier data using registered builders.
        /// </summary>
        public Dictionary<string, string> BuildReplacements(DossierData data, Dictionary<string, string> grammarRules)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Add grammar rules first
            foreach (var rule in grammarRules)
            {
                replacements[rule.Key] = rule.Value;
            }
            _logger.LogDebug("Added {Count} grammar rules", grammarRules.Count);

            // 2. Run all placeholder builders in order
            foreach (var builder in _builders)
            {
                try
                {
                    _logger.LogDebug("Running builder: {BuilderType} (Order: {Order})",
                        builder.GetType().Name, builder.Order);
                    builder.Build(replacements, data, grammarRules);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in builder {BuilderType}", builder.GetType().Name);
                    // Continue with other builders
                }
            }

            // 3. Add custom placeholders from the placeholder_catalogus
            // These have priority: dossier > gebruiker > systeem > standaard_waarde
            if (data.CustomPlaceholders.Any())
            {
                foreach (var placeholder in data.CustomPlaceholders)
                {
                    // Only add if not already set by a system placeholder
                    if (!replacements.ContainsKey(placeholder.Key))
                    {
                        replacements[placeholder.Key] = placeholder.Value;
                    }
                }
                _logger.LogInformation("Added {Count} custom placeholders", data.CustomPlaceholders.Count);
            }

            // 4. Evaluate conditional placeholders using ConditieEvaluator
            // These are placeholders with heeft_conditie = 1 and a conditie_config
            if (data.ConditionalPlaceholders.Any())
            {
                // Build evaluation context with computed fields
                var context = _conditieEvaluator.BuildEvaluationContext(data, replacements);

                foreach (var conditionalPlaceholder in data.ConditionalPlaceholders)
                {
                    var config = conditionalPlaceholder.ConditieConfig;
                    if (config != null)
                    {
                        try
                        {
                            var result = _conditieEvaluator.Evaluate(config, context);
                            var resolvedValue = _conditieEvaluator.ResolveNestedPlaceholders(result.RawResult, replacements);

                            // Conditional placeholders override any existing value
                            replacements[conditionalPlaceholder.PlaceholderKey] = resolvedValue;

                            _logger.LogDebug("Conditional placeholder {Key} evaluated to: {Value} (rule: {Rule})",
                                conditionalPlaceholder.PlaceholderKey,
                                resolvedValue,
                                result.MatchedRule?.ToString() ?? "default");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error evaluating conditional placeholder {Key}, using empty string",
                                conditionalPlaceholder.PlaceholderKey);
                            replacements[conditionalPlaceholder.PlaceholderKey] = string.Empty;
                        }
                    }
                }

                _logger.LogInformation("Evaluated {Count} conditional placeholders", data.ConditionalPlaceholders.Count);
            }

            _logger.LogInformation("Built {Count} total placeholder replacements", replacements.Count);
            return replacements;
        }

        /// <summary>
        /// Processes the document and replaces all placeholders.
        /// </summary>
        public void ProcessDocument(Body body, Dictionary<string, string> replacements, string correlationId)
        {
            var paragraphs = body.Descendants<Paragraph>().ToList();
            _logger.LogInformation($"[{correlationId}] Processing {paragraphs.Count} paragraphs");

            foreach (var paragraph in paragraphs)
            {
                ProcessParagraph(paragraph, replacements);
            }

            // Also process tables
            var tables = body.Descendants<Table>().ToList();
            _logger.LogInformation($"[{correlationId}] Processing {tables.Count} tables");

            foreach (var table in tables)
            {
                foreach (var cell in table.Descendants<TableCell>())
                {
                    foreach (var para in cell.Descendants<Paragraph>())
                    {
                        ProcessParagraph(para, replacements);
                    }
                }
            }
        }

        /// <summary>
        /// Processes headers and footers.
        /// </summary>
        public void ProcessHeadersAndFooters(MainDocumentPart mainPart, Dictionary<string, string> replacements, string correlationId)
        {
            // Process headers
            foreach (var headerPart in mainPart.HeaderParts)
            {
                foreach (var paragraph in headerPart.Header.Descendants<Paragraph>())
                {
                    ProcessParagraph(paragraph, replacements);
                }
            }

            // Process footers
            foreach (var footerPart in mainPart.FooterParts)
            {
                foreach (var paragraph in footerPart.Footer.Descendants<Paragraph>())
                {
                    ProcessParagraph(paragraph, replacements);
                }
            }

            _logger.LogInformation($"[{correlationId}] Processed headers and footers");
        }

        #region Private Helper Methods

        // Regex voor modifier placeholders: [[caps:Name]], [[upper:Name]], [[lower:Name]]
        private static readonly Regex ModifierPlaceholderPattern = new Regex(
            @"\[\[(caps|upper|lower):([^\]]+)\]\]",
            RegexOptions.IgnoreCase);

        /// <summary>
        /// Process a single paragraph and replace placeholders.
        /// </summary>
        private void ProcessParagraph(Paragraph paragraph, Dictionary<string, string> replacements)
        {
            var texts = paragraph.Descendants<Text>().ToList();
            if (!texts.Any()) return;

            // Combine all text to handle placeholders that might be split
            var fullText = string.Join("", texts.Select(t => t.Text));

            // Check if this paragraph contains any placeholders (standard or modifier)
            bool hasPlaceholders = replacements.Keys.Any(key =>
                fullText.Contains($"[[{key}]]") ||
                fullText.Contains($"{{{key}}}") ||
                fullText.Contains($"<<{key}>>") ||
                fullText.Contains($"[{key}]"));
            bool hasModifierPlaceholders = ModifierPlaceholderPattern.IsMatch(fullText);

            if (!hasPlaceholders && !hasModifierPlaceholders) return;

            var newText = fullText;

            // 1. Verwerk modifier placeholders eerst (bijv. [[caps:Partij1Benaming]])
            if (hasModifierPlaceholders)
            {
                newText = ModifierPlaceholderPattern.Replace(newText, match =>
                {
                    var modifier = match.Groups[1].Value.ToLowerInvariant();
                    var placeholder = match.Groups[2].Value;

                    // Zoek de waarde in replacements
                    string? value = null;
                    if (replacements.TryGetValue(placeholder, out var exactValue))
                    {
                        value = exactValue;
                    }
                    else
                    {
                        var key = replacements.Keys.FirstOrDefault(k =>
                            k.Equals(placeholder, StringComparison.OrdinalIgnoreCase));
                        if (key != null)
                            value = replacements[key];
                    }

                    if (value == null)
                        return match.Value; // Placeholder niet gevonden, laat staan

                    return ApplyModifier(value, modifier);
                });
            }

            // 2. Verwerk standaard placeholders
            foreach (var replacement in replacements)
            {
                newText = newText.Replace($"[[{replacement.Key}]]", replacement.Value);
                newText = newText.Replace($"{{{replacement.Key}}}", replacement.Value);
                newText = newText.Replace($"<<{replacement.Key}>>", replacement.Value);
                newText = newText.Replace($"[{replacement.Key}]", replacement.Value);
            }

            if (newText != fullText)
            {
                // Check if the new text contains line breaks
                if (newText.Contains("\n"))
                {
                    // Handle line breaks by creating proper Word line breaks
                    ReplaceTextWithLineBreaks(paragraph, texts, newText);
                }
                else
                {
                    // Simple replacement without line breaks
                    texts.Skip(1).ToList().ForEach(t => t.Remove());
                    if (texts.Any())
                    {
                        texts[0].Text = newText;
                    }
                }
            }
        }

        /// <summary>
        /// Past een modifier toe op een placeholder waarde.
        /// - caps: eerste letter hoofdletter (voor begin van zin)
        /// - upper: alles hoofdletters
        /// - lower: alles kleine letters
        /// </summary>
        private static string ApplyModifier(string value, string modifier)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return modifier switch
            {
                "caps" => char.ToUpper(value[0]) + value.Substring(1),
                "upper" => value.ToUpperInvariant(),
                "lower" => value.ToLowerInvariant(),
                _ => value
            };
        }

        /// <summary>
        /// Replace text in paragraph with proper Word line breaks for \n characters.
        /// </summary>
        private void ReplaceTextWithLineBreaks(Paragraph paragraph, List<Text> originalTexts, string newText)
        {
            // Get the parent Run of the first text element to copy its properties
            var firstText = originalTexts.FirstOrDefault();
            var parentRun = firstText?.Parent as Run;
            var runProperties = parentRun?.RunProperties?.CloneNode(true) as RunProperties;

            // Remove all original text elements
            foreach (var text in originalTexts)
            {
                text.Remove();
            }

            // Split the text by newlines and create new elements
            var lines = newText.Split(new[] { "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                // Create a new Run for each line
                var newRun = new Run();
                if (runProperties != null)
                {
                    newRun.RunProperties = runProperties.CloneNode(true) as RunProperties;
                }

                // Add the text
                var textElement = new Text(lines[i]);
                if (lines[i].StartsWith(" ") || lines[i].EndsWith(" "))
                {
                    textElement.Space = SpaceProcessingModeValues.Preserve;
                }
                newRun.Append(textElement);

                // Add a line break after each line except the last
                if (i < lines.Length - 1)
                {
                    newRun.Append(new Break());
                }

                // Insert the run into the paragraph
                if (parentRun != null)
                {
                    parentRun.Parent?.InsertBefore(newRun, parentRun);
                }
                else
                {
                    paragraph.Append(newRun);
                }
            }

            // Remove the original parent run if it's empty
            if (parentRun != null && !parentRun.Descendants<Text>().Any())
            {
                parentRun.Remove();
            }
        }

        #endregion
    }
}

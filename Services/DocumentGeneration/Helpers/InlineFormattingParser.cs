using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    public class FormattedSegment
    {
        public string Text { get; set; } = string.Empty;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUnderline { get; set; }
    }

    public static class InlineFormattingParser
    {
        // First pass: match **bold** and __underline__
        private static readonly Regex BoldUnderlinePattern = new(
            @"(\*\*(?:(?!\*\*).)+?\*\*|__(?:(?!__).)+?__)",
            RegexOptions.Compiled);

        // Second pass: match *italic* in remaining plain text
        private static readonly Regex ItalicPattern = new(
            @"\*([^*]+?)\*",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses text containing **bold**, *italic* and __underline__ markers into formatted segments.
        /// Two-pass strategy: first bold/underline, then italic in remaining plain text.
        /// </summary>
        public static List<FormattedSegment> Parse(string text)
        {
            var segments = new List<FormattedSegment>();

            if (string.IsNullOrEmpty(text))
            {
                return segments;
            }

            // First pass: extract bold and underline, collect plain text segments
            int lastIndex = 0;
            var firstPassMatches = BoldUnderlinePattern.Matches(text);

            foreach (Match match in firstPassMatches)
            {
                // Plain text before this match - will be processed for italic
                if (match.Index > lastIndex)
                {
                    var plainText = text.Substring(lastIndex, match.Index - lastIndex);
                    segments.AddRange(ParseItalic(plainText));
                }

                var matchedText = match.Value;

                if (matchedText.StartsWith("**") && matchedText.EndsWith("**"))
                {
                    segments.Add(new FormattedSegment
                    {
                        Text = matchedText.Substring(2, matchedText.Length - 4),
                        IsBold = true,
                        IsItalic = false,
                        IsUnderline = false
                    });
                }
                else if (matchedText.StartsWith("__") && matchedText.EndsWith("__"))
                {
                    segments.Add(new FormattedSegment
                    {
                        Text = matchedText.Substring(2, matchedText.Length - 4),
                        IsBold = false,
                        IsItalic = false,
                        IsUnderline = true
                    });
                }

                lastIndex = match.Index + match.Length;
            }

            // Remaining plain text after last bold/underline match
            if (lastIndex < text.Length)
            {
                var remainingText = text.Substring(lastIndex);
                segments.AddRange(ParseItalic(remainingText));
            }

            // If no matches were found at all, parse the whole text for italic
            if (segments.Count == 0)
            {
                segments.AddRange(ParseItalic(text));
            }

            // If still empty (no formatting at all), return as single plain segment
            if (segments.Count == 0)
            {
                segments.Add(new FormattedSegment
                {
                    Text = text,
                    IsBold = false,
                    IsItalic = false,
                    IsUnderline = false
                });
            }

            return segments;
        }

        /// <summary>
        /// Parses *italic* markers in plain text segments.
        /// </summary>
        private static List<FormattedSegment> ParseItalic(string text)
        {
            var segments = new List<FormattedSegment>();

            if (string.IsNullOrEmpty(text))
            {
                return segments;
            }

            int lastIndex = 0;
            var matches = ItalicPattern.Matches(text);

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    segments.Add(new FormattedSegment
                    {
                        Text = text.Substring(lastIndex, match.Index - lastIndex),
                        IsBold = false,
                        IsItalic = false,
                        IsUnderline = false
                    });
                }

                segments.Add(new FormattedSegment
                {
                    Text = match.Groups[1].Value,
                    IsBold = false,
                    IsItalic = true,
                    IsUnderline = false
                });

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                segments.Add(new FormattedSegment
                {
                    Text = text.Substring(lastIndex),
                    IsBold = false,
                    IsItalic = false,
                    IsUnderline = false
                });
            }

            return segments;
        }
    }
}

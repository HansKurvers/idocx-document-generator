using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers
{
    public class FormattedSegment
    {
        public string Text { get; set; } = string.Empty;
        public bool IsBold { get; set; }
        public bool IsUnderline { get; set; }
    }

    public static class InlineFormattingParser
    {
        private static readonly Regex InlinePattern = new(
            @"(\*\*(?:(?!\*\*).)+?\*\*|__(?:(?!__).)+?__)",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses text containing **bold** and __underline__ markers into formatted segments.
        /// </summary>
        public static List<FormattedSegment> Parse(string text)
        {
            var segments = new List<FormattedSegment>();

            if (string.IsNullOrEmpty(text))
            {
                return segments;
            }

            int lastIndex = 0;
            var matches = InlinePattern.Matches(text);

            foreach (Match match in matches)
            {
                // Add plain text before this match
                if (match.Index > lastIndex)
                {
                    segments.Add(new FormattedSegment
                    {
                        Text = text.Substring(lastIndex, match.Index - lastIndex),
                        IsBold = false,
                        IsUnderline = false
                    });
                }

                var matchedText = match.Value;

                if (matchedText.StartsWith("**") && matchedText.EndsWith("**"))
                {
                    segments.Add(new FormattedSegment
                    {
                        Text = matchedText.Substring(2, matchedText.Length - 4),
                        IsBold = true,
                        IsUnderline = false
                    });
                }
                else if (matchedText.StartsWith("__") && matchedText.EndsWith("__"))
                {
                    segments.Add(new FormattedSegment
                    {
                        Text = matchedText.Substring(2, matchedText.Length - 4),
                        IsBold = false,
                        IsUnderline = true
                    });
                }

                lastIndex = match.Index + match.Length;
            }

            // Add remaining plain text after last match
            if (lastIndex < text.Length)
            {
                segments.Add(new FormattedSegment
                {
                    Text = text.Substring(lastIndex),
                    IsBold = false,
                    IsUnderline = false
                });
            }

            // If no matches were found, return the whole text as a single plain segment
            if (segments.Count == 0)
            {
                segments.Add(new FormattedSegment
                {
                    Text = text,
                    IsBold = false,
                    IsUnderline = false
                });
            }

            return segments;
        }
    }
}

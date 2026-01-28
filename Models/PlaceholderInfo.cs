namespace scheidingsdesk_document_generator.Models
{
    /// <summary>
    /// Model for placeholder metadata returned by the placeholder catalog API
    /// </summary>
    public class PlaceholderInfo
    {
        /// <summary>
        /// The placeholder name (without brackets), e.g., "Partij1Naam"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the placeholder does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category/section the placeholder belongs to, e.g., "Partij 1 Informatie"
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The format/syntax used for this placeholder, e.g., "[[ ]]", "{ }"
        /// </summary>
        public string Format { get; set; } = "[[ ]]";
    }
}

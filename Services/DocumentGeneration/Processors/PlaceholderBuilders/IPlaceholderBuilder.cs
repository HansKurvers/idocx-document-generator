using scheidingsdesk_document_generator.Models;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Interface voor placeholder builders die een specifieke categorie van placeholders opbouwen.
    /// Elk builder is verantwoordelijk voor een logische groep placeholders.
    /// </summary>
    public interface IPlaceholderBuilder
    {
        /// <summary>
        /// De volgorde waarin deze builder wordt uitgevoerd.
        /// Lagere nummers worden eerst uitgevoerd.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Voegt placeholders toe aan de replacements dictionary.
        /// </summary>
        /// <param name="replacements">Dictionary waar placeholders aan toegevoegd worden</param>
        /// <param name="data">Dossier data met alle informatie</param>
        /// <param name="grammarRules">Grammatica regels (enkelvoud/meervoud)</param>
        void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules
        );
    }
}

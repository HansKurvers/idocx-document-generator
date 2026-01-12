using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;
using scheidingsdesk_document_generator.Services.DocumentGeneration.Helpers;
using System;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Services.DocumentGeneration.Processors.PlaceholderBuilders
{
    /// <summary>
    /// Builder voor basis dossier placeholders.
    /// Verantwoordelijk voor: DossierNummer, DossierDatum, HuidigeDatum, IsAnoniem
    /// </summary>
    public class DossierPlaceholderBuilder : BasePlaceholderBuilder
    {
        public override int Order => 10;

        public DossierPlaceholderBuilder(ILogger<DossierPlaceholderBuilder> logger)
            : base(logger)
        {
        }

        public override void Build(
            Dictionary<string, string> replacements,
            DossierData data,
            Dictionary<string, string> grammarRules)
        {
            _logger.LogDebug("Building dossier placeholders for dossier {DossierId}", data.Id);

            AddPlaceholder(replacements, "DossierNummer", data.DossierNummer);
            AddPlaceholder(replacements, "DossierDatum", DataFormatter.FormatDate(data.AangemaaktOp));
            AddPlaceholder(replacements, "HuidigeDatum", DataFormatter.FormatDateDutchLong(DateTime.Now));
            AddPlaceholder(replacements, "IsAnoniem", DataFormatter.ConvertToString(data.IsAnoniem));

            _logger.LogDebug("Added dossier placeholders: DossierNummer={Nr}, IsAnoniem={Anoniem}",
                data.DossierNummer, data.IsAnoniem);
        }
    }
}

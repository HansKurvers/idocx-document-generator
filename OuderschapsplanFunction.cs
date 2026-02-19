using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using scheidingsdesk_document_generator.Services.DocumentGeneration;
using System;
using System.Threading.Tasks;

namespace Scheidingsdesk
{
    /// <summary>
    /// Ouderschapsplan Function - Clean endpoint using modular service architecture
    /// Inherits shared request parsing and error handling from BaseDocumentFunction
    /// </summary>
    public class OuderschapsplanFunction : BaseDocumentFunction<OuderschapsplanFunction>
    {
        private readonly IDocumentGenerationService _documentGenerationService;

        public OuderschapsplanFunction(
            ILogger<OuderschapsplanFunction> logger,
            IDocumentGenerationService documentGenerationService)
            : base(logger)
        {
            _documentGenerationService = documentGenerationService;
        }

        [Function("Ouderschapsplan")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ouderschapsplan")] HttpRequest req)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation($"[{correlationId}] Ouderschapsplan generation request started");

            try
            {
                // Parse and validate request
                var request = await ParseRequestAsync<OuderschapsplanRequest>(req, correlationId);
                if (request == null)
                {
                    return CreateBadRequest("Invalid request body. Please provide a JSON object with DossierId.", correlationId);
                }

                // Get template type from request, default to 'default' if not specified
                string? templateType = request.TemplateType;
                if (string.IsNullOrWhiteSpace(templateType))
                {
                    templateType = "default";
                    _logger.LogInformation($"[{correlationId}] No template type specified, using default");
                }

                _logger.LogInformation($"[{correlationId}] Generating document for DossierId: {request.DossierId} with template type: {templateType}");

                // Generate document - ALL logic delegated to DocumentGenerationService
                var documentStream = await _documentGenerationService.GenerateDocumentAsync(
                    request.DossierId,
                    templateType,
                    correlationId
                );

                stopwatch.Stop();
                _logger.LogInformation($"[{correlationId}] Document generated successfully in {stopwatch.ElapsedMilliseconds}ms");

                // Return file result with proper naming
                var fileName = $"Ouderschapsplan_Dossier_{request.DossierId}_{DateTime.Now:yyyyMMdd}.docx";
                return new FileStreamResult(documentStream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = fileName
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Invalid operation: {ex.Message}");
                return CreateErrorResponse(ex.Message, correlationId, 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{correlationId}] Unexpected error: {ex.Message}");
                return CreateErrorResponse("An unexpected error occurred during document generation.", correlationId, 500);
            }
        }
    }

    /// <summary>
    /// Request model for Ouderschapsplan generation
    /// </summary>
    public class OuderschapsplanRequest : IDocumentRequest
    {
        public int DossierId { get; set; }
        public string? TemplateType { get; set; }
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using scheidingsdesk_document_generator.Services.DocumentGeneration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Scheidingsdesk
{
    /// <summary>
    /// Convenant Function - Endpoint for generating echtscheidingsconvenant documents
    /// Follows the same architecture as OuderschapsplanFunction using modular service architecture
    /// </summary>
    public class ConvenantFunction
    {
        private readonly ILogger<ConvenantFunction> _logger;
        private readonly IDocumentGenerationService _documentGenerationService;

        public ConvenantFunction(
            ILogger<ConvenantFunction> logger,
            IDocumentGenerationService documentGenerationService)
        {
            _logger = logger;
            _documentGenerationService = documentGenerationService;
        }

        [Function("Convenant")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "convenant")] HttpRequest req)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();

            _logger.LogInformation($"[{correlationId}] Convenant generation request started");

            try
            {
                // Parse and validate request
                var request = await ParseRequestAsync(req, correlationId);
                if (request == null)
                {
                    return CreateBadRequest("Invalid request body. Please provide a JSON object with DossierId.", correlationId);
                }

                // Template type is always 'convenant' for this endpoint
                string templateType = "convenant";

                _logger.LogInformation($"[{correlationId}] Generating convenant document for DossierId: {request.DossierId}");

                // Generate document - ALL logic delegated to DocumentGenerationService
                var documentStream = await _documentGenerationService.GenerateDocumentAsync(
                    request.DossierId,
                    templateType,
                    correlationId
                );

                stopwatch.Stop();
                _logger.LogInformation($"[{correlationId}] Convenant document generated successfully in {stopwatch.ElapsedMilliseconds}ms");

                // Return file result with proper naming
                var fileName = $"Convenant_Dossier_{request.DossierId}_{DateTime.Now:yyyyMMdd}.docx";
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

        private async Task<ConvenantRequest?> ParseRequestAsync(HttpRequest req, string correlationId)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning($"[{correlationId}] Empty request body");
                    return null;
                }

                var request = JsonConvert.DeserializeObject<ConvenantRequest>(requestBody);

                if (request?.DossierId == null || request.DossierId <= 0)
                {
                    _logger.LogWarning($"[{correlationId}] Invalid DossierId: {request?.DossierId}");
                    return null;
                }

                return request;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"[{correlationId}] JSON parsing error");
                return null;
            }
        }

        private IActionResult CreateBadRequest(string message, string correlationId)
        {
            return new BadRequestObjectResult(new
            {
                error = message,
                correlationId = correlationId
            });
        }

        private IActionResult CreateErrorResponse(string message, string correlationId, int statusCode)
        {
            return new ObjectResult(new
            {
                error = message,
                correlationId = correlationId
            })
            {
                StatusCode = statusCode
            };
        }
    }

    /// <summary>
    /// Request model for Convenant generation
    /// </summary>
    public class ConvenantRequest
    {
        public int DossierId { get; set; }
    }
}

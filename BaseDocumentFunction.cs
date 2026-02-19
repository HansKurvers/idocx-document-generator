using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Scheidingsdesk
{
    /// <summary>
    /// Base request interface for document generation endpoints.
    /// All document requests must include a DossierId.
    /// </summary>
    public interface IDocumentRequest
    {
        int DossierId { get; set; }
    }

    /// <summary>
    /// Base class for document generation Azure Functions.
    /// Provides shared request parsing, error response creation, and logging.
    /// </summary>
    public abstract class BaseDocumentFunction<TLogger> where TLogger : class
    {
        protected readonly ILogger<TLogger> _logger;

        protected BaseDocumentFunction(ILogger<TLogger> logger)
        {
            _logger = logger;
        }

        protected async Task<T?> ParseRequestAsync<T>(HttpRequest req, string correlationId) where T : class, IDocumentRequest
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning($"[{correlationId}] Empty request body");
                    return null;
                }

                var request = JsonConvert.DeserializeObject<T>(requestBody);

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

        protected IActionResult CreateBadRequest(string message, string correlationId)
        {
            return new BadRequestObjectResult(new
            {
                error = message,
                correlationId = correlationId
            });
        }

        protected IActionResult CreateErrorResponse(string message, string correlationId, int statusCode)
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
}

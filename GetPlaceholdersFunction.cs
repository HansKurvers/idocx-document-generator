using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Services;

namespace Scheidingsdesk
{
    /// <summary>
    /// Function to retrieve available placeholders from the catalog
    /// </summary>
    public class GetPlaceholdersFunction
    {
        private readonly ILogger<GetPlaceholdersFunction> _logger;
        private readonly IPlaceholderCatalogService _placeholderCatalogService;

        public GetPlaceholdersFunction(
            ILogger<GetPlaceholdersFunction> logger,
            IPlaceholderCatalogService placeholderCatalogService)
        {
            _logger = logger;
            _placeholderCatalogService = placeholderCatalogService;
        }

        [Function("GetPlaceholders")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "placeholders")] HttpRequest req)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation("[{CorrelationId}] Get placeholders request started", correlationId);

            try
            {
                // Check for optional category filter
                var categoryFilter = req.Query["category"].ToString();

                var placeholders = string.IsNullOrEmpty(categoryFilter)
                    ? await _placeholderCatalogService.GetAllPlaceholdersAsync()
                    : await _placeholderCatalogService.GetPlaceholdersByCategoryAsync(categoryFilter);

                var categories = await _placeholderCatalogService.GetCategoriesAsync();

                _logger.LogInformation(
                    "[{CorrelationId}] Successfully retrieved {Count} placeholders{Filter}",
                    correlationId,
                    placeholders.Count,
                    string.IsNullOrEmpty(categoryFilter) ? "" : $" (filtered by: {categoryFilter})");

                return new OkObjectResult(new
                {
                    placeholders = placeholders,
                    count = placeholders.Count,
                    categories = categories,
                    correlationId = correlationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] Error retrieving placeholders", correlationId);
                return new ObjectResult(new
                {
                    error = "An error occurred while retrieving placeholders",
                    correlationId = correlationId
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}

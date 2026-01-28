using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using scheidingsdesk_document_generator.Models;

namespace scheidingsdesk_document_generator.Services
{
    /// <summary>
    /// Interface for the placeholder catalog service
    /// </summary>
    public interface IPlaceholderCatalogService
    {
        /// <summary>
        /// Gets all placeholders from the catalog
        /// </summary>
        Task<IReadOnlyList<PlaceholderInfo>> GetAllPlaceholdersAsync();

        /// <summary>
        /// Gets placeholders filtered by category
        /// </summary>
        Task<IReadOnlyList<PlaceholderInfo>> GetPlaceholdersByCategoryAsync(string category);

        /// <summary>
        /// Gets all available categories
        /// </summary>
        Task<IReadOnlyList<string>> GetCategoriesAsync();
    }

    /// <summary>
    /// Service that parses placeholders.md to provide a catalog of available placeholders
    /// </summary>
    public class PlaceholderCatalogService : IPlaceholderCatalogService
    {
        private readonly ILogger<PlaceholderCatalogService> _logger;
        private readonly string _placeholdersFilePath;

        private IReadOnlyList<PlaceholderInfo>? _cachedPlaceholders;
        private IReadOnlyList<string>? _cachedCategories;
        private DateTime _cacheTimestamp;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        // Regex patterns for parsing placeholders.md
        // Matches [[PlaceholderName]] - Description
        private static readonly Regex DoubleSquareBracketPattern = new(
            @"^\s*-\s*\[\[([^\]]+)\]\]\s*-\s*(.+)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        // Matches {PlaceholderName} - Description
        private static readonly Regex CurlyBracePattern = new(
            @"^\s*-\s*\{([^}]+)\}\s*-\s*(.+)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        // Matches category headers (lines ending with ':' that are not bullet points)
        // Must start at the beginning of the line (no indentation)
        private static readonly Regex CategoryHeaderPattern = new(
            @"^([A-Za-z0-9À-ÿ\s&\(\)]+):\s*$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        // Keywords that indicate a valid category header
        private static readonly HashSet<string> CategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "informatie", "placeholders", "afspraken", "gegevens", "regeling",
            "alimentatie", "financieel", "administratief", "kinderen", "partij",
            "dossier", "templates", "convenant", "woning", "kadastraal",
            "ondertekening", "considerans", "partneralimentatie", "juridisch",
            "relatie", "zorg", "verblijf", "communicatie", "ouderschapsplan",
            "overig", "aliassen", "grammatica"
        };

        // Patterns to exclude from being categories
        private static readonly HashSet<string> ExcludedCategoryPatterns = new(StringComparer.OrdinalIgnoreCase)
        {
            "voorbeeld", "let op", "opmerking", "technische", "scenario",
            "rekening", "jan", "piet", "maria", "lisa", "emma", "ondergetekenden",
            "omgangsregeling", "zorgafspraken", "financiële afspraken", "social media",
            "devices", "bankrekeningen", "verzekeringen", "evaluatie", "kenmerken",
            "genereert", "ibans", "je kunt", "bij", "als"
        };

        public PlaceholderCatalogService(ILogger<PlaceholderCatalogService> logger)
        {
            _logger = logger;

            // Determine the path to placeholders.md
            // In Azure Functions, the working directory is the function app root
            var basePath = AppContext.BaseDirectory;
            _placeholdersFilePath = Path.Combine(basePath, "placeholders.md");

            // Fallback for local development
            if (!File.Exists(_placeholdersFilePath))
            {
                _placeholdersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "placeholders.md");
            }
        }

        public async Task<IReadOnlyList<PlaceholderInfo>> GetAllPlaceholdersAsync()
        {
            await EnsureCacheLoadedAsync();
            return _cachedPlaceholders!;
        }

        public async Task<IReadOnlyList<PlaceholderInfo>> GetPlaceholdersByCategoryAsync(string category)
        {
            await EnsureCacheLoadedAsync();

            return _cachedPlaceholders!
                .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<IReadOnlyList<string>> GetCategoriesAsync()
        {
            await EnsureCacheLoadedAsync();
            return _cachedCategories!;
        }

        private async Task EnsureCacheLoadedAsync()
        {
            // Check if cache is still valid
            if (_cachedPlaceholders != null &&
                _cachedCategories != null &&
                DateTime.UtcNow - _cacheTimestamp < _cacheDuration)
            {
                return;
            }

            await LoadAndParsePlaceholdersAsync();
        }

        private async Task LoadAndParsePlaceholdersAsync()
        {
            _logger.LogInformation("Loading placeholders from {FilePath}", _placeholdersFilePath);

            if (!File.Exists(_placeholdersFilePath))
            {
                _logger.LogWarning("Placeholders file not found at {FilePath}", _placeholdersFilePath);
                _cachedPlaceholders = Array.Empty<PlaceholderInfo>();
                _cachedCategories = Array.Empty<string>();
                _cacheTimestamp = DateTime.UtcNow;
                return;
            }

            var content = await File.ReadAllTextAsync(_placeholdersFilePath);
            var placeholders = new List<PlaceholderInfo>();
            var categories = new List<string>();

            // Split content into lines for processing
            var lines = content.Split('\n');
            var currentCategory = "Algemeen";

            foreach (var line in lines)
            {
                var trimmedLine = line.TrimEnd('\r');

                // Check for category header
                var categoryMatch = CategoryHeaderPattern.Match(trimmedLine);
                if (categoryMatch.Success)
                {
                    var potentialCategory = categoryMatch.Groups[1].Value.Trim();

                    // Validate category: must contain a keyword and not be excluded
                    if (IsValidCategory(potentialCategory))
                    {
                        currentCategory = potentialCategory;
                        if (!categories.Contains(currentCategory))
                        {
                            categories.Add(currentCategory);
                        }
                    }
                    continue;
                }

                // Check for [[ ]] placeholders
                var doubleSquareMatch = DoubleSquareBracketPattern.Match(trimmedLine);
                if (doubleSquareMatch.Success)
                {
                    var name = doubleSquareMatch.Groups[1].Value.Trim();
                    var description = doubleSquareMatch.Groups[2].Value.Trim();

                    // Skip grammar rules (they contain '/')
                    if (!name.Contains('/'))
                    {
                        placeholders.Add(new PlaceholderInfo
                        {
                            Name = name,
                            Description = description,
                            Category = currentCategory,
                            Format = "[[ ]]"
                        });
                    }
                    continue;
                }

                // Check for { } placeholders
                var curlyMatch = CurlyBracePattern.Match(trimmedLine);
                if (curlyMatch.Success)
                {
                    var name = curlyMatch.Groups[1].Value.Trim();
                    var description = curlyMatch.Groups[2].Value.Trim();

                    placeholders.Add(new PlaceholderInfo
                    {
                        Name = name,
                        Description = description,
                        Category = currentCategory,
                        Format = "{ }"
                    });
                }
            }

            _cachedPlaceholders = placeholders;
            _cachedCategories = categories;
            _cacheTimestamp = DateTime.UtcNow;

            _logger.LogInformation("Loaded {Count} placeholders in {CategoryCount} categories",
                placeholders.Count, categories.Count);
        }

        private static bool IsValidCategory(string potentialCategory)
        {
            // Must be at least 5 characters
            if (potentialCategory.Length < 5)
                return false;

            // Check if it contains an excluded pattern
            foreach (var excluded in ExcludedCategoryPatterns)
            {
                if (potentialCategory.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Check if it contains at least one category keyword
            var lowerCategory = potentialCategory.ToLowerInvariant();
            foreach (var keyword in CategoryKeywords)
            {
                if (lowerCategory.Contains(keyword))
                    return true;
            }

            return false;
        }
    }
}

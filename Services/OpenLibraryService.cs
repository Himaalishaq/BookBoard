using BookBoard.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookBoard.Services
{
    public class OpenLibraryService
    {
        private readonly HttpClient _httpClient;

        public OpenLibraryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<BookSearchResult>> SearchBooksAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<BookSearchResult>();
            }

            string encodedQuery = Uri.EscapeDataString(query);

            string url =
                $"https://openlibrary.org/search.json?q={encodedQuery}&limit=12&fields=title,author_name,first_publish_year,cover_i,first_sentence";

            var response = await _httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url);

            if (response?.Docs == null)
            {
                return new List<BookSearchResult>();
            }

            return response.Docs
                .Where(book => !string.IsNullOrWhiteSpace(book.Title))
                .Select(book => new BookSearchResult
                {
                    Title = book.Title ?? string.Empty,
                    Author = book.AuthorNames?.FirstOrDefault() ?? "Unknown author",
                    PublishedYear = book.FirstPublishYear,
                    CoverUrl = book.CoverId.HasValue
                        ? $"https://covers.openlibrary.org/b/id/{book.CoverId.Value}-L.jpg"
                        : string.Empty,
                    ShortDescription = ExtractFirstSentence(book.FirstSentence)
                })
                .ToList();
        }

        private static string ExtractFirstSentence(JsonElement? firstSentence)
        {
            if (firstSentence == null)
            {
                return string.Empty;
            }

            JsonElement element = firstSentence.Value;

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        return item.GetString() ?? string.Empty;
                    }
                }
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private class OpenLibrarySearchResponse
        {
            [JsonPropertyName("docs")]
            public List<OpenLibraryBookDoc>? Docs { get; set; }
        }

        private class OpenLibraryBookDoc
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("author_name")]
            public List<string>? AuthorNames { get; set; }

            [JsonPropertyName("first_publish_year")]
            public int? FirstPublishYear { get; set; }

            [JsonPropertyName("cover_i")]
            public int? CoverId { get; set; }

            [JsonPropertyName("first_sentence")]
            public JsonElement? FirstSentence { get; set; }
        }
    }
}
using Fuzz.Domain.Services.Interfaces;
using Google.GenAI.Types;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Fuzz.Domain.Services.Tools;

public class WebScraperAiTool : IAiTool
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebScraperAiTool(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public FunctionDeclaration GetDefinition()
    {
        return new FunctionDeclaration
        {
            Name = "ScrapeUrl",
            Description = "Fetches and reads the textual content of a given website URL. USE THIS TOOL when the user provides a URL.",
            Parameters = new Schema
            {
                Type = Google.GenAI.Types.Type.OBJECT,
                Properties = new Dictionary<string, Schema>
                {
                    { 
                        "url", 
                        new Schema { 
                            Type = Google.GenAI.Types.Type.STRING, 
                            Description = "The valid URL to scrape (e.g., https://example.com)." 
                        } 
                    }
                },
                Required = new List<string> { "url" }
            }
        };
    }

    public string? CheckGuardrails(Dictionary<string, object?> args)
    {
        // Basic check: Ensure URL is provided. We could add domain allowlisting here later.
        if (!args.ContainsKey("url")) return "Guardrails: URL is missing.";
        return null;
    }

    public async Task<object> ExecuteAsync(Dictionary<string, object?> args, string userId)
    {
        if (!args.TryGetValue("url", out var urlObj) || urlObj == null)
            return "Error: 'url' parameter is missing.";

        string url = urlObj.ToString() ?? "";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var validatedUri))
        {
            return "Error: Invalid URL format.";
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            
            var content = await client.GetStringAsync(validatedUri);

            return CleanHtml(content);
        }
        catch (HttpRequestException ex)
        {
            return $"Network Error: Could not fetch the page. ({ex.Message})";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//comment()");
        if (nodesToRemove != null)
        {
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
        }

        string text = doc.DocumentNode.InnerText;
        text = HtmlEntity.DeEntitize(text);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}

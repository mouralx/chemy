using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Chemy.Core.Cloud;

public record PubChemQueryResult(
    long Cid,
    string Query,
    string IupacName,
    string MolecularFormula,
    double MolecularWeight,
    string Smiles,
    string InChIKey
);

public class PubChemClient
{
    private readonly HttpClient _httpClient;

    public PubChemClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri("https://pubchem.ncbi.nlm.nih.gov/rest/pug/") };
    }

    public async Task<PubChemQueryResult?> SearchCompoundAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        try
        {
            string url = $"compound/name/{Uri.EscapeDataString(query)}/property/IUPACName,MolecularFormula,MolecularWeight,CanonicalSMILES,InChIKey/JSON";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var raw = await response.Content.ReadFromJsonAsync<PubChemPropertyResponse>(cancellationToken);
            var prop = raw?.PropertyTable?.Properties?.FirstOrDefault();

            if (prop == null) return null;

            return new PubChemQueryResult(
                prop.Cid,
                query,
                prop.IupacName ?? query,
                prop.MolecularFormula ?? "Unknown",
                prop.MolecularWeight,
                prop.CanonicalSmiles ?? query,
                prop.InChIKey ?? "Unknown"
            );
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class PubChemPropertyResponse
{
    [JsonPropertyName("PropertyTable")]
    public PropertyTableContainer? PropertyTable { get; set; }
}

internal sealed class PropertyTableContainer
{
    [JsonPropertyName("Properties")]
    public List<PubChemPropertyItem>? Properties { get; set; }
}

internal sealed class PubChemPropertyItem
{
    [JsonPropertyName("CID")]
    public long Cid { get; set; }

    [JsonPropertyName("IUPACName")]
    public string? IupacName { get; set; }

    [JsonPropertyName("MolecularFormula")]
    public string? MolecularFormula { get; set; }

    [JsonPropertyName("MolecularWeight")]
    public double MolecularWeight { get; set; }

    [JsonPropertyName("CanonicalSMILES")]
    public string? CanonicalSmiles { get; set; }

    [JsonPropertyName("InChIKey")]
    public string? InChIKey { get; set; }
}


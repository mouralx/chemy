using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chemy.Core;
using Chemy.Core.Documentation;

namespace Chemy.Web.Pages;

/// <summary>
/// Main Laboratory Workstation Page Model.
/// Handles 3D molecular geometry generation, catalog selection, and telemetry updates.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>Active formula or SMILES string bound from query string or input.</summary>
    [BindProperty(SupportsGet = true)]
    public string Formula { get; set; } = "H2O";

    /// <summary>Optional VSEPR geometry override (e.g. Linear, Tetrahedral).</summary>
    [BindProperty(SupportsGet = true)]
    public string? OverrideShape { get; set; } = "Auto";

    public string MoleculeName { get; set; } = string.Empty;
    public string ChemicalFormula { get; set; } = string.Empty;
    public string VseprShape { get; set; } = string.Empty;
    public double IdealBondAngleDegrees { get; set; }
    public double MolecularWeight { get; set; }
    public int TotalAtomCount { get; set; }
    public List<string> ElementsPresent { get; set; } = new();
    public List<string> FunctionalGroups { get; set; } = new();
    public string PdbContent { get; set; } = string.Empty;
    public string XyzContent { get; set; } = string.Empty;
    public string MolContent { get; set; } = string.Empty;
    public string PlanarPdbContent { get; set; } = string.Empty;
    public string PlanarXyzContent { get; set; } = string.Empty;
    public string PlanarMolContent { get; set; } = string.Empty;
    public string SkeletalSvgContent { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool IsApiConnected { get; set; }

    /// <summary>Map of documentation key to full markdown file content loaded from disk.</summary>
    public Dictionary<string, string> DocumentationMap { get; set; } = new();

    /// <summary>Extracted C# class documentation metadata directly from source code XML doc comments.</summary>
    public List<CodeClassDoc> ClassDocumentationList { get; set; } = new();

    public async Task OnGetAsync()
    {
        LoadDocumentation();
        ClassDocumentationList = CodeRefExtractor.Extract(typeof(Chemy.Core.Molecule).Assembly);

        _logger.LogInformation("Workstation rendering molecule: '{Formula}' (Override: {Shape})", Formula, OverrideShape);

        var client = _httpClientFactory.CreateClient("ChemyApi");

        try
        {
            var response = await client.PostAsJsonAsync("/api/v1/geometry/3d", new { Formula, Name = Formula, OverrideShape });
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<Geometry3DApiResponse>();
                if (data != null)
                {
                    MoleculeName = data.Name;
                    ChemicalFormula = data.ChemicalFormula;
                    VseprShape = data.VseprShape;
                    IdealBondAngleDegrees = data.IdealBondAngleDegrees;
                    MolecularWeight = data.MolecularWeight;
                    TotalAtomCount = data.TotalAtomCount > 0 ? data.TotalAtomCount : 3;
                    ElementsPresent = data.ElementsPresent ?? new();
                    FunctionalGroups = data.FunctionalGroups ?? new();
                    PdbContent = data.PdbFormat;
                    XyzContent = data.XyzFormat;
                    MolContent = data.MolFormat ?? string.Empty;
                    SkeletalSvgContent = data.SkeletalSvg ?? string.Empty;
                    IsApiConnected = true;

                    _logger.LogDebug("Fetched 3D geometry from Chemy.Api for {Formula}", ChemicalFormula);
                }
            }
            else
            {
                _logger.LogWarning("Chemy.Api returned non-success code {StatusCode}. Falling back to server-side computation.", response.StatusCode);
                FallbackComputeServerSide();
            }

            // Also fetch or compute planar 2D-in-3D representation
            var planarResp = await client.PostAsJsonAsync("/api/v1/geometry/planar-3d", new { Formula, Name = Formula });
            if (planarResp.IsSuccessStatusCode)
            {
                var planarData = await planarResp.Content.ReadFromJsonAsync<Geometry3DApiResponse>();
                if (planarData != null)
                {
                    PlanarPdbContent = planarData.PdbFormat;
                    PlanarXyzContent = planarData.XyzFormat;
                    PlanarMolContent = planarData.MolFormat ?? string.Empty;
                    if (string.IsNullOrEmpty(SkeletalSvgContent))
                    {
                        SkeletalSvgContent = planarData.SkeletalSvg ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Chemy.Api. Falling back to local Chemy.Core engine.");
            FallbackComputeServerSide();
        }

        if (string.IsNullOrEmpty(PlanarXyzContent) || string.IsNullOrEmpty(SkeletalSvgContent))
        {
            Molecule? m = null;
            if (Chemy.Core.Structure.CompoundRegistry.TryResolve(Formula, out var regName, out var regSmiles) && Molecule.TryParseSmiles(regSmiles, regName, out var rm))
            {
                m = rm;
            }
            else if (Molecule.TryParseSmiles(Formula, Formula, out var sm) && sm.Bonds.Count > 0)
            {
                m = sm;
            }
            else if (Molecule.TryParse(Formula, Formula, out var fm))
            {
                m = fm;
            }

            if (m != null)
            {
                var planar = m.ToPlanar3D();
                PlanarXyzContent = planar.ToXyz();
                PlanarPdbContent = planar.ToPdb();
                PlanarMolContent = Chemy.Core.IO.MolfileExporter.ToMolfileV2000(planar);
                SkeletalSvgContent = m.ToSkeletalSvg(true);
            }
        }
    }

    /// <summary>
    /// Fallback direct execution via Chemy.Core if the external API is unreachable.
    /// </summary>
    private void FallbackComputeServerSide()
    {
        IsApiConnected = false;
        Molecule? molecule = null;

        if (Chemy.Core.Structure.CompoundRegistry.TryResolve(Formula, out var regName, out var regSmiles) && Molecule.TryParseSmiles(regSmiles, regName, out var rm))
        {
            molecule = rm;
        }
        else if (Molecule.TryParseSmiles(Formula, Formula, out var sm) && sm.Bonds.Count > 0)
        {
            molecule = sm;
        }
        else if (Molecule.TryParse(Formula, Formula, out var fm))
        {
            molecule = fm;
        }

        if (molecule != null)
        {
            var m3d = molecule.To3D(OverrideShape);
            var planar = molecule.ToPlanar3D();

            MoleculeName = m3d.Name;
            ChemicalFormula = m3d.ChemicalFormula;
            VseprShape = m3d.VseprShape;
            IdealBondAngleDegrees = m3d.IdealBondAngleDegrees;
            MolecularWeight = molecule.MolecularWeight;
            TotalAtomCount = molecule.Atoms.Count;
            ElementsPresent = molecule.Atoms.Select(a => a.Element.Symbol).Distinct().ToList();
            FunctionalGroups = molecule.GetFunctionalGroups().Select(fg => fg.ToString()).ToList();
            PdbContent = m3d.ToPdb();
            XyzContent = m3d.ToXyz();
            MolContent = Chemy.Core.IO.MolfileExporter.ToMolfileV2000(m3d);

            PlanarPdbContent = planar.ToPdb();
            PlanarXyzContent = planar.ToXyz();
            PlanarMolContent = Chemy.Core.IO.MolfileExporter.ToMolfileV2000(planar);
            SkeletalSvgContent = molecule.ToSkeletalSvg(true);
            IsApiConnected = true;
        }
        else
        {
            ErrorMessage = $"Could not parse '{Formula}'";
            _logger.LogError("Unable to parse molecular input '{Formula}'", Formula);
        }
    }

    private void LoadDocumentation()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "docs"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../docs"),
            Path.Combine(AppContext.BaseDirectory, "docs")
        };

        string? docsFolder = candidates.FirstOrDefault(Directory.Exists);
        if (docsFolder != null)
        {
            DocumentationMap["home"] = ReadDocFile(docsFolder, "README.md");
            DocumentationMap["api"] = ReadDocFile(docsFolder, "API_REFERENCE.md");
            DocumentationMap["cookbook"] = ReadDocFile(docsFolder, "COOKBOOK.md");
            DocumentationMap["credibility"] = ReadDocFile(docsFolder, "SCIENTIFIC_CREDIBILITY_REPORT.md");
            DocumentationMap["benchmarks"] = ReadDocFile(docsFolder, "SCIENTIFIC_VERIFICATION_BENCHMARKS.md");
            DocumentationMap["showcase"] = ReadDocFile(docsFolder, "BREAKTHROUGHS_SHOWCASE.md");
            DocumentationMap["arch"] = ReadDocFile(docsFolder, "ARCHITECTURE.md");
            DocumentationMap["started"] = ReadDocFile(docsFolder, "GETTING_STARTED.md");
            DocumentationMap["science"] = ReadDocFile(docsFolder, "SCIENTIFIC_APPROACH.md");
            DocumentationMap["audit"] = ReadDocFile(docsFolder, "CODEX_AUDIT_v2.8.md");
        }
    }

    private static string ReadDocFile(string folder, string fileName)
    {
        string path = Path.Combine(folder, fileName);
        return System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : $"# Documentation Error\n\nCould not locate markdown file `{fileName}`.";
    }
}

/// <summary>DTO contract matching Chemy.Api /api/v1/geometry/3d endpoint response.</summary>
public record Geometry3DApiResponse(
    string Name,
    string ChemicalFormula,
    string VseprShape,
    double IdealBondAngleDegrees,
    double MolecularWeight,
    int TotalAtomCount,
    List<string>? ElementsPresent,
    List<string>? FunctionalGroups,
    string XyzFormat,
    string PdbFormat,
    string? MolFormat = null,
    bool? IsPlanar = false,
    string? SkeletalSvg = null
);

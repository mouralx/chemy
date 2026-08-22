using Chemy.Core;
using Chemy.Core.Cloud;
using Chemy.Core.Electrochemistry;
using Chemy.Core.Environmental;
using Chemy.Core.Evolution;
using Chemy.Core.Kinetics;
using Chemy.Core.Parsing;
using Chemy.Core.Pharmacology;
using Chemy.Core.Physics;
using Chemy.Core.Quantum;
using Chemy.Core.Rendering;
using Chemy.Core.Solutions;
using Chemy.Core.Spatial;
using Chemy.Core.Spectroscopy;
using Chemy.Core.Structure;
using Chemy.Core.Thermodynamics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var securityConfiguration = builder.Configuration.GetSection("ApiSecurity");
int rateLimitPermit = Math.Clamp(securityConfiguration.GetValue<int?>("RateLimitPermit") ?? 120, 1, 10_000);
int rateLimitWindowSeconds = Math.Clamp(securityConfiguration.GetValue<int?>("RateLimitWindowSeconds") ?? 60, 1, 3_600);
long maxRequestBodyBytes = Math.Clamp(securityConfiguration.GetValue<long?>("MaxRequestBodyBytes") ?? 65_536, 1_024, 10_485_760);
string[] allowedOrigins = securityConfiguration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

// ============================================================================
// 1. SERVICE REGISTRATIONS & DEPENDENCY INJECTION (Microsoft .NET Best Practices)
// ============================================================================

// OpenAPI & API Documentation services
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});

// Health Check probes for container orchestration (Kubernetes / Docker)
builder.Services.AddHealthChecks();

// Cross-Origin Resource Sharing (CORS) configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        PathString path = context.Request.Path;
        if (path.StartsWithSegments("/healthz") ||
            path.StartsWithSegments("/openapi") ||
            path.StartsWithSegments("/scalar") ||
            path.StartsWithSegments("/swagger"))
        {
            return RateLimitPartition.GetNoLimiter("infrastructure");
        }

        string clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitPermit,
            Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
    options.OnRejected = async (rejectionContext, cancellationToken) =>
    {
        rejectionContext.HttpContext.Response.Headers.RetryAfter = rateLimitWindowSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Request rate limit exceeded",
            detail: "Retry after the interval advertised by the Retry-After response header.",
            extensions: new Dictionary<string, object?> { ["traceId"] = rejectionContext.HttpContext.TraceIdentifier })
            .ExecuteAsync(rejectionContext.HttpContext);
    };
});

// Resilient Typed HttpClient for NCBI PubChem live cloud database queries
builder.Services.AddHttpClient<PubChemClient>(client =>
{
    client.BaseAddress = new Uri("https://pubchem.ncbi.nlm.nih.gov/rest/pug/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
bool requireApiKey = !app.Environment.IsDevelopment() && securityConfiguration.GetValue("RequireApiKey", true);
string? configuredApiKey = securityConfiguration["ApiKey"];

if (requireApiKey && string.IsNullOrWhiteSpace(configuredApiKey))
{
    throw new InvalidOperationException(
        "Production API authentication is enabled, but ApiSecurity:ApiKey is not configured. Supply it through a secret provider or environment variable.");
}

logger.LogInformation("Chemy Computational Chemistry & Chemoinformatics REST API initialized successfully.");

// ============================================================================
// 2. HTTP MIDDLEWARE PIPELINE
// ============================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler(exceptionApplication =>
{
    exceptionApplication.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", context.TraceIdentifier);
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected server error occurred",
            detail: "Use the traceId when contacting support.",
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
            .ExecuteAsync(context);
    });
});

app.Use(async (context, next) =>
{
    string? suppliedCorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    bool validCorrelationId = suppliedCorrelationId is { Length: > 0 and <= 64 } &&
        suppliedCorrelationId.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
    string correlationId = validCorrelationId ? suppliedCorrelationId! : context.TraceIdentifier;
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";

    using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        await next(context);
    }
});

app.UseCors();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (requireApiKey && context.Request.Path.StartsWithSegments("/api/v1"))
    {
        string suppliedApiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault() ?? string.Empty;
        byte[] suppliedBytes = Encoding.UTF8.GetBytes(suppliedApiKey);
        byte[] configuredBytes = Encoding.UTF8.GetBytes(configuredApiKey!);
        bool authenticated = suppliedBytes.Length == configuredBytes.Length &&
            CryptographicOperations.FixedTimeEquals(suppliedBytes, configuredBytes);
        if (!authenticated)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication required",
                detail: "Supply a valid API credential.",
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
                .ExecuteAsync(context);
            return;
        }
    }

    await next(context);
});

bool exposeDocumentation = app.Environment.IsDevelopment() || securityConfiguration.GetValue("ExposeDocumentation", false);
if (exposeDocumentation)
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "Chemy API v1");
        c.RoutePrefix = "swagger";
    });
}

// Root endpoint redirects directly to interactive Scalar API documentation
app.MapGet("/", () => exposeDocumentation ? Results.Redirect("/scalar/v1") : Results.NoContent())
   .ExcludeFromDescription();


// ============================================================================
// 3. SYSTEM HEALTH & MONITORING ENDPOINTS
// ============================================================================

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            durationMilliseconds = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new { status = entry.Value.Status.ToString(), durationMilliseconds = entry.Value.Duration.TotalMilliseconds })
        });
    }
})
.WithTags("System Health")
.WithSummary("Service Health Check")
.WithDescription("Returns HTTP 200 OK if the Chemy API microservice is healthy and ready for traffic.");

// ============================================================================
// 4. PERIODIC TABLE CATALOG ENDPOINTS
// ============================================================================

var elementsGroup = app.MapGroup("/api/v1/elements").WithTags("Periodic Table");

elementsGroup.MapGet("/", (ILogger<Program> log) =>
{
    log.LogInformation("Retrieving all 118 periodic table elements.");
    return Results.Ok(Elements.All);
})
.WithSummary("Get all periodic table elements")
.WithDescription("Returns the complete catalog of all 118 IUPAC chemical elements.");

elementsGroup.MapGet("/{query}", (string query, ILogger<Program> log) =>
{
    log.LogInformation("Looking up element for query: '{Query}'", query);

    if (int.TryParse(query, out int atomicNumber))
    {
        if (Elements.TryGetByAtomicNumber(atomicNumber, out var elementByNum))
        {
            log.LogDebug("Found element by atomic number {AtomicNumber}: {Symbol} ({Name})", atomicNumber, elementByNum.Symbol, elementByNum.Name);
            return Results.Ok(elementByNum);
        }

        log.LogWarning("Element with atomic number {AtomicNumber} not found.", atomicNumber);
        return Results.NotFound(new { error = $"Element with atomic number {atomicNumber} not found." });
    }

    if (Elements.TryGetBySymbol(query, out var elementBySym))
    {
        log.LogDebug("Found element by symbol '{Symbol}': {Name}", query, elementBySym.Name);
        return Results.Ok(elementBySym);
    }

    log.LogWarning("Element with symbol '{Symbol}' not found.", query);
    return Results.NotFound(new { error = $"Element with symbol '{query}' not found." });
})
.WithSummary("Get element by symbol or atomic number")
.WithDescription("Finds an element by its symbol (e.g. 'Fe', 'H') or atomic number (e.g. 1, 26).");

// ============================================================================
// 5. MOLECULAR STRUCTURE & 3D GEOMETRY ENDPOINTS
// ============================================================================

var moleculesGroup = app.MapGroup("/api/v1/molecules").WithTags("Molecular Structure & 3D");

moleculesGroup.MapPost("/parse", (FormulaRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Parsing chemical formula: '{Formula}' (Name: {Name})", request.Formula, request.Name);

    if (!Molecule.TryParse(request.Formula, request.Name, out var molecule, out string? error))
    {
        log.LogWarning("Formula parsing failed for '{Formula}': {Error}", request.Formula, error);
        return Results.BadRequest(new { error });
    }

    log.LogDebug("Formula parsed successfully: {Formula} with {AtomCount} atoms, MW = {MW} g/mol", molecule.ChemicalFormula, molecule.Atoms.Count, molecule.MolecularWeight);

    return Results.Ok(new
    {
        molecule.Name,
        Formula = molecule.ChemicalFormula,
        molecule.MolecularWeight,
        molecule.NetCharge,
        AtomsCount = molecule.Atoms.Count
    });
})
.WithSummary("Parse chemical formula")
.WithDescription("Parses complex chemical formula strings (including brackets, hydrates, and charges) into Molecule objects.");

moleculesGroup.MapPost("/svg", (HttpContext context, FormulaRequest request, bool? download, bool? isDarkMode, ILogger<Program> log) =>
{
    log.LogInformation("Rendering vector SVG for molecule: '{Formula}'", request.Formula);

    if (!Molecule.TryParse(request.Formula, request.Name, out var molecule, out string? error))
    {
        log.LogWarning("Formula parsing failed for SVG generation: {Error}", error);
        return Results.BadRequest(new { error });
    }

    string svg = molecule.ToSvg(isDarkMode ?? true);

    if (download == true)
    {
        string filename = $"{molecule.ChemicalFormula}.svg";
        context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");
        log.LogDebug("Attached Content-Disposition header for SVG download: {Filename}", filename);
    }

    return Results.Content(svg, "image/svg+xml");
})
.WithSummary("Render or download molecule vector SVG card")
.WithDescription("Generates an SVG vector card for a molecule. Pass query parameter ?download=true to trigger file download in browser.");

var smilesGroup = app.MapGroup("/api/v1/smiles").WithTags("Organic SMILES");

smilesGroup.MapPost("/parse", (SmilesRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Parsing organic SMILES string: '{Smiles}'", request.Smiles);

    try
    {
        var molecule = Molecule.FromSmiles(request.Smiles, request.Name);
        var functionalGroups = molecule.GetFunctionalGroups().Select(fg => fg.ToString()).ToList();

        log.LogDebug("SMILES parsed: {Formula}, MW = {MW} g/mol, Detected Groups: {Groups}", molecule.ChemicalFormula, molecule.MolecularWeight, string.Join(", ", functionalGroups));

        return Results.Ok(new
        {
            molecule.Name,
            Formula = molecule.ChemicalFormula,
            molecule.MolecularWeight,
            FunctionalGroups = functionalGroups,
            AtomsCount = molecule.Atoms.Count
        });
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Failed to parse SMILES '{Smiles}'", request.Smiles);
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithSummary("Parse organic SMILES notation")
.WithDescription("Parses organic SMILES strings, calculates implicit hydrogen topology, and detects functional groups (Alcohols, Carboxylic Acids, Esters, Ketones, etc.).");

var geometryGroup = app.MapGroup("/api/v1/geometry").WithTags("Molecular Structure & 3D");

geometryGroup.MapPost("/3d", (FormulaRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Generating 3D spatial geometry for: '{Formula}' (Override: {OverrideShape}, Planar: {IsPlanar})", request.Formula, request.OverrideShape, request.IsPlanar);

    if (!TryParseChemicalInput(request.Formula, request.Name, out var molecule))
    {
        log.LogWarning("Input '{Formula}' could not be parsed as formula or SMILES.", request.Formula);
        return Results.BadRequest(new { error = $"Could not parse input '{request.Formula}' as a chemical formula or SMILES string." });
    }

    var m3d = request.IsPlanar == true ? molecule.ToPlanar3D() : molecule.To3D(request.OverrideShape);
    var functionalGroups = molecule.GetFunctionalGroups().Select(fg => fg.ToString()).ToList();
    var elements = molecule.Atoms.Select(a => a.Element.Symbol).Distinct().ToList();

    log.LogDebug("3D geometry generated: Shape = {Shape}, Angle = {Angle}°", m3d.VseprShape, m3d.IdealBondAngleDegrees);

    return Results.Ok(new
    {
        m3d.Name,
        m3d.ChemicalFormula,
        m3d.VseprShape,
        m3d.IdealBondAngleDegrees,
        IsPlanar = request.IsPlanar == true,
        MolecularWeight = molecule.MolecularWeight,
        TotalAtomCount = molecule.Atoms.Count,
        ElementsPresent = elements,
        FunctionalGroups = functionalGroups,
        XyzFormat = m3d.ToXyz(),
        PdbFormat = m3d.ToPdb(),
        MolFormat = Chemy.Core.IO.MolfileExporter.ToMolfileV2000(m3d),
        SkeletalSvg = molecule.ToSkeletalSvg(true)
    });
})
.WithSummary("Calculate 3D molecular geometry & VSEPR shape")
.WithDescription("Calculates 3D spatial coordinates (X, Y, Z), VSEPR geometries, and generates XYZ, PDB, and 2D skeletal SVG representations.");

geometryGroup.MapPost("/planar-3d", (FormulaRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Generating Planar 2D-in-3D representation for: '{Formula}'", request.Formula);

    if (!TryParseChemicalInput(request.Formula, request.Name, out var molecule))
    {
        log.LogWarning("Input '{Formula}' could not be parsed as formula or SMILES.", request.Formula);
        return Results.BadRequest(new { error = $"Could not parse input '{request.Formula}' as a chemical formula or SMILES string." });
    }

    var m3d = molecule.ToPlanar3D();
    var functionalGroups = molecule.GetFunctionalGroups().Select(fg => fg.ToString()).ToList();
    var elements = molecule.Atoms.Select(a => a.Element.Symbol).Distinct().ToList();

    log.LogDebug("Planar 2D-in-3D geometry generated: {Formula} with Z=0", m3d.ChemicalFormula);

    return Results.Ok(new
    {
        m3d.Name,
        m3d.ChemicalFormula,
        m3d.VseprShape,
        m3d.IdealBondAngleDegrees,
        IsPlanar = true,
        MolecularWeight = molecule.MolecularWeight,
        TotalAtomCount = molecule.Atoms.Count,
        ElementsPresent = elements,
        FunctionalGroups = functionalGroups,
        Atoms = m3d.Atoms.Select(a => new { a.Atom.Element.Symbol, a.Position.X, a.Position.Y, a.Position.Z }),
        XyzFormat = m3d.ToXyz(),
        PdbFormat = m3d.ToPdb(),
        MolFormat = Chemy.Core.IO.MolfileExporter.ToMolfileV2000(m3d),
        SkeletalSvg = molecule.ToSkeletalSvg(true)
    });
})
.WithSummary("Calculate Planar 2D-in-3D layout (Z = 0.0)")
.WithDescription("Generates textbook ChemDraw-style 2D structural diagram coordinates embedded in 3D Euclidean space (Z = 0.0) for 3Dmol.js rendering.");

geometryGroup.MapPost("/skeletal-2d", (FormulaRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Generating 2D Skeletal ChemDraw vector SVG for: '{Formula}'", request.Formula);

    if (!TryParseChemicalInput(request.Formula, request.Name, out var molecule))
    {
        log.LogWarning("Input '{Formula}' could not be parsed as formula or SMILES.", request.Formula);
        return Results.BadRequest(new { error = $"Could not parse input '{request.Formula}' as a chemical formula or SMILES string." });
    }

    string svg = molecule.ToSkeletalSvg(true, 600, 400);
    log.LogDebug("Generated Skeletal 2D SVG for: {Formula}", molecule.ChemicalFormula);

    return Results.Ok(new
    {
        molecule.Name,
        molecule.ChemicalFormula,
        molecule.MolecularWeight,
        SvgContent = svg
    });
})
.WithSummary("Render IUPAC / ChemDraw 2D Skeletal Vector SVG")
.WithDescription("Generates textbook 2D skeletal line diagram with implicit carbons, parallel double bonds, and heteroatom labels.");

// ============================================================================
// 6. SOLUTIONS CHEMISTRY & ACID-BASE EQUILIBRIA ENDPOINTS
// ============================================================================

var solutionsGroup = app.MapGroup("/api/v1/solutions").WithTags("Solutions & Acid-Base");

solutionsGroup.MapPost("/ph", (PhRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Calculating solution pH for Concentration = {Conc} M (Ka = {Ka})", request.ConcentrationMolar, request.Ka);

    try
    {
        var result = request.Ka.HasValue
            ? SolutionsEngine.CalculateWeakAcidPh(request.ConcentrationMolar, request.Ka.Value)
            : SolutionsEngine.CalculateStrongAcidPh(request.ConcentrationMolar);

        log.LogDebug("pH calculated: pH = {pH}, pOH = {pOH}", result.Ph, result.Poh);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error calculating solution pH");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithSummary("Calculate solution pH & pOH")
.WithDescription("Calculates pH, pOH, [H+], and [OH-] for strong or weak acid solutions.");

solutionsGroup.MapPost("/buffer", (BufferRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Solving Henderson-Hasselbalch buffer: pKa = {pKa}, [HA] = {AcidM} M, [A-] = {BaseM} M", request.Pka, request.AcidConcentrationMolar, request.ConjugateBaseConcentrationMolar);

    try
    {
        var result = SolutionsEngine.CalculateBufferPh(request.Pka, request.AcidConcentrationMolar, request.ConjugateBaseConcentrationMolar);
        log.LogDebug("Buffer pH calculated: {pH}", result.Ph);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error solving buffer pH");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithSummary("Calculate Henderson-Hasselbalch buffer pH")
.WithDescription("Solves the Henderson-Hasselbalch equation for acid/conjugate-base buffer solutions.");

// ============================================================================
// 7. ELECTROCHEMISTRY & NERNST POTENTIAL ENDPOINTS
// ============================================================================

var electroGroup = app.MapGroup("/api/v1/electrochemistry").WithTags("Electrochemistry");

electroGroup.MapPost("/nernst", (NernstRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Calculating Nernst potential: E° = {E0} V, n = {n}, Q = {Q}, T = {T} K", request.StandardCellPotentialVolts, request.ElectronsTransferred, request.ReactionQuotientQ, request.TemperatureKelvin);

    try
    {
        var result = ElectrochemistryEngine.CalculateNernstPotential(
            request.StandardCellPotentialVolts,
            request.ElectronsTransferred,
            request.ReactionQuotientQ,
            request.TemperatureKelvin ?? 298.15
        );
        log.LogDebug("Nernst result: E_cell = {Ecell} V (Spontaneous: {Spont})", result.CellPotentialVolts, result.IsSpontaneousGalvanic);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error calculating Nernst potential");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithSummary("Calculate Nernst cell potential")
.WithDescription("Calculates non-standard electrochemical cell potential (E_cell) via the Nernst equation.");

// ============================================================================
// 8. CHEMICAL KINETICS & REACTION NETWORKS ENDPOINTS
// ============================================================================

var kineticsGroup = app.MapGroup("/api/v1/kinetics").WithTags("Chemical Kinetics");

kineticsGroup.MapPost("/arrhenius", (ArrheniusRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Calculating Arrhenius rate constant: A = {A}, Ea = {Ea} kJ/mol, T = {T} K", request.PreExponentialFactorA, request.ActivationEnergykJPerMol, request.TemperatureKelvin);

    try
    {
        var result = KineticsEngine.CalculateRateConstant(
            request.PreExponentialFactorA,
            request.ActivationEnergykJPerMol,
            request.TemperatureKelvin
        );
        log.LogDebug("Arrhenius rate constant: k = {k} s⁻¹", result.RateConstantK);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error calculating Arrhenius rate constant");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithSummary("Calculate Arrhenius rate constant")
.WithDescription("Calculates reaction rate constant k as a function of temperature and activation energy E_a.");

kineticsGroup.MapPost("/network", (NetworkKineticsRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Simulating RK4 reaction network cascade: [A]0 = {ConcA} M, k1 = {k1}, k2 = {k2}, time = {time}s", request.InitialConcA, request.K1, request.K2, request.TotalTime);

    var res = ReactionNetworkEngine.SimulateConsecutiveCascade(
        request.InitialConcA,
        request.K1,
        request.K2,
        request.TotalTime,
        request.Steps
    );

    log.LogDebug("RK4 simulation completed with {PointCount} time steps.", res.Points.Count);
    return Results.Ok(res);
})
.WithSummary("Simulate multi-step reaction cascade kinetics (A -> B -> C)")
.WithDescription("Uses Runge-Kutta 4th Order (RK4) numerical differential equation integration to plot concentration trajectories.");

// ============================================================================
// 9. STOICHIOMETRY & REACTION BALANCER ENDPOINTS
// ============================================================================

var reactionsGroup = app.MapGroup("/api/v1/reactions").WithTags("Stoichiometry & Reactions");

reactionsGroup.MapPost("/balance", (ReactionRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Balancing chemical equation: '{Equation}'", request.Equation);

    if (!Reaction.TryParse(request.Equation, out var reaction, out string? error))
    {
        log.LogWarning("Reaction parsing failed for '{Equation}': {Error}", request.Equation, error);
        return Results.BadRequest(new { error });
    }

    var balanced = reaction.Balance();
    log.LogDebug("Reaction balanced: '{Balanced}'", balanced.ToString());

    return Results.Ok(new
    {
        Unbalanced = reaction.ToString(),
        Balanced = balanced.ToString(),
        Reactants = balanced.Reactants.Select(r => new { r.Molecule.Name, r.Molecule.ChemicalFormula, r.Coefficient }),
        Products = balanced.Products.Select(p => new { p.Molecule.Name, p.Molecule.ChemicalFormula, p.Coefficient })
    });
})
.WithSummary("Balance chemical reaction equation")
.WithDescription("Balances chemical equations using exact rational linear algebra to calculate minimal stoichiometric coefficients.");

reactionsGroup.MapPost("/explain", (ReactionRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Generating 5-step educational balancing breakdown for: '{Equation}'", request.Equation);

    if (!Reaction.TryParse(request.Equation, out var reaction, out string? error))
    {
        log.LogWarning("Reaction parsing failed for explanation: {Error}", error);
        return Results.BadRequest(new { error });
    }

    var explanation = reaction.BalanceWithSteps();

    return Results.Ok(new
    {
        Balanced = explanation.BalancedReaction.ToString(),
        Steps = explanation.Steps,
        MarkdownExplanation = explanation.FormattedExplanation
    });
})
.WithSummary("Generate step-by-step balancing explanation")
.WithDescription("Returns a structured 5-step educational balancing breakdown and formatted Markdown output.");

reactionsGroup.MapPost("/thermodynamics", (ThermoRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Calculating reaction thermodynamics for: '{Equation}' at T = {T} K", request.Equation, request.TemperatureKelvin);

    if (!Reaction.TryParse(request.Equation, out var reaction, out string? error))
    {
        log.LogWarning("Reaction parsing failed for thermodynamics: {Error}", error);
        return Results.BadRequest(new { error });
    }

    try
    {
        double tempK = request.TemperatureKelvin ?? 298.15;
        var thermo = reaction.GetThermodynamics(tempK);
        log.LogDebug("Thermodynamics result: ΔH = {dH} kJ, ΔG = {dG} kJ, Spontaneous = {Spont}", thermo.EnthalpyChangekJ, thermo.GibbsFreeEnergykJ, thermo.IsSpontaneous);
        return Results.Ok(thermo);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Error computing reaction thermodynamics");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithSummary("Calculate reaction thermodynamics")
.WithDescription("Calculates reaction Enthalpy (ΔH), Entropy (ΔS), and Gibbs Free Energy (ΔG) at a specified temperature.");

reactionsGroup.MapPost("/svg", (HttpContext context, ReactionRequest request, bool? download, bool? isDarkMode, ILogger<Program> log) =>
{
    log.LogInformation("Rendering vector SVG for reaction: '{Equation}'", request.Equation);

    if (!Reaction.TryParse(request.Equation, out var reaction, out string? error))
    {
        log.LogWarning("Reaction parsing failed for SVG: {Error}", error);
        return Results.BadRequest(new { error });
    }

    string svg = reaction.ToSvg(isDarkMode ?? true);

    if (download == true)
    {
        context.Response.Headers.Append("Content-Disposition", "attachment; filename=\"reaction.svg\"");
        log.LogDebug("Attached Content-Disposition header for reaction SVG download");
    }

    return Results.Content(svg, "image/svg+xml");
})
.WithSummary("Render or download reaction vector SVG diagram")
.WithDescription("Generates an SVG diagram for a reaction equation. Pass query parameter ?download=true to trigger file download in browser.");

// ============================================================================
// 10. SPECTROSCOPY PREDICTION ENGINE ENDPOINTS
// ============================================================================

var specGroup = app.MapGroup("/api/v1/spectroscopy").WithTags("Spectroscopy");

specGroup.MapPost("/predict", (SpectroscopyRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Predicting NMR and IR spectra for: '{Formula}'", request.Formula);

    if (!TryParseChemicalInput(request.Formula, request.Formula, out var target))
    {
        log.LogWarning("Spectroscopy parsing failed for '{Formula}'", request.Formula);
        return Results.BadRequest(new { error = $"Could not parse '{request.Formula}'" });
    }

    var prediction = SpectroscopyEngine.Predict(target);

    log.LogDebug("Spectroscopy predicted: {1HCount} 1H-NMR peaks, {13CCount} 13C-NMR peaks, {IRCount} IR bands", prediction.H1NmrPeaks.Count, prediction.C13NmrPeaks.Count, prediction.IrBands.Count);
    return Results.Ok(prediction);
})
.WithSummary("Predict 1H-NMR, 13C-NMR & IR spectrum bands")
.WithDescription("Predicts chemical shifts (ppm), peak multiplets, and IR absorption bands for functional groups.");

// ============================================================================
// 11. PHYSICS & FORCE FIELD ENERGY MINIMIZATION ENDPOINTS
// ============================================================================

var physicsGroup = app.MapGroup("/api/v1/physics").WithTags("Physics & Force Field");

physicsGroup.MapPost("/minimize", (ForceFieldRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Minimizing 3D molecular energy for: '{Formula}' (MaxIterations = {MaxIter})", request.Formula, request.MaxIterations);

    if (!TryParseChemicalInput(request.Formula, request.Formula, out var target))
    {
        log.LogWarning("Force field parsing failed for '{Formula}'", request.Formula);
        return Results.BadRequest(new { error = $"Could not parse '{request.Formula}'" });
    }

    int maxIterations = request.MaxIterations ?? 500;
    if (maxIterations is < 1 or > 2_000)
    {
        return Results.BadRequest(new { error = "MaxIterations must be between 1 and 2000." });
    }

    var result = Geometry3DEngine.GenerateConformer3DResult(target, request.OverrideShape, maxIterations);

    log.LogDebug(
        "Energy minimized: {Initial} -> {Final} kcal/mol ({Reason} after {Iters} iterations, gradient {Gradient})",
        result.InitialEnergyKcalPerMol,
        result.FinalEnergyKcalPerMol,
        result.TerminationReason,
        result.Iterations,
        result.FinalGradientNorm);
    return Results.Ok(result);
})
.WithSummary("Minimize 3D molecular energy using a UFF-inspired potential")
.WithDescription("Evaluates the five-term UFF-inspired potential and returns relaxed coordinates with explicit convergence, termination, energy, and gradient diagnostics.");

// ============================================================================
// 12. PUBCHEM LIVE CLOUD INTEGRATOR ENDPOINTS
// ============================================================================

var cloudGroup = app.MapGroup("/api/v1/cloud").WithTags("PubChem Cloud");

cloudGroup.MapGet("/pubchem/{query}", async (string query, PubChemClient client, CancellationToken cancellationToken, ILogger<Program> log) =>
{
    log.LogInformation("Querying NCBI PubChem live cloud database for: '{Query}'", query);

    var res = await client.SearchCompoundAsync(query, cancellationToken);
    if (res == null)
    {
        log.LogWarning("Compound '{Query}' not found on PubChem", query);
        return Results.NotFound(new { error = $"Compound '{query}' not found on PubChem cloud database." });
    }

    log.LogDebug("PubChem hit: CID = {CID}, Formula = {Formula}, MW = {MW} g/mol", res.Cid, res.MolecularFormula, res.MolecularWeight);
    return Results.Ok(res);
})
.WithSummary("Search PubChem live cloud database")
.WithDescription("Live query NCBI PubChem API for CID, IUPAC Name, Formula, SMILES, and InChIKey.");

// ============================================================================
// 13. PHARMACOLOGY & ADMET TOXICITY SHIELD ENDPOINTS
// ============================================================================

var pharmaGroup = app.MapGroup("/api/v1/pharmacology").WithTags("Pharmacology & ADMET");

pharmaGroup.MapPost("/admet", (AdmetRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Analyzing ADMET profile & Lipinski Rule of 5 for: '{Formula}'", request.Formula);

    if (!TryParseChemicalInput(request.Formula, request.Formula, out var target))
    {
        log.LogWarning("ADMET parsing failed for '{Formula}'", request.Formula);
        return Results.BadRequest(new { error = $"Could not parse '{request.Formula}'" });
    }

    var profile = AdmetEngine.Analyze(target);

    log.LogDebug("ADMET analyzed: MW = {MW}, LogP = {LogP}, QED = {QED}, Passes = {Passes}", profile.MolecularWeight, profile.CalculatedLogP, profile.QedDrugLikenessScore, profile.PassesLipinskiRuleOf5);
    return Results.Ok(profile);
})
.WithSummary("Screen physicochemical properties, Lipinski Rule of 5 & QED drug-likeness")
.WithDescription("Calculates Molecular Weight, LogP, TPSA, HBD, HBA, rotatable bonds, Lipinski/Veber/Ghose rules, and Bickerton QED drug-likeness desirability score.");

// ============================================================================
// 14. AUTONOMOUS MOLECULAR EVOLUTION & LEAD OPTIMIZATION ENDPOINTS
// ============================================================================

var evolutionGroup = app.MapGroup("/api/v1/evolution").WithTags("Molecular Evolution & Lead Optimization");

evolutionGroup.MapPost("/evolve", (EvolutionRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Executing de novo molecular evolution on lead: '{Input}' (Generations: {Gen})", request.Input, request.Generations);

    var result = MolecularEvolverEngine.EvolveLeadCandidate(request.Input, request.Generations ?? 50);

    log.LogDebug("Evolution completed. Generated {Count} candidates from baseline QED = {QED}", result.Candidates.Count, result.BaselineQed);
    return Results.Ok(result);
})
.WithSummary("Autonomous de novo molecular evolution & bioisosteric optimizer")
.WithDescription("Evolves 5 candidate drug derivatives with evaluated QED, LogP, and bioisosteric structural rationales.");

// ============================================================================
// 15. ENVIRONMENTAL & ECOCLEAN PFAS BIOCLEAVAGE ENDPOINTS
// ============================================================================

var envGroup = app.MapGroup("/api/v1/environmental").WithTags("Environmental & EcoClean");

envGroup.MapPost("/ecoclean", (EcoCleanRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Solving EcoClean biocleavage degradation cascade for: '{Pollutant}'", request.Pollutant);

    var result = EcoCleanEngine.SolveDegradationCascade(request.Pollutant);

    log.LogDebug("EcoClean solved: {Class}, Theoretical Products: {EndProducts}", result.PollutantClass, result.TheoreticalMineralizationProducts);
    return Results.Ok(result);
})
.WithSummary("Solve PFAS and microplastic biocleavage degradation pathways");
// ============================================================================
// 16. QUANTUM ELECTRONIC STRUCTURE & HÜCKEL MOLECULAR ORBITALS (HMO)
// ============================================================================

var quantumGroup = app.MapGroup("/api/v1/quantum").WithTags("Quantum & Molecular Orbitals");

quantumGroup.MapPost("/huckel", (HuckelRequest request, ILogger<Program> log) =>
{
    log.LogInformation("Computing Hückel Molecular Orbital (HMO) electronic structure for input: '{Formula}'", request.Formula);

    if (!TryParseChemicalInput(request.Formula, null, out var molecule))
    {
        return Results.BadRequest(new { error = $"Unable to parse '{request.Formula}' as a valid chemical formula or organic SMILES." });
    }

    try
    {
        var result = HuckelEngine.Analyze(molecule, request.BetaEv ?? HuckelEngine.DefaultBetaEv);
        log.LogDebug("Hückel calculation completed for '{Name}': HOMO={HomoBeta}β, LUMO={LumoBeta}β, Gap={Gap} eV, λ_max={Lambda} nm",
            result.MoleculeName, result.HomoEnergyBetaCoeff, result.LumoEnergyBetaCoeff, result.HomoLumoGapEv, result.EstimatedUvVisMaxWavelengthNm);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithSummary("Compute Hückel Molecular Orbitals (HMO), HOMO/LUMO, and UV-Vis spectrum")
.WithDescription("Solves the secular determinant det|H - E·I| = 0 via exact Jacobi symmetric matrix diagonalization for conjugated π-systems.");

app.Run();

// ============================================================================
// DTO REQUEST / RESPONSE CONTRACTS
// ============================================================================

/// <summary>Request contract for chemical formula parsing and 3D geometry calculations.</summary>
public record FormulaRequest(string Formula = "Fe2(SO4)3*5H2O", string? Name = "Iron(III) Sulfate Pentahydrate", string? OverrideShape = null, bool? IsPlanar = false);

/// <summary>Request contract for SMILES parsing and functional group detection.</summary>
public record SmilesRequest(string Smiles = "CC(=O)O", string? Name = "Acetic Acid");

/// <summary>Request contract for stoichiometric chemical equation balancing.</summary>
public record ReactionRequest(string Equation = "CH4 + O2 -> CO2 + H2O");

/// <summary>Request contract for Hess's Law reaction thermodynamics calculations.</summary>
public record ThermoRequest(string Equation = "CH4 + 2O2 -> CO2 + 2H2O", double? TemperatureKelvin = 298.15);

/// <summary>Request contract for strong/weak acid pH calculations.</summary>
public record PhRequest(double ConcentrationMolar = 0.1, double? Ka = null);

/// <summary>Request contract for Henderson-Hasselbalch buffer pH calculations.</summary>
public record BufferRequest(double Pka = 4.76, double AcidConcentrationMolar = 0.1, double ConjugateBaseConcentrationMolar = 0.1);

/// <summary>Request contract for Nernst equation non-standard cell potential calculations.</summary>
public record NernstRequest(double StandardCellPotentialVolts = 1.10, int ElectronsTransferred = 2, double ReactionQuotientQ = 0.01, double? TemperatureKelvin = 298.15);

/// <summary>Request contract for Arrhenius activation energy and rate constant calculations.</summary>
public record ArrheniusRequest(double PreExponentialFactorA = 1e13, double ActivationEnergykJPerMol = 75.0, double TemperatureKelvin = 298.15);

/// <summary>Request contract for NMR and IR spectroscopy spectral predictions.</summary>
public record SpectroscopyRequest(string Formula = "CC(=O)Oc1ccccc1C(=O)O");

/// <summary>Request contract for UFF-inspired 3D coordinate energy minimization.</summary>
public record ForceFieldRequest(string Formula = "H2O", string? OverrideShape = null, int? MaxIterations = 500);

/// <summary>Request contract for Runge-Kutta 4th Order (RK4) multi-step reaction cascade kinetics.</summary>
public record NetworkKineticsRequest(double InitialConcA = 1.0, double K1 = 0.5, double K2 = 0.2, double TotalTime = 10.0, int Steps = 50);

/// <summary>Request contract for ADMET biophysical screening and Lipinski Rule of 5 audit.</summary>
public record AdmetRequest(string Formula = "CC(=O)Oc1ccccc1C(=O)O");

/// <summary>Request contract for autonomous generative de novo molecular optimization.</summary>
public record EvolutionRequest(string Input = "CC(=O)Oc1ccccc1C(=O)O", int? Generations = 50);

/// <summary>Request contract for EcoClean PFAS and microplastic catalytic mineralization.</summary>
public record EcoCleanRequest(string Pollutant = "PFOA C8HF15O2");

/// <summary>Request contract for Hückel Molecular Orbital (HMO) quantum electronic structure calculations.</summary>
public record HuckelRequest(string Formula = "c1ccccc1", double? BetaEv = 2.71);

public partial class Program
{
    private static bool TryParseChemicalInput(string input, string? name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Molecule? molecule)
    {
        molecule = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string trimmed = input.Trim();

        // 0. Check Curated Compound Registry for canonical names / empirical formulas (e.g. ATP, Glucose, PFOA, PFOS, C10H16N5O13P3, C6H12O6)
        if (Chemy.Core.Structure.CompoundRegistry.TryResolve(trimmed, out var regName, out var regSmiles))
        {
            if (Molecule.TryParseSmiles(regSmiles, name ?? regName, out var regMol) && regMol.Bonds.Count > 0)
            {
                molecule = regMol;
                return true;
            }
        }

        // 1. If it parses as SMILES with bonding topology, prioritize SMILES for organic compounds (e.g. Ibuprofen, Aspirin, Ethanol)
        if (Molecule.TryParseSmiles(trimmed, name, out var smilesMol) && smilesMol.Bonds.Count > 0)
        {
            // Only prefer empirical formula parser if it is a pure stoichiometry without SMILES syntax (e.g. C6H12O6, H2O)
            bool hasSmilesChars = trimmed.Any(ch => char.IsLower(ch) || ch is '(' or ')' or '=' or '#' or ':');
            if (!hasSmilesChars && Molecule.TryParse(trimmed, name, out var formulaMol) && formulaMol.Atoms.Count > smilesMol.Atoms.Count)
            {
                molecule = formulaMol;
                return true;
            }

            molecule = smilesMol;
            return true;
        }

        // 2. Try standard chemical formula (e.g. H2O, C6H12O6, Fe2O3, KMnO4, H2SO4)
        if (Molecule.TryParse(trimmed, name, out molecule))
        {
            return true;
        }

        // 3. Fallback to token extraction (e.g. "PFOA C8HF15O2")
        var parts = trimmed.Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (Chemy.Core.Structure.CompoundRegistry.TryResolve(part, out var pName, out var pSmiles) && Molecule.TryParseSmiles(pSmiles, name ?? pName, out molecule)) return true;
            if (Molecule.TryParseSmiles(part, name ?? part, out molecule) && molecule.Bonds.Count > 0) return true;
            if (Molecule.TryParse(part, name ?? part, out molecule)) return true;
        }

        return false;
    }
}

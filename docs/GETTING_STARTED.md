# Getting Started with Chemy

[Documentation home](README.md) · [Cookbook](COOKBOOK.md) · [API reference](API_REFERENCE.md) · [Architecture](ARCHITECTURE.md)

This guide takes a new contributor from checkout to a running library, API, or web workstation.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- Python 3 only for repository verification scripts; normal `Chemy.Core` execution does not require Python or RDKit

Confirm the SDK:

```bash
dotnet --version
```

## Restore, build, and test

From the repository root:

```bash
dotnet restore src/Chemy.slnx
dotnet build src/Chemy.slnx --configuration Release -warnaserror
dotnet test src/Chemy.slnx --configuration Release --no-build
```

The solution contains the core library, REST API, web workstation, and test project.

## Use `Chemy.Core` from another project

Until Chemy is distributed as a package, add a project reference:

```bash
dotnet add path/to/YourProject.csproj reference src/Chemy.Core/Chemy.Core.csproj
```

Then parse a bonded structure and calculate a physicochemical profile:

```csharp
using Chemy.Core;
using Chemy.Core.Pharmacology;

var molecule = Molecule.FromSmiles("CCO", "Ethanol");
var profile = AdmetEngine.Analyze(molecule);

Console.WriteLine(molecule.ChemicalFormula);
Console.WriteLine($"Molecular weight: {profile.MolecularWeight:F3} g/mol");
Console.WriteLine($"TPSA: {profile.TpsaAngstrom2:F2} Å²");
Console.WriteLine($"Applicability: {profile.Applicability.Status}");
```

Chemy distinguishes empirical formulas from bonded structures. Use `Molecule.Parse("C2H6O")` when composition alone is sufficient and `Molecule.FromSmiles("CCO")` when topology-dependent calculations are required.

## Run the REST API

```bash
dotnet run --project src/Chemy.Api
```

The default Development profile listens on `http://localhost:5192`:

- Scalar: `http://localhost:5192/scalar/v1`
- Swagger UI: `http://localhost:5192/swagger`
- OpenAPI JSON: `http://localhost:5192/openapi/v1.json`
- Health: `http://localhost:5192/healthz`

Development mode does not require an API key. Non-Development environments require `ApiSecurity__ApiKey` by default and hide interactive documentation unless `ApiSecurity__ExposeDocumentation=true` is configured deliberately. See the [API Reference](API_REFERENCE.md#system-health--observability) for runtime settings.

## Run the web workstation

```bash
dotnet run --project src/Chemy.Web
```

The default Development profile opens `http://localhost:5045`. The workstation provides molecular visualization and interactive access to the calculation suite.

## Understand scientific result contracts

Before consuming approximate results in an automated workflow, inspect the result metadata:

- `MethodInfo` identifies the method, version, evidence level, references, and warnings.
- `Applicability` reports `InDomain`, `Boundary`, or `OutOfDomain` with reasons.
- `Uncertainty` reports a calibrated reference-agreement envelope where one exists.
- `Diagnostics` reports convergence, residual, step, or conservation information for numerical solvers.

Unsupported predictive inputs normally throw rather than return an unjustified value. Boundary results should be reviewed explicitly by the calling workflow.

## Next steps

- Use the [Cookbook](COOKBOOK.md) for task-oriented C# examples.
- Use the [API Reference](API_REFERENCE.md) for types, routes, and payloads.
- Use the [Scientific Approach](SCIENTIFIC_APPROACH.md) for equations and applicability assumptions.
- Use the [Scientific Credibility Report](SCIENTIFIC_CREDIBILITY_REPORT.md) before relying on a model's evidence level.

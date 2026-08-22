# Chemy

<div align="center">

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Chemy CI](https://github.com/mouralx/chemy/actions/workflows/ci.yml/badge.svg)](https://github.com/mouralx/chemy/actions/workflows/ci.yml)
![Coverage Gate](https://img.shields.io/badge/Coverage-%E2%89%A580%25%20line%20%7C%20%E2%89%A570%25%20branch-blue)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Pure C# computational chemistry, chemoinformatics, and molecular-analysis components for .NET 10.

[Get started](docs/GETTING_STARTED.md) · [Documentation](docs/README.md) · [API reference](docs/API_REFERENCE.md) · [Scientific evidence](docs/SCIENTIFIC_CREDIBILITY_REPORT.md) · [Latest audit](docs/CODEX_AUDIT_v2.8.md)

</div>

## What Chemy provides

Chemy combines a reusable domain library, an HTTP API, and a browser-based molecular workstation. Its scientific outputs are separated into exact equations, numerical approximations, calibrated empirical models, and explicitly qualitative heuristics.

- **Chemical foundations:** formula and bounded SMILES parsing, molecular graphs, ring perception, subgraph matching, exact mass/charge reaction balancing, and common chemical file formats.
- **Physical models:** a UFF-compatible organic molecular-mechanics subset, 3D coordinate generation, Hückel molecular orbitals, NIST Shomate thermodynamics, electrochemistry, acid/base equilibria, and RK4 reaction networks.
- **Chemoinformatics:** TPSA, LogP, QED, hydrogen bonding, rotatable bonds, physicochemical filters, spectroscopy estimates, and rule-based molecular exploration.
- **Interfaces:** `Chemy.Core`, the `Chemy.Api` REST service with OpenAPI/Scalar, and the `Chemy.Web` interactive workstation.

Approximate and heuristic results expose applicability, evidence, uncertainty, or numerical diagnostics as appropriate. Unsupported scientific inputs fail closed where the implementation cannot justify a result.

## Start in five minutes

Prerequisite: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/mouralx/chemy.git
cd chemy
dotnet restore src/Chemy.slnx
dotnet build src/Chemy.slnx --configuration Release
dotnet test src/Chemy.slnx --configuration Release --no-build
```

Run the REST API in Development:

```bash
dotnet run --project src/Chemy.Api
```

Open `http://localhost:5192/scalar/v1` for the interactive API reference or `http://localhost:5192/healthz` for the health probe.

Run the web workstation:

```bash
dotnet run --project src/Chemy.Web
```

Open `http://localhost:5045`.

For project references, configuration, and the first library example, continue with [Getting Started](docs/GETTING_STARTED.md). For focused C# examples, use the [Cookbook](docs/COOKBOOK.md).

## Minimal library example

```csharp
using Chemy.Core;
using Chemy.Core.Pharmacology;

var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var profile = AdmetEngine.Analyze(aspirin);

Console.WriteLine($"Formula: {aspirin.ChemicalFormula}");
Console.WriteLine($"TPSA: {profile.TpsaAngstrom2:F2} Å²");
Console.WriteLine($"LogP: {profile.CalculatedLogP:F2}");
Console.WriteLine($"Applicability: {profile.Applicability.Status}");
```

## Choose the right document

| If you want to… | Start here |
| :--- | :--- |
| Install, build, and run Chemy | [Getting Started](docs/GETTING_STARTED.md) |
| Copy focused C# examples | [Cookbook](docs/COOKBOOK.md) |
| Find a C# type or HTTP endpoint | [API Reference](docs/API_REFERENCE.md) |
| Understand components and data flow | [Architecture](docs/ARCHITECTURE.md) |
| Understand equations and model boundaries | [Scientific Approach](docs/SCIENTIFIC_APPROACH.md) |
| Evaluate current scientific credibility | [Scientific Credibility Report](docs/SCIENTIFIC_CREDIBILITY_REPORT.md) |
| Inspect benchmark design and results | [Verification and Benchmarks](docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md) |
| Review the current score and remaining work | [Scientific Audit v2.8](docs/CODEX_AUDIT_v2.8.md) |
| Browse every document and historical audit | [Documentation Home](docs/README.md) |

## Projects

```text
src/
├── Chemy.Core/          Scientific models, parsers, graph algorithms, and I/O
├── Chemy.Api/           ASP.NET Core REST API and interactive OpenAPI reference
├── Chemy.Web/           Razor-based molecular workstation
└── Chemy.Core.Tests/    Unit, contract, benchmark, and interoperability tests
```

The core library has no native scientific runtime dependency. External tools such as RDKit are used to generate and verify pinned reference artifacts, not to execute normal `Chemy.Core` calculations.

## Scientific status

The v2.8 audit assigns **9.7/10 internal scientific implementation readiness** within the declared applicability domains. The remaining 0.3 is reserved for prospective evaluation, independent reproduction, and identifiable chemistry-domain review; it is not represented as completed certification.

The evidence chain is intentionally split by purpose:

1. [Scientific Approach](docs/SCIENTIFIC_APPROACH.md) — implemented methods and equations.
2. [Verification and Benchmarks](docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md) — executable comparisons and datasets.
3. [Scientific Credibility Report](docs/SCIENTIFIC_CREDIBILITY_REPORT.md) — current capability and boundary summary.
4. [Scientific Audit v2.8](docs/CODEX_AUDIT_v2.8.md) and [acceptance manifest](docs/SCIENTIFIC_ACCEPTANCE_v2.8.json) — score, gates, and machine-readable evidence.

## Visual interfaces

<details>
<summary>View the web workstation and API explorers</summary>

### Molecular workstation

![Chemy 3D molecular workstation](docs/images/3d_workstation_nicotine.png)

### Scalar REST API explorer

![Chemy Scalar REST API explorer](docs/images/scalar_api_reference.png)

### C# type explorer

![Chemy C# reflection API explorer](docs/images/csharp_reflection_api_explorer.png)

</details>

## Quality gates

The v2.8 audited state has 171 passing tests, zero Release compiler warnings, 85.47% line coverage, and 76.42% branch coverage. CI enforces minimum floors of 80% line and 70% branch coverage.

```bash
dotnet build src/Chemy.slnx --configuration Release -warnaserror
dotnet test src/Chemy.slnx --configuration Release --no-build
python3 scripts/verify_claim_consistency.py
```

## License and citation

Chemy is available under the [MIT License](LICENSE). Citation metadata is provided in [CITATION.cff](CITATION.cff).

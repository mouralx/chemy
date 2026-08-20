# 🧪 Chemy — Computational Chemistry Engine & REST API

<div align="center">

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-64%20Passed%20(100%25)-brightgreen?logo=xunit)
![Zero Warnings](https://img.shields.io/badge/Compiler-0%20Warnings-success)
![License](https://img.shields.io/badge/License-MIT-blue.svg)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20Microservice-orange)

**Industrial-grade, zero-dependency computational chemistry, chemoinformatics, and lead optimization toolkit for .NET.**  
*From exact equation balancing and 4-term molecular mechanics force fields to topological graph rewrites, Ertl TPSA, and MDL Molfile V2000 export.*

[✨ Key Features](#-key-features) • [🚀 Quick Start](#-quick-start) • [🧬 Societal Breakthroughs](#-societal-breakthrough-engines) • [🏗️ Architecture](#-project-structure) • [📖 Documentation](#-documentation)

</div>

---

## 💡 What is Chemy?

**Chemy** is a modern, high-performance chemistry computational platform built for developers, scientists, students, and researchers. 

All algorithms in Chemy are **pure, deterministic C# implementations** without external Python, cloud AI, or black-box dependencies:
- 🌿 **Chemical Graph Theory & Subgraph Isomorphism**: Implements VF2-style subgraph matching (`SubgraphMatcher`) and topological graph rewriting (`GraphRewriter`) on immutable molecular graphs (`ChemicalGraph`).
- ⚛️ **Multi-Term Molecular Mechanics Force Field**: 4-term analytical potential (Bond Stretching, Angle Bending, Dihedral Torsions, 12-6 Lennard-Jones van der Waals) with conjugate gradient geometric relaxation (`ForceFieldEngine`).
- 🛡️ **Standard ADMET & Drug Safety Screening**: Exact Ertl Topological Polar Surface Area (TPSA), Wildman-Crippen $\log P$, Lipinski Rule of 5, Veber Oral Bioavailability rules, and Ghose drug-likeness filters (`AdmetEngine`).
- 🧬 **Bioisosteric Lead Optimization**: Generates optimized lead candidates via topological graph substitutions (1H-tetrazole cycles, para-fluorine shielding, morpholines, deuterium KIE).
- 📁 **Standard Chemical File Formats**: Full support for MDL Molfile (V2000), Structure-Data Files (SDF), Protein Data Bank (PDB), and XYZ formats (`MolfileExporter`).
- ⚖️ **Exact Reaction Balancer**: Solves chemical equations using exact rational Gaussian elimination nullspace matrix algebra ($M\vec{x} = \vec{0}$).
- ♻️ **EcoClean PFAS & Plastic Solver**: Calculates Bond Dissociation Energies ($\text{BDE}$) and constructs catalytic mineralization cascades for persistent pollutants.
- 📉 **NMR & IR Spectroscopy Predictor**: Predicts $^1\text{H}$-NMR, $^{13}\text{C}$-NMR, and IR vibrational absorption bands across 20+ organic functional group classes.
- ⏱️ **4th-Order Runge-Kutta (RK4) Kinetics**: Integrates multi-step consecutive reaction cascades ($A \xrightarrow{k_1} B \xrightarrow{k_2} C$).
- 🌐 **NCBI PubChem Integrator**: Live REST query client for 110M+ real compounds.

---

## 🌟 Key Features at a Glance

| Feature | What It Does (In Plain English) | Exact Scientific Implementation |
| :--- | :--- | :--- |
| **🌿 Graph Substructure Matcher** | Finds specific chemical motifs (acids, esters, rings) inside any molecular graph. | Topological graph isomorphism pattern matching with adjacency index tables (`SubgraphMatcher`). |
| **⚛️ Molecular Mechanics (UFF/MMFF)** | Relaxes atoms in 3D Euclidean space to relieve steric strain. | 4-term analytical potential ($E_{\text{bond}} + E_{\text{angle}} + E_{\text{torsion}} + E_{\text{vdw}}$) solved via gradient descent (`ForceFieldEngine`). |
| **🛡️ Ertl TPSA & ADMET Shield** | Evaluates whether a molecule can safely act as an oral medicine. | Standard Ertl atomic polar surface area fragments, Wildman-Crippen $\log P$, Veber rules, and Ghose filters (`AdmetEngine`). |
| **🧬 Bioisosteric Graph Evolver** | Mutates a baseline compound to produce optimized, less toxic lead candidates. | Topological graph rewriting replacing target motifs with bioisosteric rings (`MolecularEvolverEngine`). |
| **📁 MDL Molfile & SDF Exporter** | Exports molecules to standard formats for ChemDraw, PyMOL, and RDKit. | ISO/IUPAC-compliant MDL Molfile V2000 and multi-record SDF serializer (`MolfileExporter`). |
| **⚖️ Smart Reaction Balancer** | Instantly balances chemical equations with zero rounding errors. | Exact rational Gaussian elimination nullspace reduction ($M\vec{x} = \vec{0}$) over $\mathbb{Q}$ with LCM integer scaling. |
| **📐 3D VSEPR Molecule Builder** | Converts formulas and SMILES codes into accurate 3D atomic coordinates. | Valence Shell Electron Pair Repulsion (VSEPR) steric coordination algorithms and Cartesian geometry generators. |
| **📉 NMR & IR Spectroscopy** | Predicts chemical shifts and Infrared absorption frequencies. | Empirical functional group shielding tables and Hooke's Law harmonic oscillator frequencies. |
| **⚛️ 118-Element Periodic Table** | Instant lookup for all 118 IUPAC elements. | $O(1)$ constant-time lookup backed by .NET `FrozenDictionary`. |
| **🔥 Thermodynamics & Feasibility** | Predicts if a reaction is exothermic ($\Delta H$) or spontaneous ($\Delta G$). | Hess's Law using standard thermodynamic tables with Benson Group Additivity fallback for unknown molecules. |
| **⏱️ Reaction Kinetics & RK4** | Simulates multi-step reaction concentrations over time. | 4th-Order Runge-Kutta (RK4) numerical ODE solver. |
| **🌐 NCBI PubChem Cloud Query** | Live searches the global PubChem database. | Resilient typed `HttpClient` querying the official NCBI REST PUG API. |

---

## 🚀 Quick Start (C# Code)

### 1. Topological Graph Pattern Matching & Rewriting

```csharp
using Chemy.Core;
using Chemy.Core.Graph;

// Build molecular graph from Aspirin
var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var graph = ChemicalGraph.FromMolecule(aspirin);

// Detect carboxylic acid motif and replace with 1H-tetrazole ring
var matches = SubgraphMatcher.FindMatches(graph, SubgraphMatcher.CarboxylicAcidQuery);
Console.WriteLine($"Found {matches.Count} carboxyl motif(s)");

var tetrazoleLead = GraphRewriter.ReplaceCarboxylWithTetrazole(aspirin);
Console.WriteLine($"New Lead Formula: {tetrazoleLead.ChemicalFormula}");
```

### 2. Multi-Term Molecular Mechanics Energy Minimization

```csharp
using Chemy.Core;
using Chemy.Core.Physics;

// Relax 3D Cartesian coordinates using 4-term force field
var water3D = Molecule.Water.To3D();
var result = ForceFieldEngine.MinimizeEnergy(water3D, maxIterations: 50);

Console.WriteLine($"Initial Energy: {result.InitialEnergyKcalPerMol} kcal/mol");
Console.WriteLine($"Relaxed Energy: {result.FinalEnergyKcalPerMol} kcal/mol (Converged: {result.Converged})");
```

### 3. Screen Drug Safety (Ertl TPSA, Veber & Ghose Rules)

```csharp
using Chemy.Core;
using Chemy.Core.Pharmacology;

var molecule = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var admet = AdmetEngine.Analyze(molecule);

Console.WriteLine($"Ertl TPSA: {admet.TpsaAngstrom2} Å² (Veber Limit: <= 140 Å²)");
Console.WriteLine($"Passes Lipinski Rule of 5: {admet.PassesLipinskiRuleOf5}");
Console.WriteLine($"Passes Veber Oral Bioavailability: {admet.PassesVeberRules}");
Console.WriteLine($"Passes Ghose Filter: {admet.PassesGhoseFilter}");
```

### 4. Export to Standard MDL Molfile (V2000)

```csharp
using Chemy.Core;
using Chemy.Core.IO;

var aspirin3D = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin").To3D();
string molfileV2000 = MolfileExporter.ToMolfileV2000(aspirin3D);

Console.WriteLine(molfileV2000);
```

---

## 🏗️ Project Structure

The codebase is organized cleanly following enterprise .NET architecture:

```text
chemy/
├── docs/                        # Comprehensive technical documentation
│   ├── API_REFERENCE.md         # Exhaustive C# and REST API guide
│   ├── ARCHITECTURE.md          # Mathematics, linear algebra, and diagrams
│   ├── BREAKTHROUGHS_SHOWCASE.md # Aspirin, Cocaine, and PFOA case studies
│   ├── GETTING_STARTED.md       # Step-by-step developer tutorial
│   └── SCIENTIFIC_APPROACH.md   # Physical chemistry and computational principles
├── src/                         # All project source code
│   ├── Chemy.slnx               # Modern solution file
│   ├── Directory.Build.props    # Global zero-warning compiler rules
│   ├── Chemy.Core/              # Pure computational chemistry library
│   │   ├── Graph/               # ChemicalGraph, SubgraphMatcher, GraphRewriter
│   │   ├── Physics/             # Multi-term ForceFieldEngine (MMFF/UFF)
│   │   ├── Pharmacology/        # Ertl TPSA, Crippen LogP, Veber/Ghose rules
│   │   ├── IO/                  # MDL Molfile V2000 & SDF serializers
│   │   └── ...                  # Reactions, Kinetics, Solutions, Thermodynamics
│   ├── Chemy.Api/               # Pure REST API microservice (Scalar & Swagger)
│   ├── Chemy.Web/               # Interactive 3D laboratory workstation
│   └── Chemy.Core.Tests/        # Complete xUnit test suite (64 tests)
└── README.md                    # Project overview
```

---

## 🌐 Running the REST API Microservice

Chemy includes a lightweight, ultra-fast **REST API microservice** (`Chemy.Api`) with built-in interactive documentation:

```bash
dotnet run --project src/Chemy.Api
```

Once running, open your browser:
* **Interactive Scalar UI**: [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1) *(or automatically at `/`)*
* **Swagger UI**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
* **Health Probe**: [http://localhost:5000/healthz](http://localhost:5000/healthz)

---

## 🧪 Running the 3D Web Workstation

To launch the full visual 3D laboratory workstation:

```bash
dotnet run --project src/Chemy.Web
```

Visit `http://localhost:5002` to explore rotatable 3D molecular structures, interactive reaction balancers, and spectroscopy charts.

---

## 🛡️ Testing & Quality

Chemy is built to the highest enterprise standards:
- **100% Passing Tests**: 64/64 unit tests in `Chemy.Core.Tests`.
- **Zero Warnings**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` enforced across all projects.
- **Zero Allocations**: High-frequency element and bond structs allocated on the stack.

```bash
dotnet test src/Chemy.slnx
```

```text
Passed! - Failed: 0, Passed: 64, Skipped: 0, Total: 64 (Duration: 35 ms)
```

---

<div align="center">
Built with ❤️ for science, education, and humanity.
</div>

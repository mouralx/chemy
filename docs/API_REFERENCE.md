# Chemy API Reference Manual

Welcome to the definitive **Chemy API Reference**. This document provides an exhaustive specification for both the **`Chemy.Core` C# Class Library** and the **`Chemy.Api` HTTP REST API Microservice**.

---

## 📑 Table of Contents

1. [C# Domain Class Reference (`Chemy.Core`)](#1-c-domain-class-reference-chemycore)
   - [Chemical Graph Theory & Subgraph Isomorphism](#chemicalgraph--subgraphmatcher--graphrewriter)
   - [Periodic Table & Elements](#element--elements)
   - [Molecules & SMILES](#molecule--smilesparser)
   - [3D Geometry & VSEPR](#geometry3dengine--molecule3d)
   - [Multi-Term Molecular Mechanics Force Field](#forcefieldengine)
   - [Standard ADMET, Ertl TPSA & Veber Rules](#admetengine)
   - [MDL Molfile (V2000) & SDF Exporter](#molfileexporter)
   - [Autonomous Bioisosteric Molecular Evolver](#molecularevolverengine)
   - [EcoClean Biocleavage Solver](#ecocleanengine)
   - [NMR & IR Spectroscopy](#spectroscopyengine)
   - [PubChem Live Cloud Integrator](#pubchemclient)
   - [Kinetics & RK4 Networks](#reactionnetworkengine--kineticsengine)
   - [Stoichiometry & Reaction Balancer](#reaction--stepbystepbalancer)
   - [Thermodynamics & Benson Additivity](#reactionthermodynamics)
   - [Solutions & Electrochemistry](#solutionsengine--electrochemistryengine)
2. [HTTP REST API Endpoints (`Chemy.Api`)](#2-http-rest-api-endpoints-chemyapi)
   - [Service Health & Observability](#system-health--observability)
   - [Periodic Table Endpoints](#periodic-table-endpoints)
   - [Molecular Structure & 3D Endpoints](#molecular-structure--3d-endpoints)
   - [Autonomous Molecular Evolution Endpoints](#molecular-evolution--lead-optimization-endpoints)
   - [Pharmacology & ADMET Endpoints](#pharmacology--admet-endpoints)
   - [Environmental EcoClean Endpoints](#environmental--ecoclean-endpoints)
   - [Spectroscopy & Force Field Endpoints](#spectroscopy--physics-endpoints)
   - [PubChem Cloud Query Endpoints](#pubchem-cloud-endpoints)
   - [Kinetics & Reaction Network Endpoints](#chemical-kinetics-endpoints)
   - [Stoichiometry & Balancer Endpoints](#stoichiometry--reactions-endpoints)
   - [Solutions & Electrochemistry Endpoints](#solutions--electrochemistry-endpoints)

---

## 1. C# Domain Class Reference (`Chemy.Core`)

### `ChemicalGraph`, `SubgraphMatcher` & `GraphRewriter`
Topological molecular graph representation with adjacency lists, DFS ring detection, VF2 subgraph isomorphism pattern matching, and atomic graph rewriting.

```csharp
namespace Chemy.Core.Graph;

public class ChemicalGraph
{
    public IReadOnlyList<GraphNode> Nodes { get; }
    public IReadOnlyList<GraphEdge> Edges { get; }
    public static ChemicalGraph FromMolecule(Molecule molecule);
    public IReadOnlyList<GraphEdge> GetIncidentEdges(int nodeId);
    public IReadOnlyList<int> GetNeighbors(int nodeId);
    public IReadOnlyList<IReadOnlyList<int>> FindRings();
}

public static class SubgraphMatcher
{
    public static readonly SubgraphQuery CarboxylicAcidQuery;
    public static readonly SubgraphQuery EsterQuery;
    public static readonly SubgraphQuery AmideQuery;
    public static readonly SubgraphQuery KetoneQuery;
    public static IReadOnlyList<IReadOnlyDictionary<int, int>> FindMatches(ChemicalGraph graph, SubgraphQuery query);
}

public static class GraphRewriter
{
    public static Molecule ToMolecule(ChemicalGraph graph, string name = "Derivative");
    public static Molecule ReplaceCarboxylWithTetrazole(Molecule source);
    public static Molecule AppendFluorineShield(Molecule source);
}
```

---

### `ForceFieldEngine`
Multi-term Molecular Mechanics Force Field:
$$E_{\text{total}} = E_{\text{bond}} + E_{\text{angle}} + E_{\text{torsion}} + E_{\text{vdw}}$$
Solved via gradient descent with adaptive step size acceleration.

```csharp
namespace Chemy.Core.Physics;

public static class ForceFieldEngine
{
    public static EnergyMinimizationResult MinimizeEnergy(Molecule3D molecule3D, int maxIterations = 50);
    public static double CalculateTotalEnergy(Molecule3D molecule3D);
}
```

---

### `AdmetEngine`
Calculates Ertl Topological Polar Surface Area ($\text{TPSA}$ in $\text{\AA}^2$), Crippen $\log P$, Lipinski Rule of 5 violations, Veber Oral Bioavailability rules, Ghose drug-likeness filter, and Bickerton QED score.

```csharp
namespace Chemy.Core.Pharmacology;

public static class AdmetEngine
{
    public static AdmetProfile Analyze(Molecule molecule);
    public static double CalculateErtlTpsa(Molecule molecule);
    public static double CalculateCrippenLogP(Molecule molecule);
}
```

---

### `MolfileExporter`
Serializes 3D molecular structures to industry-standard ISO/IUPAC MDL Molfile (V2000) and Structure-Data File (SDF) formats.

```csharp
namespace Chemy.Core.IO;

public static class MolfileExporter
{
    public static string ToMolfileV2000(Molecule3D molecule3D);
    public static string ToSdf(IEnumerable<Molecule3D> molecules);
}
```

---

### `Element` & `Elements`
Provides $O(1)$ constant-time lookup for all 118 IUPAC elements backed by .NET `FrozenDictionary`.

```csharp
namespace Chemy.Core;

public readonly record struct Element(int AtomicNumber, string Symbol, string Name, double StandardAtomicMass);

public static class Elements
{
    public static IReadOnlyList<Element> All { get; }
    public static Element GetBySymbol(string symbol);
    public static Element GetByAtomicNumber(int atomicNumber);
    public static bool TryGetBySymbol(string symbol, out Element element);
    public static bool TryGetByAtomicNumber(int atomicNumber, out Element element);
}
```

---

### `Molecule` & `SmilesParser`
Represents an immutable chemical compound with explicit atom graph topology, molecular weight, charge, and functional groups.

```csharp
namespace Chemy.Core;

public record Molecule(string Name, IReadOnlyList<Atom> Atoms, IReadOnlyList<Bond> Bonds, double MolecularWeight, int NetCharge)
{
    public string ChemicalFormula { get; }
    public static Molecule Parse(string formula, string? name = null);
    public static bool TryParse(string formula, string? name, out Molecule molecule, out string? error);
    public static Molecule FromSmiles(string smiles, string? name = null);
    public static bool TryParseSmiles(string smiles, out Molecule? molecule);
    public IReadOnlyList<FunctionalGroup> GetFunctionalGroups();
    public Molecule3D To3D(string? overrideShape = null);
    public string ToSvg(bool isDarkMode = true);
    public void SaveSvg(string filePath, bool isDarkMode = true);
}
```

---

### `MolecularEvolverEngine`
Autonomous multi-objective optimization using topological graph rewrites (tetrazole bioisosteres, fluorine metabolic shielding, azetidines, morpholines, deuterium KIE).

```csharp
namespace Chemy.Core.Evolution;

public static class MolecularEvolverEngine
{
    public static EvolutionOptimizationResult EvolveLeadCandidate(string input, int generations = 50);
}
```

---

### `EcoCleanEngine`
Calculates bond dissociation energies ($\text{BDE}$) and generates step-by-step catalytic and enzymatic mineralization cascades for PFAS and microplastics.

```csharp
namespace Chemy.Core.Environmental;

public static class EcoCleanEngine
{
    public static EcoCleanDegradationResult SolveDegradationCascade(string input);
}
```

---

### `SpectroscopyEngine`
Predicts $^1\text{H}$-NMR chemical shifts ($\delta$ ppm), peak multiplets ($N+1$ coupling rule), $^{13}\text{C}$-NMR shifts, and Infrared (IR) absorption spectrum bands across 20+ functional groups.

```csharp
namespace Chemy.Core.Spectroscopy;

public static class SpectroscopyEngine
{
    public static SpectroscopyPrediction Predict(Molecule molecule);
}
```

---

### `ReactionNetworkEngine`
Runge-Kutta 4th Order (RK4) numerical integrator for multi-step consecutive reaction cascades ($A \xrightarrow{k_1} B \xrightarrow{k_2} C$).

```csharp
namespace Chemy.Core.Kinetics;

public static class ReactionNetworkEngine
{
    public static ReactionNetworkSimulationResult SimulateConsecutiveCascade(
        double initialConcA = 1.0,
        double k1 = 0.5,
        double k2 = 0.2,
        double totalTime = 10.0,
        int steps = 50
    );
}
```

---

## 2. HTTP REST API Endpoints (`Chemy.Api`)

Base URL: `http://localhost:5000` (or `https://localhost:5001`)

### System Health & Observability

#### `GET /healthz`
Returns service liveness and readiness probe status.
* **Tags**: `System Health`
* **Response (200 OK)**:
```json
{
  "status": "Healthy",
  "timestamp": "2026-08-20T00:00:00.0000000Z"
}
```

---

### Molecular Evolution & Lead Optimization Endpoints

#### `POST /api/v1/evolution/evolve`
Autonomously evolves 5 optimized drug candidates from a baseline molecule using bioisosteric transformations.
* **Tags**: `Molecular Evolution & Lead Optimization`
* **Request Payload**:
```json
{
  "input": "CC(=O)Oc1ccccc1C(=O)O",
  "generations": 50
}
```
* **Response (200 OK)**:
```json
{
  "baselineMolecule": "C9H8O4",
  "baselineSmiles": "CC(=O)Oc1ccccc1C(=O)O",
  "baselineQed": 0.704,
  "generationsRun": 50,
  "candidates": [
    {
      "candidateName": "Candidate Alpha (Tetrazole Bioisostere)",
      "smiles": "CC(=O)Oc1ccccc1c1nnn[nH]1",
      "chemicalFormula": "C10H9N4O2",
      "molecularWeight": 204.16,
      "qedScore": 0.884,
      "calculatedLogP": 0.95,
      "rationale": "Replaced metabolic liability (-COOH) with non-classical 1H-tetrazole 5-membered aromatic ring.",
      "toxicityImprovement": "Eliminates reactive acyl-glucuronide hepatotoxicity and extends metabolic half-life."
    }
  ]
}
```

---

### Pharmacology & ADMET Endpoints

#### `POST /api/v1/pharmacology/admet`
Screens Ertl TPSA, Wildman-Crippen LogP, Lipinski Rule of 5, Veber rules, Ghose filter, and QED score.
* **Tags**: `Pharmacology & ADMET`
* **Request Payload**:
```json
{
  "formula": "CC(=O)Oc1ccccc1C(=O)O"
}
```
* **Response (200 OK)**:
```json
{
  "formula": "C9H8O4",
  "molecularWeight": 180.16,
  "calculatedLogP": 1.31,
  "tpsaAngstrom2": 63.6,
  "hydrogenBondDonors": 1,
  "hydrogenBondAcceptors": 4,
  "rotatableBonds": 3,
  "aromaticRings": 1,
  "lipinskiViolations": 0,
  "passesLipinskiRuleOf5": true,
  "passesVeberRules": true,
  "passesGhoseFilter": true,
  "qedDrugLikenessScore": 0.534,
  "methodInfo": {
    "methodName": "Chemy Comprehensive Physicochemical & Drug-Likeness Profile",
    "evidenceLevel": "EmpiricalModel"
  }
}
```

---

### Environmental & EcoClean Endpoints

#### `POST /api/v1/environmental/ecoclean`
Calculates bond dissociation energies and generates step-by-step enzymatic/electrochemical mineralization cascades for PFAS and microplastics.
* **Tags**: `Environmental & EcoClean`
* **Request Payload**:
```json
{
  "pollutant": "PFOA C8HF15O2"
}
```

---

### Molecular Structure & 3D Endpoints

#### `POST /api/v1/geometry/3d`
Calculates 3D Cartesian coordinates, VSEPR shapes, and exports `.xyz` / `.pdb` formats.
* **Tags**: `Molecular Structure & 3D`
* **Request Payload**:
```json
{
  "formula": "CH4",
  "name": "Methane",
  "overrideShape": null
}
```

---

### Spectroscopy & Physics Endpoints

#### `POST /api/v1/spectroscopy/predict`
Predicts $^1\text{H}$-NMR peaks (ppm), $^{13}\text{C}$-NMR peaks, and IR absorption bands.
* **Tags**: `Spectroscopy`

#### `POST /api/v1/physics/minimize`
Relaxes 3D Cartesian coordinates using multi-term Molecular Mechanics force field minimization.
* **Tags**: `Physics & Force Field`

---

### PubChem Cloud Endpoints

#### `GET /api/v1/cloud/pubchem/{query}`
Live queries NCBI PubChem database (110M+ compounds) for CID, IUPAC Name, Formula, SMILES, and InChIKey.
* **Tags**: `PubChem Cloud`

---

### Chemical Kinetics Endpoints

#### `POST /api/v1/kinetics/network`
Simulates multi-step reaction cascades ($A \xrightarrow{k_1} B \xrightarrow{k_2} C$) via 4th-order Runge-Kutta numerical integration.
* **Tags**: `Chemical Kinetics`

#### `POST /api/v1/kinetics/arrhenius`
Calculates Arrhenius rate constant $k = A e^{-E_a/(RT)}$.
* **Tags**: `Chemical Kinetics`

---

### Stoichiometry & Reactions Endpoints

#### `POST /api/v1/reactions/balance`
Balances chemical reaction equations using exact rational Gaussian elimination nullspace algebra.
* **Tags**: `Stoichiometry & Reactions`

#### `POST /api/v1/reactions/explain`
Generates structured 5-step educational balancing explanations with Markdown.
* **Tags**: `Stoichiometry & Reactions`

#### `POST /api/v1/reactions/thermodynamics`
Calculates reaction Enthalpy ($\Delta H$), Entropy ($\Delta S$), and Gibbs Free Energy ($\Delta G$).
* **Tags**: `Stoichiometry & Reactions`

---

### Solutions & Electrochemistry Endpoints

#### `POST /api/v1/solutions/ph`
Calculates pH, pOH, $[\text{H}^+]$, and $[\text{OH}^-]$ for strong or weak acids.
* **Tags**: `Solutions & Acid-Base`

#### `POST /api/v1/solutions/buffer`
Solves the Henderson-Hasselbalch equation ($\text{pH} = \text{pK}_a + \log([\text{A}^-]/[\text{HA}])$).
* **Tags**: `Solutions & Acid-Base`

#### `POST /api/v1/electrochemistry/nernst`
Calculates non-standard electrochemical cell potential ($E_{\text{cell}}$) via the Nernst equation ($E = E^\circ - \frac{RT}{nF}\ln Q$).
* **Tags**: `Electrochemistry`

---

### Quantum & Molecular Orbitals Endpoints

#### `POST /api/v1/quantum/huckel`
Computes Hückel Molecular Orbital (HMO) electronic structure, HOMO and LUMO eigenvalues, bandgap $\Delta E$, estimated UV-Vis absorption maximum $\lambda_{\max}$, Dewar aromatic resonance stabilization energy, Coulson $\pi$-bond orders, and Fukui chemical reactivity indices.
* **Tags**: `Quantum & Molecular Orbitals`
* **Request Contract**:
  ```json
  {
    "formula": "c1ccccc1",
    "betaEv": 2.71
  }
  ```

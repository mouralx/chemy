# Chemy Cookbook

[Documentation home](README.md) · [Getting started](GETTING_STARTED.md) · [API reference](API_REFERENCE.md) · [Scientific approach](SCIENTIFIC_APPROACH.md)

These focused examples assume a project reference to `Chemy.Core`. They demonstrate API usage, not universal scientific validity; callers should inspect applicability, uncertainty, and numerical diagnostics.

## Contents

1. [Physicochemical descriptors](#physicochemical-descriptors)
2. [UFF-compatible energy minimization](#uff-compatible-energy-minimization)
3. [Graph matching and rewriting](#graph-matching-and-rewriting)
4. [3D coordinates and chemical files](#3d-coordinates-and-chemical-files)
5. [Spectroscopy estimation](#spectroscopy-estimation)
6. [Reaction-network kinetics](#reaction-network-kinetics)
7. [Solutions chemistry](#solutions-chemistry)
8. [Electrochemistry](#electrochemistry)
9. [Rule-based molecular exploration](#rule-based-molecular-exploration)
10. [Qualitative degradation pathways](#qualitative-degradation-pathways)

## Physicochemical descriptors

```csharp
using Chemy.Core;
using Chemy.Core.Pharmacology;

var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var profile = AdmetEngine.Analyze(aspirin);

Console.WriteLine($"MW: {profile.MolecularWeight:F3} g/mol");
Console.WriteLine($"LogP: {profile.CalculatedLogP:F2}");
Console.WriteLine($"TPSA: {profile.TpsaAngstrom2:F2} Å²");
Console.WriteLine($"QED: {profile.QedDrugLikenessScore:F3}");
Console.WriteLine($"Applicability: {profile.Applicability.Status}");
```

This is a physicochemical and drug-likeness profile, not an efficacy, toxicity, metabolism, or clinical prediction.

## UFF-compatible energy minimization

```csharp
using Chemy.Core;
using Chemy.Core.Physics;

var water = Molecule.Water.To3D();
var applicability = ForceFieldEngine.AssessApplicability(water.SourceMolecule);
var result = ForceFieldEngine.MinimizeEnergy(water, maxIterations: 500);

Console.WriteLine($"Domain: {applicability.Status}");
Console.WriteLine($"Energy: {result.InitialEnergyKcalPerMol:F4} -> {result.FinalEnergyKcalPerMol:F4} kcal/mol");
Console.WriteLine($"Converged: {result.Converged}; reason: {result.TerminationReason}");
Console.WriteLine($"Final gradient norm: {result.FinalGradientNorm:G5}");
```

The force field implements a declared UFF-compatible organic subset. Applicability is checked before unsupported atom types are evaluated.

## Graph matching and rewriting

```csharp
using Chemy.Core;
using Chemy.Core.Graph;

var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var graph = ChemicalGraph.FromMolecule(aspirin);
var matches = SubgraphMatcher.FindMatches(graph, SubgraphMatcher.CarboxylicAcidQuery);
var candidate = GraphRewriter.ReplaceCarboxylWithTetrazole(aspirin);

Console.WriteLine($"Carboxylic-acid matches: {matches.Count}");
Console.WriteLine($"Candidate formula: {candidate.ChemicalFormula}");
```

Graph rewrites generate structures for expert review; they do not establish synthesis feasibility or biological activity.

## 3D coordinates and chemical files

```csharp
using Chemy.Core;
using Chemy.Core.IO;

var molecule = Molecule.FromSmiles("CCO", "Ethanol");
var conformer = molecule.To3D();

string xyz = conformer.ToXyz();
string pdb = conformer.ToPdb();
string molfile = MolfileExporter.ToMolfileV2000(conformer);
```

Use `Geometry3DEngine.GenerateConformer3DResult` when the workflow needs convergence and iteration-budget evidence rather than coordinates alone.

## Spectroscopy estimation

```csharp
using Chemy.Core;
using Chemy.Core.Spectroscopy;

var acetone = Molecule.FromSmiles("CC(=O)C", "Acetone");
var spectrum = SpectroscopyEngine.Predict(acetone);

foreach (var peak in spectrum.H1NmrPeaks)
{
    Console.WriteLine($"δ {peak.ChemicalShiftPpm:F2} ppm — {peak.Description}");
}

Console.WriteLine($"Applicability: {spectrum.Applicability.Status}");
```

The 1H NMR model has narrow empirical calibration. The 13C NMR and IR outputs remain heuristic correlation estimates.

## Reaction-network kinetics

```csharp
using Chemy.Core.Kinetics;

var cascade = ReactionNetworkEngine.SimulateConsecutiveCascade(
    initialConcA: 1.0,
    k1: 0.5,
    k2: 0.2,
    totalTime: 10.0);

Console.WriteLine($"Analytical residual: {cascade.Diagnostics.MaximumResidual:G5}");
Console.WriteLine($"Conservation error: {cascade.Diagnostics.MaximumConservationError:G5}");
```

## Solutions chemistry

```csharp
using Chemy.Core.Solutions;

var acid = SolutionsEngine.CalculateWeakAcidPh(
    concentrationMolar: 0.1,
    ka: 1.8e-5);

var buffer = SolutionsEngine.CalculateBufferPh(
    pka: 4.76,
    acidConcentrationMolar: 0.1,
    conjugateBaseConcentrationMolar: 0.1);

Console.WriteLine($"Weak-acid pH: {acid.Ph:F3}; residual: {acid.Diagnostics.MaximumResidual:G5}");
Console.WriteLine($"Buffer pH: {buffer.Ph:F3}");
```

## Electrochemistry

```csharp
using Chemy.Core.Electrochemistry;

var result = ElectrochemistryEngine.CalculateNernstPotential(
    standardCellPotentialVolts: 1.10,
    electronsTransferred: 2,
    reactionQuotientQ: 0.01,
    temperatureKelvin: 298.15);

Console.WriteLine($"Cell potential: {result.CellPotentialVolts:F3} V");
Console.WriteLine($"Evidence: {result.MethodInfo.EvidenceLevel}");
```

## Rule-based molecular exploration

```csharp
using Chemy.Core.Evolution;

var exploration = MolecularEvolverEngine.EvolveLeadCandidate(
    "CC(=O)Oc1ccccc1C(=O)O",
    generations: 50);

foreach (var candidate in exploration.Candidates)
{
    Console.WriteLine($"{candidate.CandidateName}: QED={candidate.QedScore:F3}, LogP={candidate.CalculatedLogP:F2}");
}

Console.WriteLine(exploration.MethodInfo.Warnings[0]);
```

This engine ranks deterministic graph mutations by bounded descriptors. It does not predict potency, safety, metabolism, or synthesis feasibility.

## Qualitative degradation pathways

```csharp
using Chemy.Core.Environmental;

var pathway = EcoCleanEngine.SolveDegradationCascade("PFOA C8HF15O2");

Console.WriteLine($"Applicability: {pathway.Applicability.Status}");
foreach (var step in pathway.DegradationCascade)
{
    Console.WriteLine($"{step.StepNumber}: {step.TargetBond} via {step.EnzymeOrCatalyst}");
}
```

EcoClean produces a qualitative pathway hypothesis. It does not calculate degradation kinetics, yield, mineralization efficiency, or environmental safety.

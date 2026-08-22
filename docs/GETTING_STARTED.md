# Getting Started with Chemy

This guide walks you through setting up, configuring, and consuming the **Chemy** engine and REST API.

---

## 1. Autonomous De Novo Molecular Evolution

Evolve 5 optimized drug candidates from a baseline molecule:

```csharp
using Chemy.Core.Evolution;

// Evolve derivatives from baseline Aspirin to eliminate acyl glucuronide toxicity
var evolution = MolecularEvolverEngine.EvolveLeadCandidate("CC(=O)Oc1ccccc1C(=O)O", generations: 50);

Console.WriteLine($"Baseline: {evolution.BaselineMolecule} (QED {evolution.BaselineQed:F2})");
foreach (var lead in evolution.Candidates)
{
    Console.WriteLine($"-> {lead.CandidateName} | QED: {lead.QedScore:F2} | {lead.Rationale}");
}
```

---

## 2. Physicochemical & Lipinski Rule of 5 Screening

Screen biophysical properties and oral drug-likeness descriptors:

```csharp
using Chemy.Core;
using Chemy.Core.Pharmacology;

var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var admet = AdmetEngine.Analyze(aspirin);

Console.WriteLine($"MW: {admet.MolecularWeight} g/mol");
Console.WriteLine($"Calculated LogP: {admet.CalculatedLogP}");
Console.WriteLine($"TPSA: {admet.TpsaAngstrom2} Å²");
Console.WriteLine($"QED Score: {admet.QedDrugLikenessScore}");
Console.WriteLine($"Lipinski Violations: {admet.LipinskiViolations} (Passes: {admet.PassesLipinskiRuleOf5})");
```

---

## 3. EcoClean Degradation Pathway Solver

Solve catalytic degradation pathways for environmental pollutants:

```csharp
using Chemy.Core.Environmental;

var cascade = EcoCleanEngine.SolveDegradationCascade("PFOA C8HF15O2");

Console.WriteLine($"Pollutant: {cascade.PollutantClass}");
Console.WriteLine($"Theoretical Products: {cascade.TheoreticalMineralizationProducts}");

foreach (var step in cascade.DegradationCascade)
{
    Console.WriteLine($"Step {step.StepNumber}: {step.TargetBond} (BDE: {step.BondDissociationEnergyKcalPerMol} kcal/mol) via {step.EnzymeOrCatalyst}");
}
```

---

## 4. 3D Spatial Geometry & File Exporters

Generate 3D Cartesian coordinates ($x, y, z$) and VSEPR geometries:

```csharp
using Chemy.Core;

var methane = Molecule.Parse("CH4");
var m3d = methane.To3D();

Console.WriteLine($"VSEPR Shape: {m3d.VseprShape}"); // Tetrahedral
Console.WriteLine($"Bond Angle: {m3d.IdealBondAngleDegrees}°"); // 109.5°

// Export to .xyz / .pdb formats
string xyz = m3d.ToXyz();
string pdb = m3d.ToPdb();
```

---

## 5. NMR & IR Spectroscopy Prediction

Predict $^1\text{H}$-NMR peaks, $^{13}\text{C}$-NMR chemical shifts, and IR absorption spectrum bands:

```csharp
using Chemy.Core;
using Chemy.Core.Spectroscopy;

var aspirin = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var prediction = SpectroscopyEngine.Predict(aspirin);

foreach (var peak in prediction.H1NmrPeaks)
{
    Console.WriteLine($"1H-NMR Shift: δ {peak.ChemicalShiftPpm} ppm ({peak.Annotation})");
}
```

---

## 6. UFF-Inspired Energy Minimization

Relax 3D spatial Cartesian coordinates using van der Waals potential minimization:

```csharp
using Chemy.Core;
using Chemy.Core.Physics;

var m3d = Molecule.Parse("H2O").To3D();
var result = ForceFieldEngine.MinimizeEnergy(m3d, maxIterations: 500);

Console.WriteLine($"Initial Energy: {result.InitialEnergyKcalPerMol} kcal/mol");
Console.WriteLine($"Final Relaxed Energy: {result.FinalEnergyKcalPerMol} kcal/mol");
```

---

## 7. PubChem Live Cloud Search

Query NCBI PubChem database for 110M+ compounds:

```csharp
using Chemy.Core.Cloud;

var client = new PubChemClient();
var compound = await client.SearchCompoundAsync("Cocaine");

if (compound != null)
{
    Console.WriteLine($"CID: {compound.Cid} | IUPAC: {compound.IupacName}");
    Console.WriteLine($"Formula: {compound.MolecularFormula} | MW: {compound.MolecularWeight} g/mol");
}
```

---

## 8. Multi-Step Reaction Cascade Kinetics (RK4)

Simulate multi-step reaction cascades ($A \rightarrow B \rightarrow C$) using 4th-order Runge-Kutta integration:

```csharp
using Chemy.Core.Kinetics;

var cascade = ReactionNetworkEngine.SimulateConsecutiveCascade(
    initialConcA: 1.0,
    k1: 0.5,
    k2: 0.2,
    totalTime: 10.0
);

foreach (var point in cascade.Points.Take(5))
{
    Console.WriteLine($"t={point.TimeSeconds}s -> [A]={point.ConcentrationA}, [B]={point.ConcentrationB}, [C]={point.ConcentrationC}");
}
```

---

## 9. Solutions, pH & Buffer Calculations

Calculate solution pH and buffer capacity:

```csharp
using Chemy.Core.Solutions;

// Strong Acid pH (0.1 M HCl)
var strongAcid = SolutionsEngine.CalculateStrongAcidPh(0.1);
Console.WriteLine($"pH: {strongAcid.Ph}"); // 1.0

// Henderson-Hasselbalch Buffer
var buffer = SolutionsEngine.CalculateBufferPh(pka: 4.76, acidConcentrationMolar: 0.1, conjugateBaseConcentrationMolar: 0.1);
Console.WriteLine($"Buffer pH: {buffer.Ph}"); // 4.76
```

---

## 10. Electrochemistry & Nernst Equation

Calculate non-standard cell potentials ($E_{\text{cell}}$):

```csharp
using Chemy.Core.Electrochemistry;

var nernst = ElectrochemistryEngine.CalculateNernstPotential(
    standardCellPotentialVolts: 1.10,
    electronsTransferred: 2,
    reactionQuotientQ: 0.01,
    temperatureKelvin: 298.15
);

Console.WriteLine($"E_cell: {nernst.CellPotentialVolts:F3} V");
```

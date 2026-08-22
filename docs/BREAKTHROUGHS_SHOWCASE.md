# Chemy Use Cases and Demonstrations

[Documentation home](README.md) · [Cookbook](COOKBOOK.md) · [Scientific approach](SCIENTIFIC_APPROACH.md) · [Credibility report](SCIENTIFIC_CREDIBILITY_REPORT.md)

This document demonstrates three computational chemistry capabilities provided in **Chemy**:
1. 🧬 **Bioisosteric Lead Exploration Engine** (Pharmacophore Substitution & Graph Rewriting)
2. 🛡️ **Physicochemical Descriptors & Drug-Likeness** (Lipinski, Veber, Ghose, Ertl TPSA, Crippen LogP, QED)
3. ♻️ **EcoClean Qualitative Degradation Pathways** (Bond Dissociation Thermodynamics & Cleavage Cascades)

> These are illustrative software workflows. Molecular exploration and EcoClean outputs are hypotheses for expert review, not validated safety, efficacy, synthesis, kinetic, or environmental-outcome predictions.

---

## 🧬 Case Study 1: Bioisosteric Lead Exploration

### Background
Lead optimization explores structural modifications to candidate molecules to evaluate changes in lipophilicity ($\log P$), polar surface area ($\text{TPSA}$), and drug-likeness desirability ($\text{QED}$).

### The Chemy Approach
The `MolecularEvolverEngine` executes graph-traversing bioisosteric operations (e.g. replacing carboxylic acid groups with tetrazole heterocycles, para-fluorine substitution, pyridyl aza-substitution) and calculates resulting physicochemical profiles.

### Live Execution Demonstration
**Input Lead Molecule**: `CC(=O)Oc1ccccc1C(=O)O` (Aspirin)

```csharp
using Chemy.Core.Evolution;

var result = MolecularEvolverEngine.EvolveLeadCandidate("CC(=O)Oc1ccccc1C(=O)O", generations: 50);

Console.WriteLine($"Baseline: {result.BaselineMolecule} | QED: {result.BaselineQed:F2}");
foreach (var lead in result.Candidates)
{
    Console.WriteLine($"\n[★] {lead.CandidateName}");
    Console.WriteLine($"    SMILES: {lead.Smiles}");
    Console.WriteLine($"    QED Score: {lead.QedScore:F2} (LogP: {lead.CalculatedLogP:F2})");
    Console.WriteLine($"    Chemical Rationale: {lead.Rationale}");
    Console.WriteLine($"    Property Modification: {lead.ToxicityImprovement}");
}
```

### Generated Output

```text
Baseline: C9H8O4 | QED: 0.53

[★] Lead-01 (1H-Tetrazole Bioisostere)
    SMILES: CC(=O)Oc1ccccc1c1nnn[nH]1
    QED Score: 0.59 (LogP: 1.05)
    Chemical Rationale: Substituted metabolic carboxylic acid with non-classical 1H-tetrazole 5-membered aromatic ring.
    Property Modification: Carboxylic acid to 1H-tetrazole bioisosteric substitution; modulates acidity while preserving hydrogen-bonding topology.

[★] Lead-02 (Fluorine Bioisostere)
    SMILES: CC(=O)Oc1ccc(F)cc1C(=O)O
    QED Score: 0.55 (LogP: 1.45)
    Chemical Rationale: Introduced bioisosteric fluorine atom at aromatic scaffold position.
    Property Modification: Para-fluorine substitution heuristic; modulates lipophilicity and electronic distribution.
```

---

## 🛡️ Case Study 2: Physicochemical Descriptors & Drug-Likeness Profiling

### Background
Evaluating early-stage small molecules requires rapid screening of biophysical properties such as molecular weight, polar surface area, hydrogen-bond donors/acceptors, and empirical lipophilicity.

### The Chemy Approach
The `AdmetEngine` performs instantaneous descriptor calculations covering Lipinski's Rule of 5, Veber oral bioavailability criteria, Ghose drug filter, Ertl Topological Polar Surface Area ($\text{TPSA}$), and Bickerton Quantitative Estimate of Drug-Likeness ($\text{QED}$).

### Live Execution Demonstration
**Input Molecule**: `CC(=O)Oc1ccccc1C(=O)O` (Aspirin)

```csharp
using Chemy.Core;
using Chemy.Core.Pharmacology;

var molecule = Molecule.FromSmiles("CC(=O)Oc1ccccc1C(=O)O", "Aspirin");
var profile = AdmetEngine.Analyze(molecule);

Console.WriteLine($"Molecular Weight: {profile.MolecularWeight} g/mol");
Console.WriteLine($"Calculated LogP: {profile.CalculatedLogP}");
Console.WriteLine($"Topological Polar Surface Area: {profile.TpsaAngstrom2} Å²");
Console.WriteLine($"H-Bond Donors: {profile.HydrogenBondDonors} | H-Bond Acceptors: {profile.HydrogenBondAcceptors}");
Console.WriteLine($"Lipinski Rule of 5: {(profile.PassesLipinskiRuleOf5 ? "PASSED (0 violations)" : "FAILED")}");
Console.WriteLine($"QED Drug-Likeness Score: {profile.QedDrugLikenessScore}");
```

### Generated Output

```text
Molecular Weight: 180.16 g/mol
Calculated LogP: 1.31
Topological Polar Surface Area: 63.6 Å²
H-Bond Donors: 1 | H-Bond Acceptors: 4
Lipinski Rule of 5: PASSED (0 violations)
QED Drug-Likeness Score: 0.534
```

---

## ♻️ Case Study 3: *EcoClean* PFAS & Plastic Degradation Pathways

### Background
Perfluoroalkyl substances (**PFAS**) and synthetic polymers feature high bond dissociation energies ($\text{C}-\text{F} \approx 110\text{ kcal/mol}$), making degradation mechanisms a key area of environmental chemistry research.

### The Chemy Approach
The `EcoCleanEngine` models bond dissociation thermodynamics and constructs qualitative enzymatic and advanced oxidation cleavage cascades based on published chemical degradation mechanisms.

### Live Execution Demonstration
**Input Pollutant**: `PFOA C8HF15O2` (Perfluorooctanoic Acid)

```csharp
using Chemy.Core.Environmental;

var result = EcoCleanEngine.SolveDegradationCascade("PFOA C8HF15O2");

Console.WriteLine($"Pollutant Class: {result.PollutantClass}");
Console.WriteLine($"Theoretical Mineralization Products: {result.TheoreticalMineralizationProducts}\n");

foreach (var step in result.DegradationCascade)
{
    Console.WriteLine($"[Step {step.StepNumber}] {step.TargetBond}");
    Console.WriteLine($"       BDE: {step.BondDissociationEnergyKcalPerMol} kcal/mol");
    Console.WriteLine($"       Candidate System: {step.EnzymeOrCatalyst}");
    Console.WriteLine($"       Intermediate: {step.IntermediateProduct}");
    Console.WriteLine($"       Mechanism: {step.CleavageMechanism}\n");
}
```

### Generated Output

```text
Pollutant Class: PFAS 'Forever Chemical' (Perfluoroalkyl Substance)
Theoretical Mineralization Products: Fluoride (F⁻) + CO₂ + H₂O

[Step 1] Terminal Carboxylate Decarboxylation (C-COOH)
       BDE: 85 kcal/mol
       Candidate System: Electrochemical Anodic Oxidation / UV-Sulfite Catalysis
       Intermediate: Perfluoroalkyl Radical [C7F15•]
       Mechanism: Single-electron transfer decarboxylation initiates defluorination cascade.
```

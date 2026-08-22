# Scientific Foundations & Computational Algorithms in Chemy

This document provides a rigorous breakdown of the scientific theories, physical chemistry equations, chemoinformatics principles, and mathematical algorithms implemented in **Chemy**.

---

## 1. Bioisosteric Graph Mutations & Lead Optimization

The `MolecularEvolverEngine` implements graph-traversing bioisosteric optimization:
- Evaluates candidate lead structures against Quantitative Estimate of Drug-Likeness (QED), calculated lipophilicity ($\log P$), and structural toxicity liabilities.

### Bioisosteric Graph Transformations
- **Carboxylic Acid $\to$ 1H-Tetrazole Bioisosterism**: Substituted via `GraphRewriter.ReplaceCarboxylWithTetrazole` to explore carboxylic acid isosteric acidic proton binding.
- **Para-Fluorination**: Introducing para-fluorine ($\text{C-F}$) via `GraphRewriter.AppendFluorineShield` to modulate lipophilicity and electronic distribution.
- **Heterocycle Aza-Substitution**: Pyridyl ring nitrogen insertions to modulate $\log P$ and hydrogen-bonding selectivity.
- **Conformational Ring Locking**: Aliphatic methyl $\to$ cyclopropyl substitutions to reduce entropic conformational binding penalties.

---

## 2. Physicochemical Descriptors & Drug-Likeness (QED)

### Atom-Additive Crippen-Inspired $\log P$
Parameterizes the octanol-water partition coefficient based on atom hybridization ($sp^3, sp^2, sp$), aromaticity, formal charge, and neighbor connectivity:
$$\log P = \sum_{i=1}^N a_i n_i$$

### Ertl Topological Polar Surface Area (TPSA) (2000)
Calculates polar surface area from 2D topological fragment tables ($\text{\AA}^2$):
- Carbonyl oxygen ($=O$): $17.07\text{ \AA}^2$
- Hydroxyl oxygen ($-OH$): $20.23\text{ \AA}^2$
- Ester / Ether bridging oxygen ($-O-$): $9.23\text{ \AA}^2$
- Primary amide ($-CONH_2$): $17.07$ (carbonyl) $+ 26.02$ (nitrogen) $= \mathbf{43.09\text{ \AA}^2}$
- Secondary amide ($-CONHR$): $17.07$ (carbonyl) $+ 12.03$ (nitrogen) $= \mathbf{29.10\text{ \AA}^2}$
- Nitro group ($-NO_2$): $45.82\text{ \AA}^2$

### Bickerton QED (Nature Chemistry 2012)
$$\text{QED} = \exp\left( \frac{\sum_{i=1}^8 w_i \ln d_i}{\sum_{i=1}^8 w_i} \right)$$
where each desirability term follows the asymmetric sigmoid function:
$$d_i(x) = a_i + \frac{b_i}{1 + \exp\left(-\frac{x - c_i}{d_i}\right)}$$
evaluated across MW, ALOGP, HBD, HBA, PSA, ROTB, AROM, and Structural Alerts.

---

## 3. EcoClean PFAS & Plastic Degradation Pathways

### Topological Bond Dissociation Energies ($\text{BDE}$)
Retrieves characteristic single and multiple bond strengths from molecular topology:
- $\text{C-F}$ (PFAS): $\sim 116\text{ kcal/mol}$ ($485\text{ kJ/mol}$) (highest single-bond BDE in organic chemistry)
- $\text{C-H}$ (Aliphatic): $\sim 99\text{ kcal/mol}$ ($414\text{ kJ/mol}$)
- $\text{C-O}$ (Polyester Ester): $\sim 86\text{ kcal/mol}$ ($358\text{ kJ/mol}$)
- $\text{C-Cl}$ (Organohalide): $\sim 78\text{ kcal/mol}$ ($328\text{ kJ/mol}$)
- $\text{C-Br}$ (Organobromide): $\sim 66\text{ kcal/mol}$ ($276\text{ kJ/mol}$)

### Catalytic Degradation Pathways
1. **Electrochemical Anodic Decarboxylation**: Single-electron oxidation converts terminal perfluoroalkyl carboxylate $\text{R}_f\text{-COO}^-$ into fluororadical $\text{R}_f^\bullet$.
2. **$\alpha$-Elimination & HF Release**: Hydroxylation generates perfluoroalkanol which eliminates $\text{F}^-$.
3. **Qualitative Mineralization Cascade**: Stepwise chain shortening produces stoichiometric inorganic mineral ions ($\text{F}^-, \text{Cl}^-, \text{Br}^-, \text{SO}_4^{2-}, \text{PO}_4^{3-}, \text{NO}_3^-, \text{CO}_2, \text{H}_2\text{O}$).

---

## 4. Stoichiometry & Mass/Charge Nullspace Algebra

The chemical reaction nullspace equation enforces both atom conservation and electric charge conservation:

$$\begin{pmatrix} A_{1,1} & \cdots & -A_{C,1} \\ \vdots & \ddots & \vdots \\ A_{1,E} & \cdots & -A_{C,E} \\ q_1 & \cdots & -q_C \end{pmatrix} \begin{pmatrix} \nu_1 \\ \vdots \\ \nu_C \end{pmatrix} = \begin{pmatrix} 0 \\ \vdots \\ 0 \end{pmatrix}$$

All matrix operations are performed over the exact rational field $\mathbb{Q}$ using greatest common divisor ($\gcd$) reduction and least common multiple ($\text{lcm}$) scaling to yield the unique primitive minimal integer solution vector $\vec{\nu} \in \mathbb{N}^C$.

---

## 5. 3D Spatial Geometry & Multi-Center Conformer Embedding

- **VSEPR Coordination**: Direct coordinate generation for small inorganics based on steric numbers and lone pairs (Linear $180^\circ$, Bent $104.5^\circ$, Trigonal Planar $120^\circ$, Tetrahedral $109.5^\circ$, Trigonal Bipyramidal $90^\circ/120^\circ$, Octahedral $90^\circ$).
- **Multi-Center Graph Embedding**: Breadth-first graph propagation constructing local orthogonal reference frames via Gram-Schmidt vector projection and ideal covalent radii $r_0(e_1, e_2, \text{bondType})$.

---

## 6. 5-Term UFF-Inspired Molecular Mechanics Force Field

### Potential Function
$$E_{\text{total}} = E_{\text{bond}} + E_{\text{angle}} + E_{\text{torsion}} + E_{\text{inv}} + E_{\text{vdw}}$$

Bond and angle terms are harmonic, torsions use the implemented 2-fold/3-fold forms, and trivalent planar C/N/O centers use three out-of-plane permutations. Carbonyl carbon receives the published UFF 50 kcal/mol inversion special case. `CalculateEnergyComponents` exposes every term independently; the implementation remains an explicitly documented UFF-inspired subset rather than a full interchangeable UFF engine.

### Central Finite-Difference Force Gradients ($-\nabla E$)
Forces on atom coordinates are evaluated via high-precision central finite difference numerical gradients:
$$F_{x,i} = -\frac{E(\mathbf{r}_i + h\hat{x}) - E(\mathbf{r}_i - h\hat{x})}{2h}$$
with displacement $h = 10^{-5}\text{ \AA}$, coupled with line-search optimization and soft-core steric clash buffering.

`EnergyMinimizationResult` and `Geometry3DEngine.GenerateConformer3DResult` expose convergence, termination reason, final gradient, iteration count, and initial/final energies so production callers can reject non-converged coordinates.

---

## 7. Solutions Chemistry & Exact Cubic Equilibrium Solver

Weak monoprotic acid dissociation with autoionization of water ($K_w = 1.0 \times 10^{-14}$) is solved via the exact cubic polynomial:

$$[\text{H}^+]^3 + K_a [\text{H}^+]^2 - (K_w + K_a C_{\text{total}})[\text{H}^+] - K_a K_w = 0$$

Solved via **Halley's high-order root-finding method**, providing machine-precision convergence without dilutional divergence.

---

## 8. Benson Group Additivity & Thermochemistry

Standard enthalpies ($\Delta H_f^\circ$), entropies ($S^\circ$), and Gibbs energies ($\Delta G_f^\circ$) are calculated using the published **Benson group increment scheme** (S.W. Benson, 1976), recognizing central polyvalent atoms with their ligand coordination environments and ring strain corrections.

---

## 9. Chemical Kinetics & Electrochemistry

- **Coupled Reaction ODEs**: Solved via 4th-Order Runge-Kutta (RK4) numerical integration for arbitrary multi-step networks.
- **Arrhenius Equation**: $k = A \exp(-E_a / RT)$.
- **Nernst Equation**: $E_{\text{cell}} = E^\circ_{\text{cell}} - \frac{RT}{nF} \ln Q$.

---

## 10. Verification & Technical Credibility Audit

Every algorithm across Chemy is validated by comprehensive automated unit tests in `Chemy.Core.Tests` with zero compiler warnings. 
- Consult the exhaustive [**Scientific Credibility & Technical Audit Report**](SCIENTIFIC_CREDIBILITY_REPORT.md) for full mathematical proofs, algorithm evaluations, and domain scorecards.
- Consult the [**Scientific Verification Benchmarks Suite**](SCIENTIFIC_VERIFICATION_BENCHMARKS.md) for empirical benchmark tables across 21 standard chemical systems.

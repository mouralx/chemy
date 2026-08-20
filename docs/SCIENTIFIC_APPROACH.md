# Scientific Foundations & Computational Algorithms in Chemy

This document provides a rigorous breakdown of the scientific theories, physical chemistry equations, chemoinformatics principles, and mathematical algorithms implemented in **Chemy**.

---

## 1. De Novo Generative Evolutionary Optimization

The `MolecularEvolverEngine` implements an autonomous population-based multi-objective genetic algorithm:

$$\text{Fitness}(\mathbf{m}) = w_1 \cdot \text{QED}(\mathbf{m}) + w_2 \cdot \mathcal{S}_{\text{solubility}}(\mathbf{m}) - w_3 \cdot \mathcal{P}_{\text{toxicity}}(\mathbf{m}) - w_4 \cdot \mathcal{C}_{\text{synthetic}}(\mathbf{m})$$

### Bioisosteric Graph Transformations
- **Carboxylic Acid $\to$ 1H-Tetrazole Bioisosterism**: Substituted via `GraphRewriter.ReplaceCarboxylWithTetrazole` to eliminate reactive acyl-glucuronide hepatotoxicity while maintaining isosteric acidic proton binding.
- **Metabolic Fluorine Shielding**: Introducing para-fluorine ($\text{C-F}$) via `GraphRewriter.AppendFluorineShield` to sterically block Cytochrome P450 CYP3A4 aromatic hydroxylation.
- **Heterocycle Aza-Substitution**: Pyridyl and pyrimidinyl ring nitrogen insertions to modulate $\log P$ and hydrogen-bonding selectivity.
- **Conformational Ring Locking**: Aliphatic methyl $\to$ cyclopropyl substitutions to reduce entropic conformational binding penalties.

---

## 2. ADMET & Quantitative Estimate of Drug-Likeness (QED)

### Full 68-Atom Wildman-Crippen $\log P$ (1999)
Parameterizes the octanol-water partition coefficient based on atom hybridization ($sp^3, sp^2, sp$), aromaticity, formal charge, and neighbor connectivity:
$$\log P = \sum_{i=1}^N a_i n_i$$

### Full 43-Fragment Ertl Topological Polar Surface Area (TPSA) (2000)
Calculates polar surface area from exact 2D topological fragment tables ($\text{\AA}^2$):
- Carbonyl oxygen ($=O$): $17.07\text{ \AA}^2$
- Hydroxyl oxygen ($-OH$): $20.23\text{ \AA}^2$
- Ester / Ether bridging oxygen ($-O-$): $9.23\text{ \AA}^2$
- Secondary amide ($-C(=O)NH-$): $29.10\text{ \AA}^2$
- Primary amide ($-C(=O)NH_2$): $43.09\text{ \AA}^2$
- Nitro group ($-NO_2$): $45.82\text{ \AA}^2$

### Exact Bickerton QED (Nature Chemistry 2012)
$$\text{QED} = \exp\left( \frac{\sum_{i=1}^8 w_i \ln d_i}{\sum_{i=1}^8 w_i} \right)$$
where each desirability term follows the asymmetric sigmoid function:
$$d_i(x) = a_i + \frac{b_i}{1 + \exp\left(-\frac{x - c_i}{d_i}\right)}$$
evaluated across MW, ALOGP, HBD, HBA, PSA, ROTB, AROM, and Structural Alerts (PAINS/Brenk toxicophores).

---

## 3. EcoClean PFAS & Plastic Biocleavage Thermodynamics

### Bond Dissociation Energies ($\text{BDE}$)
Calculates local bond strengths from molecular topology:
- $\text{C-F}$ (PFAS): $\sim 116\text{ kcal/mol}$ ($485\text{ kJ/mol}$) (highest single-bond BDE in organic chemistry)
- $\text{C-H}$ (Aliphatic): $\sim 99\text{ kcal/mol}$ ($414\text{ kJ/mol}$)
- $\text{C-O}$ (Polyester Ester): $\sim 86\text{ kcal/mol}$ ($358\text{ kJ/mol}$)
- $\text{C-Cl}$ (Organohalide): $\sim 78\text{ kcal/mol}$ ($328\text{ kJ/mol}$)
- $\text{C-Br}$ (Organobromide): $\sim 66\text{ kcal/mol}$ ($276\text{ kJ/mol}$)

### Catalytic Cascade Mechanisms
1. **Electrochemical Anodic Decarboxylation**: Single-electron oxidation converts terminal perfluoroalkyl carboxylate $\text{R}_f\text{-COO}^-$ into reactive fluororadical $\text{R}_f^\bullet$.
2. **$\alpha$-Elimination & HF Release**: Hydroxylation generates perfluoroalkanol which spontaneously eliminates $\text{F}^-$.
3. **Complete Mineralization**: Iterative chain shortening produces harmless inorganic minerals ($\text{F}^-, \text{Cl}^-, \text{Br}^-, \text{SO}_4^{2-}, \text{PO}_4^{3-}, \text{NO}_3^-, \text{CO}_2, \text{H}_2\text{O}$).

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

## 6. 4-Term Molecular Mechanics Force Field (UFF)

### Analytical Potential Function
$$E_{\text{total}} = \sum \frac{1}{2} k_r (r - r_0)^2 + \sum \frac{1}{2} k_\theta (\theta - \theta_0)^2 + \sum \frac{V_3}{2}[1 + \cos(3\phi)] + \sum_{\text{1,4+}} \epsilon \left[\left(\frac{r_m}{r_{ij}}\right)^{12} - 2\left(\frac{r_m}{r_{ij}}\right)^6\right]$$

### Exact Analytical Force Gradients ($-\nabla E$)
- **Harmonic Bond Pull**: $\mathbf{F}_{i} = -k_r (r - r_0) \frac{\mathbf{r}_i - \mathbf{r}_j}{r}$
- **Valence Angle Restoring Torque**: Analytical tangential force vectors on triplets $n_1 - c - n_2$.
- **Lennard-Jones van der Waals Force**:
  $$\mathbf{F}_{\text{vdw}} = \frac{12 \epsilon}{r^2}\left[\left(\frac{r_m}{r}\right)^{12} - \left(\frac{r_m}{r}\right)^6\right](\mathbf{r}_i - \mathbf{r}_j)$$

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

## 10. Verification & Benchmark Suite

Every algorithm across Chemy is validated by **71 automated unit tests** in `Chemy.Core.Tests` with zero compiler warnings. Consult [Scientific Verification Benchmarks Suite](file:///Users/moura/Desktop/chemy/docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md) for full benchmark tables.

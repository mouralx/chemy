# Scientific Foundations & Computational Algorithms in Chemy

This document provides a rigorous breakdown of the scientific theories, physical chemistry equations, chemoinformatics principles, and mathematical algorithms implemented in **Chemy**.

---

## 1. De Novo Generative Evolutionary Optimization

The `MolecularEvolverEngine` implements an autonomous multi-objective genetic algorithm:

$$\text{Fitness}(\mathbf{m}) = w_1 \cdot \text{QED}(\mathbf{m}) + w_2 \cdot \mathcal{S}_{\text{solubility}}(\mathbf{m}) - w_3 \cdot \mathcal{P}_{\text{toxicity}}(\mathbf{m}) - w_4 \cdot \mathcal{C}_{\text{synthetic}}(\mathbf{m})$$

### Bioisosteric Transformations
- **Carboxylic Acid Bioisosterism**: Substituted with tetrazole ($-\text{c1nnn[nH]1}$) or acyl sulfonamide to eliminate reactive acyl-glucuronide formation.
- **Deuterium Kinetic Isotope Effect (KIE)**: Substituting vulnerable $\text{C-H}$ bonds with heavier $\text{C-D}$ ($k_H / k_D \approx 6.5$) to reduce Phase-I CYP-mediated clearance.
- **Metabolic Shielding**: Strategically introducing para-fluorine ($\text{C-F}$) to block toxic quinone-imine formation.

---

## 2. ADMET & Quantitative Estimate of Drug-Likeness (QED)

### Lipinski's Rule of 5 (Pfizer Rule)
Empirical guidelines for oral drug bioavailability:
1. $\text{Molecular Weight} \le 500\text{ g/mol}$
2. $\text{Calculated }\log P \le 5$ (Octanol-Water partition coefficient)
3. $\text{Hydrogen Bond Donors (HBD)} \le 5$
4. $\text{Hydrogen Bond Acceptors (HBA)} \le 10$

### Topological Polar Surface Area (TPSA)
Sum of polar fragments ($\text{O}, \text{N}, \text{S}$) parameterized in $\text{\AA}^2$. Predicts blood-brain barrier permeability ($\text{TPSA} < 90\text{ \AA}^2$) versus peripheral restriction ($\text{TPSA} > 140\text{ \AA}^2$).

---

## 3. EcoClean PFAS & Plastic Biocleavage Thermodynamics

### Bond Dissociation Energies ($\text{BDE}$)
Calculates energy required for homolytic bond cleavage:
- $\text{C-F}$ (PFAS): $\sim 110\text{ kcal/mol}$ (most recalcitrant single bond in organic chemistry)
- $\text{C-C}$ (Aliphatic): $\sim 83\text{ kcal/mol}$
- $\text{C-O}$ (Polyester Ester): $\sim 78\text{ kcal/mol}$

### Catalytic Cascade Mechanisms
1. **Electrochemical Decarboxylation**: Single-electron oxidation converts terminal $\text{-COOH}$ into reactive fluororadical $\text{R}_f^\bullet$.
2. **$\alpha$-Elimination & HF Release**: Hydroxylation generates perfluoroalkanol which spontaneously eliminates $\text{F}^-$.
3. **Complete Mineralization**: Iterative chain shortening produces harmless inorganic minerals ($\text{F}^-, \text{CO}_2, \text{H}_2\text{O}$).

---

## 4. Stoichiometry & Exact Rational Matrix Algebra

### The Law of Conservation of Mass
The fundamental physical law governing chemical reactions is the **Law of Conservation of Mass** (Antoine Lavoisier, 1789), which states that mass is neither created nor destroyed in a chemical reaction.

$$M \vec{x} = \vec{0}, \quad \vec{x} \in \mathbb{Z}_{>0}^{R+P}$$

All matrix operations are performed over the exact rational field $\mathbb{Q}$ using greatest common divisor ($\gcd$) reduction and least common multiple ($\text{lcm}$) scaling to yield the unique primitive minimal integer solution vector $\vec{x}$.

---

## 5. 3D Spatial Geometry & VSEPR Valence Theory

VSEPR (Valence Shell Electron Pair Repulsion) theory models 3D molecular geometry based on electron pair repulsion around central atoms:

| Steric Number | Lone Pairs | VSEPR Shape | Ideal Bond Angle | Examples |
| :--- | :--- | :--- | :--- | :--- |
| 2 | 0 | **Linear** | $180^\circ$ | $\text{CO}_2$, $\text{H}_2$ |
| 3 | 0 | **Trigonal Planar** | $120^\circ$ | $\text{BH}_3$, $\text{CO}_3^{2-}$ |
| 4 | 0 | **Tetrahedral** | $109.5^\circ$ | $\text{CH}_4$, $\text{CCl}_4$, $\text{NH}_4^+$ |
| 4 | 1 | **Trigonal Pyramidal** | $107^\circ$ | $\text{NH}_3$, $\text{PCl}_3$ |
| 4 | 2 | **Bent** | $104.5^\circ$ | $\text{H}_2\text{O}$, $\text{H}_2\text{S}$ |
| 4 | 0 | **Square Planar** | $90^\circ$ | $\text{XeF}_4$, $[\text{PtCl}_4]^{2-}$ |
| 5 | 0 | **Trigonal Bipyramidal** | $90^\circ / 120^\circ$ | $\text{PCl}_5$ |
| 6 | 0 | **Octahedral** | $90^\circ$ | $\text{SF}_6$, $[\text{Fe(CN)}_6]^{3-}$ |

---

## 6. NMR & IR Spectroscopy Theory

### Nuclear Magnetic Resonance ($^1\text{H}$-NMR & $^{13}\text{C}$-NMR)
Chemical shift $\delta$ in parts per million (ppm) is determined by nuclear shielding tensors $\sigma$:

$$\delta = \frac{\nu_{\text{sample}} - \nu_{\text{ref}}}{\nu_{\text{ref}}} \times 10^6$$

### Infrared (IR) Vibrational Spectroscopy
Infrared absorption frequency $\nu$ follows Hooke's Law for diatomic harmonic oscillators:

$$\bar{\nu} = \frac{1}{2\pi c} \sqrt{\frac{k}{\mu}}$$

---

## 7. 4-Term Molecular Mechanics Force Field (UFF)

### Analytical Potential Function
$$E_{\text{total}} = \sum \frac{1}{2} k_r (r - r_0)^2 + \sum \frac{1}{2} k_\theta (\theta - \theta_0)^2 + \sum \frac{V_n}{2}[1 + \cos(n\phi - \gamma)] + \sum_{\text{1,4+}} \epsilon \left[\left(\frac{r_m}{r_{ij}}\right)^{12} - 2\left(\frac{r_m}{r_{ij}}\right)^6\right]$$

### Non-Bonded 1,2 & 1,3 Steric Exclusions
In standard computational chemistry, directly bonded (1,2) and geminal angle-connected (1,3) atom pairs are strictly excluded from non-bonded van der Waals Lennard-Jones summation, preventing unphysical short-range repulsive divergence while preserving true equilibrium conformations.

---

## 8. Multi-Step Reaction Networks & RK4 Integration

For consecutive reaction networks ($A \xrightarrow{k_1} B \xrightarrow{k_2} C$), concentration trajectories are integrated using the 4th-order Runge-Kutta (RK4) algorithm:

$$y_{n+1} = y_n + \frac{\Delta t}{6}(k_1 + 2k_2 + 2k_3 + k_4)$$

---

## 9. Solutions Chemistry & Acid-Base Equilibria

$$\text{pH} = -\log_{10}[\text{H}^+], \quad \text{pH} = \text{pK}_a + \log_{10}\left( \frac{[\text{A}^-]}{[\text{HA}]} \right)$$

---

## 10. Electrochemistry & Chemical Kinetics

### Nernst Equation
$$E = E^\circ - \frac{R T}{n F} \ln Q$$

### Arrhenius Rate Law
$$k = A \cdot e^{-\frac{E_a}{R T}}$$

---

## 11. Empirical Verification & Benchmark Suite

Every algorithm and physical equation across Chemy is validated against peer-reviewed experimental and quantum mechanical benchmarks. For full mathematical derivations, real-world sample inputs, and live microservice response payloads, consult the dedicated [Scientific Verification Benchmarks Suite](file:///Users/moura/Desktop/chemy/docs/SCIENTIFIC_VERIFICATION_BENCHMARKS.md).

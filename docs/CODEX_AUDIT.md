# Chemy Scientific Credibility Audit

> **Audit snapshot:** This report describes the repository before the scientific-validity changes in commit `b436fe3` (`fix: enforce scientific validity boundaries`). Some findings have since been addressed on the `fix/scientific-validity-boundaries` branch. The report is retained as the baseline assessment that motivated those changes.

## Executive verdict

Chemy contains several correctly implemented textbook calculators and useful software components. However, its documentation systematically elevates simplified algorithms and heuristics into claims of scientific prediction, universal applicability, or industrial validation.

The appropriate classification at the time of this audit was:

- **Credible for:** chemistry education, demonstrations, developer experiments, and elementary deterministic calculations.
- **Potentially useful with validation:** reaction balancing, formula handling, simple kinetics, Hückel exercises, and chemical-format generation.
- **Not credible for:** quantitative ADMET, drug safety, lead optimization, spectroscopy prediction, environmental-remediation efficiency, general thermochemistry, force-field energetics, or publication-quality molecular modelling.

The blanket claims of “industrial-grade,” “100% scientifically verified,” and a 9.5/10 credibility score were not supported by the repository evidence.

## Audit method

The assessment included:

- inspection of every major scientific subsystem;
- comparison of implementation behavior with README and scientific-documentation claims;
- inspection of all 114 tests;
- a complete solution build and test run;
- code-coverage measurement;
- dependency vulnerability inspection;
- independent adversarial probes not authored by the project.

At the time of the audit:

- the solution built with 0 warnings and 0 errors;
- 114 tests passed, with no failures or skips;
- core line coverage was approximately 79.3%;
- core branch coverage was approximately 68.1%.

Passing tests supported regression stability, but did not establish scientific validity for every advertised domain.

## Component summary

| Component | Did code implement the claim? | Assessment |
|---|---|---|
| Periodic table | Mostly | Credible lookup table, but not a complete isotope/reference system |
| Formula parser | Mostly | Good compositional parser; generated bonds were chemically unreliable |
| SMILES parser | Partially | Useful subset, not standards-compliant SMILES |
| Reaction balancing | Partially | Exact rational arithmetic for simple one-dimensional nullspaces |
| Stoichiometry | Yes, within scope | Basic mole/mass arithmetic was sound |
| Strong/weak acid pH | Mostly | Sound equations under restricted ideal-solution assumptions |
| Buffer pH | Yes, within scope | Direct Henderson–Hasselbalch calculation |
| Electrochemistry | Yes, within scope | Correct basic Nernst calculation |
| Basic kinetics | Yes, within scope | Standard half-life and Arrhenius equations |
| Reaction networks | Mostly | RK4 was present, but “arbitrary” was overstated |
| Hückel matrix solver | Mostly | Useful educational HMO implementation |
| Automatic molecular Hückel analysis | Partially | Atom typing and interpretation were heuristic |
| Thermodynamic tables | Partially | Small, phase-ambiguous lookup table |
| Benson group additivity | No, not faithfully | Loosely inspired heuristic, not a demonstrated Benson implementation |
| Molecular mechanics | No | Energy and force implementations were inconsistent |
| 3D conformers | Partially | Diagram/initial-coordinate generator, not validated conformer generation |
| TPSA/LogP/QED | No, not faithfully | Simplified approximations labeled as complete published methods |
| ADMET/safety | No | Threshold-generated advisory text, not predictive toxicology |
| Spectroscopy | Partially | Functional-group teaching table, not general spectral prediction |
| Graph matching | Mostly | Basic injective matcher; not VF2 or industrial substructure search |
| Ring detection | Partially | DFS cycle detection, not SSSR/Hansch ring perception |
| Lead evolution | No | Scripted mutations, not demonstrated lead optimization |
| EcoClean | No | Hard-coded narratives and manufactured efficiency values |
| Molfile/SDF export | Partially | Basic output existed; complete conformance was unproven |
| PubChem client | Mostly | Simple HTTP lookup, not a resilient integration |
| REST API | Implemented | Broad endpoint surface, but essentially untested as an API |

## Detailed findings

### 1. Periodic table and formula calculations

The repository contained all 118 elements in frozen lookup dictionaries. This supported element lookup, atomic-number lookup, and approximate molar-mass calculations.

It did not support the broader description of “elemental physics” or “isotopic models.” Each element had one scalar mass, and default neutron counts were obtained by rounding that mass. This was not an isotope-abundance model.

The formula parser handled nested brackets, hydrates, and charges reasonably well. However, it generated molecular connectivity by connecting every atom to the first non-hydrogen atom. Formula input could therefore establish composition and mass, but generally could not establish chemically meaningful topology, functional groups, geometry, or graph-derived properties.

### 2. SMILES parsing

The parser supported common atoms, branches, one-digit ring closures, common bond orders, aromatic bonds, and simple bracket charges and hydrogen counts.

Unsupported characters were silently ignored. Independent probes found:

- `C@C` was accepted as ethane, `C2H6`;
- `F[C@](Cl)(Br)I` and `F[C@@](Cl)(Br)I` produced indistinguishable graphs;
- stereochemistry was discarded rather than preserved.

The parser also lacked general multi-digit ring syntax, isotope handling, robust aromatic semantics, complete bracket syntax, bond direction, and validation expected from a standards-grade implementation.

### 3. Reaction balancing

For conventional equations with a one-dimensional nullspace, the implementation genuinely used rational Gaussian elimination and conserved charge. The common documented examples were credible.

It was not a general positive-integer nullspace solver. It selected one free variable and left other free variables at zero. The independent probe:

```text
C + O2 -> CO + CO2
```

caused a `DivideByZeroException`, although the equation admits positive balanced families. Integer overflow was also possible because rational values used `long` before conversion to `int`.

### 4. Acid/base, electrochemistry, and kinetics

These were among the strongest components.

The documented strong-acid equation and weak-monoprotic-acid cubic were implemented. Their applicability was nevertheless limited by:

- fixed `Kw = 1e-14`;
- fixed `pH + pOH = 14`;
- ideal activities;
- monoprotic acids only;
- no ionic-strength or temperature dependence;
- unconditional Henderson–Hasselbalch use without a validity check.

The Nernst implementation used the documented constants and equation correctly. Arrhenius and standard half-life equations were also correct within their elementary models.

The general reaction-network method used the RK4 tableau, but nonnegative clamping at intermediate and final stages changed the numerical method. The fixed cascade method did not validate all inputs: a zero-step simulation returned a one-point result rather than rejecting the request.

### 5. Thermodynamics

Hess-law arithmetic was correctly performed once appropriate formation data were available.

The limitations were primarily in identity, reference data, and fallback estimation:

- only a small compound table was present;
- physical phases were not represented;
- formulas and aliases were used as compound identities;
- isomers with the same formula received the same properties;
- heat-capacity, temperature, and phase-transition effects were absent.

For example, every `C2H6O` structure resolved to the ethanol entry, including dimethyl ether.

The claimed “true Benson group additivity” fallback began with an ad hoc entropy baseline based on molecular weight and applied a limited set of local increments. This did not establish a faithful, generally applicable Benson implementation. Calling the engine “100% universal” was contradicted by the code.

### 6. Hückel engine

The explicit-matrix pathway was one of the stronger scientific implementations. The Jacobi eigensolver reproduced analytical textbook eigenvalues for ethylene, butadiene, benzene, and several fused systems.

Important limitations remained:

- Jacobi diagonalization is numerical, not exact;
- automatic molecular atom typing used limited heuristics;
- degenerate open-shell occupancy was not treated rigorously;
- UV-visible output was simply `hc / HOMO-LUMO gap`, without transition selection rules or intensities;
- the resonance calculation used a simplified localized-double-bond reference;
- the reported benzene value of 125 kcal/mol should not be interpreted as experimental aromatic stabilization energy.

The engine was credible as an educational Hückel calculator, not as general quantum chemistry or quantitative UV-visible prediction.

### 7. Force field and 3D geometry

The documentation claimed that the forces were the exact analytical gradient of the implemented energy. They were not.

The energy excluded 1–2 and 1–3 nonbonded interactions, whereas the force calculation excluded only directly bonded 1–2 interactions. Additional arbitrary scale factors and force clamping made the energy and force implementations mathematically inconsistent.

The minimizer also returned `Converged = true` unconditionally. With `maxIterations = 0`, an independent probe reported one iteration, unchanged energy, and successful convergence.

The 3D system used fixed geometries, regular polygons, breadth-first coordinate propagation, and this generalized force field. That was useful for visualization and starting coordinates, but did not justify claims of physically valid conformers or UFF/MMFF behavior.

### 8. Chemoinformatics and ADMET

The code did not contain complete implementations of the published Wildman–Crippen, 43-fragment Ertl TPSA, or QED algorithms.

For ibuprofen, the documentation reported:

- LogP: 4.00;
- TPSA: 34.1 Å².

The audited implementation returned:

- LogP: 2.88;
- TPSA: 37.3 Å².

The published “live benchmark” therefore did not match the current implementation.

Hydrogen-bond acceptors were counted as every oxygen, nitrogen, or fluorine atom. That misclassified amide nitrogen, protonated and pyrrolic nitrogen, acidic groups, and covalently bound fluorine.

hERG, CYP, and blood-brain-barrier output consisted of fixed thresholds and prose, not trained or externally validated models. An audit probe classified ibuprofen as having “High BBB Permeability (CNS Active)” solely because it passed three descriptor thresholds. This was not a defensible drug-safety prediction.

### 9. Spectroscopy

The spectroscopy engine was a functional-group correlation table, not a general spectrum predictor.

For acetone, documentation reported one six-proton singlet. The implementation returned:

- a three-proton singlet at 2.15 ppm;
- a three-proton triplet at 1.15 ppm.

The engine did not determine chemical equivalence, coupling graphs, stereochemical relationships, solvent effects, conformational averaging, or calculated shielding and vibrational frequencies. It was reasonable as an educational functional-group hint generator.

### 10. Lead evolution

The engine performed a small set of scripted graph edits. It was not meaningfully a genetic or evolutionary optimization system:

- no randomized population;
- no crossover;
- no fitness-based parent selection;
- little relationship between requested generations and search depth;
- duplicate suppression based on formula rather than molecular identity;
- duplicate baseline fallbacks described as optimized candidates.

Generated rationales claimed improvements in toxicity, metabolism, binding, half-life, and bioavailability that could not be inferred from the implemented graph edits.

### 11. EcoClean

EcoClean was the most serious scientific-credibility issue found.

The engine selected predefined narrative pathways based mainly on the elements and functional groups present. It did not simulate reactions, catalysts, kinetics, yields, equilibria, competing pathways, experimental conditions, or mass balance.

Mineralization efficiency was generated from:

```csharp
99.0 + Math.Clamp(10.0 / secondaryBde, 0.2, 0.8)
```

An independent PFOA probe returned 99.2%. This was not a scientifically derived mineralization prediction. The dedicated test merely asserted that the result exceeded 90%, confirming the hard-coded behavior rather than validating it.

### 12. Graph algorithms and chemical formats

The subgraph matcher was a valid small injective backtracking matcher, but was not VF2 despite that label appearing elsewhere.

Ring detection used DFS back-edge collection rather than a demonstrated SSSR or chemically robust ring-perception algorithm.

Molfile and SDF output implemented basic records. However:

- formal charges were not written;
- isotope records were absent;
- V2000 size limits were not enforced;
- stereochemistry was absent;
- external conformance tests were absent;
- the exporter was described as a parser despite containing no parser.

Basic interoperability was plausible, but complete standards compliance and compatibility with named third-party applications were unverified.

## Engineering and reproducibility findings

Positive findings included:

- clean compilation;
- nullable reference types;
- deterministic core behavior;
- reasonable project organization;
- meaningful unit-test coverage.

Problems included:

- `TreatWarningsAsErrors` was explicitly `false` despite the README claiming enforcement;
- the API suppressed `NU1903`;
- the dependency graph contained high-severity advisory `GHSA-v5pm-xwqc-g5wc` through `Microsoft.OpenApi 2.0.0`;
- API integration tests were absent;
- PubChem, serialization, and parts of reaction-network functionality had no measured coverage;
- no CI workflow was present;
- no `LICENSE` file existed despite an MIT badge;
- no `CITATION.cff`, versioned scientific dataset, benchmark-generation script, uncertainty analysis, or independent validation report was present;
- “zero dependency” was false for the full product;
- “zero allocations” was contradicted throughout the implementation.

## Conclusion

Chemy was not empty or fraudulent software. It contained genuine implementations, several sound elementary calculations, and a meaningful regression suite. Its central credibility problem was failure to distinguish between:

1. exact arithmetic and textbook equations;
2. numerical approximations;
3. empirical models;
4. visualization and classification heuristics;
5. unsupported predictive narratives.

A defensible description of the audited code was:

> An educational .NET chemistry toolkit providing formula parsing, common chemistry equations, basic graph operations, simplified Hückel calculations, heuristic molecular visualization, and experimental rule-based property demonstrations.

Under that description, Chemy had credible value for students and developers. Under its advertised claims of industrial chemoinformatics, lead optimization, safety prediction, universal thermodynamics, and environmental-remediation design, it was not credible without substantial correction and independent validation.

## Remediation boundary

Code changes can correct mathematical defects, reject unsupported inputs, expose applicability information, and remove manufactured predictions. Code alone cannot establish experimental validity. Credible scientific claims ultimately require versioned reference datasets, reproducible benchmark generation, error metrics, applicability-domain analysis, and independent comparison or review.

# Chemy Scientific Credibility Audit

**Audit date:** 2026-08-21  
**Audited revision:** `236fac3` (`main`)  
**Auditor:** OpenAI Codex  
**Scope:** Documentation claims, core scientific implementations, automated tests, numerical behavior, API surface, dependency health, and reproducibility evidence.

## 1. Executive conclusion

Chemy is a real and substantial software project. It contains working parsers, graph utilities, numerical routines, educational chemical calculators, rendering components, and a passing automated test suite. Several narrow textbook calculations—molar mass, common reaction balances, Nernst potential, Arrhenius rate constants, elementary half-lives, and explicit-matrix Hückel examples—are implemented competently within limited domains.

The solution is **not presently credible as an industrial or research-grade computational chemistry platform**. The main problem is not the absence of code; it is the mismatch between what the code calculates and what the documentation says those calculations establish. Simplified lookup rules and visualization heuristics are repeatedly described as complete published methods, safety predictions, universal thermodynamics, physically valid conformers, or experimentally meaningful remediation outcomes.

### Credibility ratings

| Context | Rating | Interpretation |
|---|---:|---|
| Software implementation quality | **6.0 / 10** | Organized and functional, but with correctness defects, weak boundaries, and security/configuration issues |
| Chemistry education and demonstrations | **7.0 / 10** | Useful if limitations are taught explicitly and outputs are independently checked |
| Developer prototyping | **6.0 / 10** | A useful experimental .NET codebase, not yet a dependable chemistry foundation |
| Quantitative scientific analysis | **3.0 / 10** | Some sound narrow equations; many graph-derived and predictive results are not faithful models |
| Research/publication use | **2.0 / 10** | No external validation corpus, uncertainty analysis, reproducible benchmark generation, or applicability controls |
| Drug-safety/environmental decisions | **1.0 / 10** | Current categorical outputs can imply evidence that the implementation does not contain |
| **Overall claim-adjusted credibility** | **4.5 / 10** | Genuine educational software whose advertised scientific authority substantially exceeds its evidence |

The overall rating is a structured engineering judgment, not a statistical confidence interval. It combines implementation fidelity, validation strength, applicability controls, failure behavior, reproducibility, and the risk created by misleading claims.

### Bottom-line usage recommendation

Chemy may currently be used for:

- educational demonstrations of elementary chemistry and numerical methods;
- UI/API prototyping;
- exploratory calculations that are independently verified;
- studying simplified Hückel and graph algorithms.

It should not currently be used without independent reference software and domain review for:

- ADMET, hERG, CYP, BBB, toxicity, or clinical-safety assessment;
- lead optimization or medicinal-chemistry decisions;
- PFAS or environmental-remediation design;
- quantitative spectra;
- conformer-energy ranking;
- general thermochemical prediction;
- publication-quality molecular modelling.

## 2. Audit method and evidence

This audit did not accept the repository's own credibility report as independent evidence. Claims were treated as hypotheses and compared with source code and executable behavior.

The audit included:

1. Inventorying high-confidence language across `README.md` and all scientific documentation.
2. Inspecting each major scientific engine and its supporting data structures.
3. Inspecting the complete test suite and the nature of its assertions.
4. Building the entire solution.
5. Running all tests and collecting line and branch coverage.
6. Querying NuGet vulnerability data.
7. Running adversarial examples that were not included in project tests.
8. Comparing documented benchmark responses with actual runtime output.

### Reproduced engineering results

| Check | Result |
|---|---|
| Full solution build | Passed, 0 warnings, 0 errors |
| Unit tests | 114 passed, 0 failed, 0 skipped |
| Core line coverage | 4,036 / 5,088 lines, **79.3%** |
| Core branch coverage | 1,756 / 2,578 branches, **68.1%** |
| Dependency audit | High-severity vulnerable transitive `Microsoft.OpenApi 2.0.0` |
| CI workflow found | No |
| License file found | No, despite an MIT badge |
| Citation metadata found | No `CITATION.cff` |
| Reproducible benchmark generator found | No |

Passing tests demonstrate regression stability over the tested examples. They do not by themselves demonstrate physical accuracy, published-method fidelity, applicability to arbitrary molecules, or predictive validity.

## 3. Claim-to-code scorecard

The following scale is used for individual subsystems:

- **5 — Supported:** implementation and tests support the scoped claim.
- **4 — Mostly supported:** correct core with explicit, manageable limitations.
- **3 — Partially supported:** useful implementation, but important claims or edge cases fail.
- **2 — Weakly supported:** heuristic implementation presented too broadly.
- **1 — Contradicted:** central advertised capability is not present or output is materially misleading.
- **0 — Unsafe/unsupported:** quantitative or safety claim is manufactured without a corresponding model.

| Subsystem | Score / 5 | Claim status | Principal finding |
|---|---:|---|---|
| Elements and molar mass | 4.0 | Mostly supported | Complete 118-element lookup; not an isotope-abundance model |
| Formula parsing | 3.0 | Partially supported | Composition works; formula-derived topology is chemically invented |
| SMILES parsing | 2.0 | Weakly supported | Useful subset, silently drops unsupported syntax and stereochemistry |
| Reaction balancing | 3.0 | Partially supported | Exact arithmetic for nullity-one cases; underdetermined systems crash |
| Stoichiometry | 4.0 | Mostly supported | Sound basic mass/mole arithmetic after a valid balance |
| Solutions chemistry | 4.0 | Mostly supported | Correct ideal monoprotic equations; domain is much narrower than “industrial-grade” |
| Electrochemistry | 4.5 | Mostly supported | Correct basic Nernst calculation with limited thermodynamic scope |
| Basic kinetics | 4.0 | Mostly supported | Standard equations correctly coded |
| Reaction-network integration | 3.0 | Partially supported | RK4 exists; clamping changes the method and validation is incomplete |
| Hückel explicit-matrix solver | 4.0 | Mostly supported | Strong educational implementation and analytical examples |
| Automatic molecular Hückel analysis | 2.5 | Partially supported | Heuristic atom typing and overinterpreted outputs |
| Thermodynamic reference calculations | 2.5 | Partially supported | Hess arithmetic works; identity and phase handling are insufficient |
| “Benson group additivity” | 1.0 | Contradicted | Limited heuristic does not establish a faithful Benson implementation |
| Molecular mechanics | 1.0 | Contradicted | Energy and reported analytical forces are inconsistent |
| 3D conformer generation | 1.5 | Weakly supported | Useful visual coordinates, not validated conformers |
| TPSA/LogP/QED | 1.5 | Contradicted | Simplified rules labeled as full published models; benchmark mismatch |
| ADMET/safety outputs | 0.5 | Unsupported | Fixed thresholds generate medical-sounding classifications |
| Spectroscopy | 1.5 | Weakly supported | Functional-group hints, not molecular spectra |
| Graph matching | 3.0 | Partially supported | Basic injective backtracking, not VF2 |
| Ring perception | 2.0 | Weakly supported | DFS back-edge cycles, not established SSSR/Hansch perception |
| Lead “evolution” | 1.0 | Contradicted | Scripted enumeration with unsupported improvement claims |
| EcoClean | 0.0 | Unsupported | Efficiency, half-life, and mineralization claims are not calculated scientifically |
| Chemical-file export | 2.5 | Partially supported | Basic serialization; conformance and round trips are unproven |
| PubChem client | 3.0 | Partially supported | Simple HTTP lookup; no resilience policy or coverage |
| REST API | 3.0 | Partially supported | Broad surface builds, but lacks integration validation and hardened defaults |

## 4. Detailed findings

### 4.1 Elements, atoms, and molecular composition

#### Supported

The project contains 118 element records and uses frozen dictionaries for symbol and atomic-number lookup. This supports the README's 118-element and constant-time lookup claims.

Molecular weight is calculated as the sum of stored standard atomic weights. Common formula examples are tested and behave as expected.

#### Limitations and contradictions

The documentation expands this into “elemental physics,” “isotopic models,” and exact isotopic-abundance claims. The implementation stores one scalar mass per element. Default neutron counts are derived by rounding this mass and subtracting the atomic number. That is not an isotope distribution, exact isotope mass, or abundance calculation.

The `Element` XML summary calls a record struct “stack-allocated.” A value type is not guaranteed to remain exclusively on the stack; it can be embedded, boxed, captured, or stored in heap objects.

#### Required remediation

- Separate standard atomic weight from isotope mass data.
- Do not derive a chemically meaningful isotope by rounding average atomic weight.
- Add explicit isotope records and provenance if isotope functionality is claimed.
- Remove allocation-location claims that the runtime does not guarantee.

### 4.2 Formula parsing and topology

The formula parser handles nested groups, several bracket types, hydrate separators, multipliers, and charge suffixes. Its compositional functionality is useful.

The critical problem is that an empirical formula does not determine molecular connectivity, yet `AutoGenerateBonds` connects every atom to the first non-hydrogen atom ([`FormulaParser.cs`](../src/Chemy.Core/Parsing/FormulaParser.cs#L117)). For most polyatomic formulas this creates an arbitrary star graph that can violate realistic valence and structure.

Every topology-dependent engine can then consume that invented graph: functional-group detection, 3D coordinates, force field, spectroscopy, ADMET, thermodynamics fallback, and environmental classification.

#### Required remediation

- Introduce a composition-only type distinct from `Molecule`.
- Permit formula input for composition, charge, and molar mass only.
- Require SMILES, Molfile, or explicit bonds for topology-dependent calculations.
- Return an applicability error instead of manufacturing connectivity.

### 4.3 SMILES parser

The parser supports a useful educational subset: atoms, branches, basic bracket atoms, one-digit ring closures, disconnected components, and common bond orders.

It is not standards-compliant and does not fail safely:

- unknown characters fall into an `else` branch and are silently skipped ([`SmilesParser.cs`](../src/Chemy.Core/Structure/SmilesParser.cs#L131));
- unmatched closing branches are ignored;
- unclosed branches and ring openings are not checked at end of input;
- `@`/`@@` stereochemistry and `/`/`\` bond direction are discarded;
- `%nn` multi-digit ring identifiers are unsupported;
- bracket syntax, isotopes, aromatic atoms, valence, and charges are incomplete.

Independent probes showed:

```text
C@C                    -> accepted as C2H6
F[C@](Cl)(Br)I         -> same graph as F[C@@](Cl)(Br)I
```

The documentation says stereochemical symbols are “parsed as standard bonds,” but the implementation actually ignores those characters. This is dangerous because invalid loss of information appears as a successful parse.

#### Required remediation

- Reject every unsupported token with an indexed parse error.
- Validate branch and ring-stack completion.
- Preserve stereochemistry or explicitly refuse stereochemical inputs.
- Add a conformance corpus before claiming SMILES support generally.

### 4.4 Reaction balancing and stoichiometry

The reaction balancer builds an element-and-charge conservation matrix and performs rational row reduction. For the common nullity-one equations in the tests, this is a legitimate exact-arithmetic approach.

The global guarantee of a unique minimal positive solution is too broad. The solver finds only one free column, assigns it one, and leaves other free variables zero ([`MatrixSolver.cs`](../src/Chemy.Core/Reactions/MatrixSolver.cs#L79)). It does not solve for a positive combination of a multi-dimensional nullspace basis.

Independent probe:

```text
C + O2 -> CO + CO2
```

Result:

```text
DivideByZeroException: Rational denominator cannot be zero
```

This equation has a family of positive balances. The library should report ambiguity or return a parameterized/selected balance, not crash internally.

Rational arithmetic also uses `long` multiplication without overflow checks and ultimately converts coefficients to `int`, so “zero rounding errors” does not imply arbitrary-size safety.

Stoichiometric mass and limiting-reagent calculations are conventional and credible after a valid balanced reaction is supplied.

#### Required remediation

- Compute a complete rational nullspace basis.
- Detect nullity and return structured `Unique`, `Ambiguous`, or `Impossible` outcomes.
- Search for a strictly positive primitive integer combination when appropriate.
- Use `BigInteger` and verify `M × x = 0` before returning.

### 4.5 Solutions chemistry

The strong-acid quadratic including water autoionization is implemented correctly for an ideal monoprotic strong acid. The weak-acid cubic is algebraically appropriate for a monoprotic weak acid under the stated idealized equilibrium.

The “industrial-grade” and “arbitrary dilution” language omits important boundaries:

- `Kw` is fixed at `1e-14`;
- `pH + pOH` is fixed at 14;
- activities are replaced with concentrations;
- ionic strength is ignored;
- strong/weak bases and polyprotic systems are absent;
- Halley iteration returns no residual, convergence flag, or failure state;
- Henderson–Hasselbalch is applied without checking buffer capacity or approximation validity.

The core equations are credible educational calculations. They are not a general aqueous-equilibrium engine.

### 4.6 Electrochemistry

The implementation uses:

```text
E = E° - RT/(nF) ln(Q)
```

with appropriate constants and validates positive `n` and `Q`. The documented Daniell-cell numerical example is reproduced.

Limitations include ideal activities, no uncertainty, no electrochemical reaction construction, no standard-potential database, and acceptance of `0 K` despite documentation describing absolute temperature. This is a good Nernst calculator, not an industrial electrochemistry engine.

### 4.7 Kinetics and reaction networks

The elementary half-life and Arrhenius formulas are implemented correctly within their assumed reaction orders.

The general reaction-network method implements the RK4 staging structure. However, it clamps intermediate and final concentrations to zero. That changes the mathematical integrator and can violate conservation or conceal an unstable step. No local-error estimate or adaptive step control exists.

The fixed cascade method has weaker validation than the general method. An independent call with `steps = 0` returned one initial point instead of rejecting the invalid request.

#### Required remediation

- Validate all time, step, rate, shape, and finiteness inputs.
- Separate nominal RK4 from an explicitly named positivity-preserving scheme.
- Add conservation diagnostics and comparison to analytical solutions.
- Add adaptive RK45 for general-purpose use.

### 4.8 Thermodynamics

Hess-law summation is correctly coded once valid species properties are available. The bundled table can reproduce the specific examples it contains.

The broader scientific claims fail for several reasons:

- species are resolved primarily by empirical formula ([`ThermodynamicsEngine.cs`](../src/Chemy.Core/Thermodynamics/ThermodynamicsEngine.cs#L90));
- structural isomers therefore receive the same data;
- phases are absent, despite large phase dependence in enthalpy and entropy;
- temperature dependence and heat capacities are absent;
- the source/provenance of individual table values is not machine-readable;
- the table mixes aliases and formula strings rather than stable chemical identities.

The audit demonstrated that ethanol and dimethyl ether are indistinguishable when resolved as `C2H6O`.

The fallback labeled “true Benson group additivity” begins with:

```csharp
double s = 150.0 + (1.5 * 8.314 * Math.Log(Math.Max(10.0, molecule.MolecularWeight)));
```

([`ThermodynamicsEngine.cs`](../src/Chemy.Core/Thermodynamics/ThermodynamicsEngine.cs#L98)) and then applies a small set of local rules. This is not evidence of a complete Benson group-definition and correction system. Its entropy and Gibbs calculations are particularly weakly founded.

#### Required remediation

- Identify species by structure, charge, phase, and reference state.
- Store source, uncertainty, temperature, and phase for every datum.
- Remove fallback estimates until a complete, testable group scheme exists.
- Validate against a broad external thermochemical dataset with MAE/RMSE.

### 4.9 Hückel molecular-orbital engine

This subsystem has genuine strengths. The explicit-matrix API constructs and diagonalizes symmetric Hamiltonians, assigns orbital occupancy, and computes eigenvalue-based properties. Tests reproduce analytical eigenvalues and total π energies for standard classroom systems.

The documentation nevertheless overstates the result:

- Jacobi diagonalization is numerical, not exact;
- automatic conjugated-atom detection is heuristic;
- the heteroatom parameters are not supplied with a versioned, citable parameter table;
- degeneracy and open-shell occupation are handled by sequential filling rather than a rigorous state treatment;
- the UV-visible estimate is simply `hc / orbital gap`, with no transition dipole, intensity, selection rules, vibronic effects, solvent, or geometry relaxation;
- “Fukui indices” are frontier-orbital coefficient squares, not validated condensed Fukui functions from charged-state calculations;
- resonance-energy conversion uses a simplified reference and should not be read as experimental stabilization energy.

#### Credible scope

The engine is credible as a deterministic educational Hückel calculator for supported π systems, especially through its explicit-matrix API. It is not general electronic-structure or quantitative spectroscopy software.

### 4.10 Molecular mechanics

This is a confirmed mathematical contradiction.

The energy function excludes both 1–2 and 1–3 nonbonded pairs ([`ForceFieldEngine.cs`](../src/Chemy.Core/Physics/ForceFieldEngine.cs#L201)). The force function excludes only directly bonded 1–2 pairs ([`ForceFieldEngine.cs`](../src/Chemy.Core/Physics/ForceFieldEngine.cs#L390)). Consequently, the returned forces cannot be the exact gradient of the returned energy.

Additional inconsistencies:

- bond force includes an unexplained `0.001` factor;
- angle force includes `0.0005`;
- torsion force includes `0.001`;
- van der Waals forces are clamped;
- total energy is clamped to be nonnegative, despite an attractive Lennard-Jones term;
- parameters are global constants rather than atom types;
- electrostatics are absent;
- the code is described as UFF/MMFF without implementing either parameterization.

The minimizer always constructs the result with `Converged = true`, regardless of the exit reason. With `maxIterations = 0`, it reported one iteration, unchanged energy, and successful convergence.

The tests check nonnegative energy, atom count, and the hard-coded convergence flag. They do not perform finite-difference gradient verification, known-minimum validation, invariance tests, or parameter comparisons.

#### Required remediation

- Derive forces from the exact implemented energy or use verified numerical gradients.
- Use identical exclusions and scaling in energy and forces.
- report termination reason, gradient norm, and genuine convergence;
- preserve signed energy;
- remove UFF/MMFF names unless the published atom typing and parameters are implemented;
- add finite-difference tests for every energy term.

### 4.11 3D geometry and conformers

The geometry engine contains deterministic VSEPR templates and graph-layout heuristics. Water's fixed coordinates reproduce the expected displayed bond length and angle. Planar layouts are useful for visualization.

For general molecules, coordinates are produced through regular polygons, breadth-first propagation, fixed bond lengths and angles, simple local frames, and the generalized force field. Remaining atoms can be placed on a straight fallback line.

This is an initial-coordinate and diagram generator, not a validated conformer generator. It does not provide:

- stereochemical constraints;
- distance geometry;
- systematic torsion sampling;
- multiple conformers;
- ring-template validation;
- energy/RMSD ranking;
- comparison to experimental or trusted geometries.

“Physically valid 3D coordinates” and “accurate conformers” should be replaced by “heuristic visualization coordinates.”

### 4.12 TPSA, LogP, hydrogen bonding, and QED

The implementation does not contain the complete advertised published methods.

#### TPSA

The code uses a compact element-and-neighbor decision tree. It does not demonstrate exhaustive assignment of the published fragment types. Some comments describe whole group values as though they were atomic contributions, creating double-counting risk.

The documentation claims ibuprofen TPSA is 34.1 Å². Runtime output was 37.3 Å². The documentation even states `COOH = 37.3 Å² ⇒ 34.1 Å²`, an internal numerical contradiction.

#### LogP

The code contains a small set of approximate atom contributions, not a demonstrated complete Wildman–Crippen atom-typing implementation. Runtime ibuprofen LogP was 2.88, whereas the claimed live response is 4.00.

#### Hydrogen-bond acceptors

The implementation counts every O, N, or F atom as an acceptor ([`AdmetEngine.cs`](../src/Chemy.Core/Pharmacology/AdmetEngine.cs#L430)). This mishandles standard cases including amide nitrogen, pyrrolic nitrogen, protonated/quaternary nitrogen, acidic hydroxyls, and organic fluorine.

#### QED

The code uses one simplified sigmoid form across descriptors with a small parameter set. It does not establish faithful reproduction of the published asymmetric double-sigmoid functions and complete alert definitions. Structural alerts contain only a few broad rules rather than the advertised PAINS/Brenk system.

#### Required remediation

- Implement versioned, exhaustive atom types and contribution tables.
- Return per-atom typing and fail or warn on untyped environments.
- Validate hundreds or thousands of structures against a trusted implementation.
- Publish error distributions, not selected examples.

### 4.13 ADMET and safety language

The result includes hERG risk, CYP metabolism, and BBB permeability fields. These are produced by a few fixed thresholds and functional-group checks, not trained or validated predictive models.

For audited ibuprofen input, runtime output included:

```text
Moderate Risk (Monitor hERG patch clamp in vitro)
CYP1A2 / CYP2C9: Aromatic para-hydroxylation
High BBB Permeability (CNS Active)
```

No hERG model, CYP substrate model, BBB classifier, training dataset, calibration, applicability domain, uncertainty, or external validation exists. “Low Risk (Normal cardiac safety window)” is particularly unsafe wording because it can be mistaken for a safety conclusion.

#### Required remediation

- Remove these fields or label them explicitly as non-predictive heuristics.
- Prefer descriptor and rule outputs without medical conclusions.
- If predictive models are later added, version their parameters, datasets, applicability domain, probability calibration, and uncertainty.

### 4.14 Spectroscopy

The engine assigns fixed peaks and bands when functional groups are detected. It does not calculate shielding tensors, equivalence classes, coupling networks, normal modes, or solvent-dependent spectra.

The documented acetone benchmark claims one six-proton singlet at 2.15 ppm. Actual output was:

```text
3H singlet at 2.15 ppm
3H triplet at 1.15 ppm
```

The discrepancy follows directly from adding a fixed three-hydrogen ketone peak and assigning all remaining protons to a generic aliphatic peak ([`SpectroscopyEngine.cs`](../src/Chemy.Core/Spectroscopy/SpectroscopyEngine.cs#L143)). Aromatic hydrogen counts are capped at five, and carbon environments are similarly assigned by broad remainder rules.

This is a functional-group hint engine. Calling it complete NMR/IR prediction is not credible.

### 4.15 Graph matching, ring perception, and rewriting

The subgraph matcher performs injective backtracking with element and required-bond checks. It is useful for small motifs, but it is not the VF2 algorithm claimed by the evolution-engine documentation. It lacks query atom/bond expressions, aromatic normalization, charge/isotope matching, stereochemistry, and advanced pruning.

Ring perception uses DFS back edges ([`ChemicalGraph.cs`](../src/Chemy.Core/Graph/ChemicalGraph.cs#L108)). This is not a demonstrated SSSR/Hansch implementation and can behave poorly on fused, bridged, and polycyclic graphs.

Graph rewriting performs concrete edits, but chemical validity is not comprehensively checked after mutation. Formula equality is sometimes used as structural identity, collapsing isomers.

### 4.16 Molecular evolution and lead optimization

The engine is deterministic scripted analogue enumeration, not a demonstrated genetic algorithm:

- no random population;
- no crossover;
- no fitness-based parent selection;
- no diversity or Pareto treatment;
- no canonical structural identity;
- at most a small fixed set of mutation types;
- baseline duplicates are added until five candidates exist;
- requested generation count is reported even if little search occurred.

The code makes unsupported causal claims, including eliminating hepatotoxicity, blocking CYP3A4 metabolism, increasing plasma half-life, improving receptor binding, improving bioavailability, and minimizing off-target toxicity ([`MolecularEvolverEngine.cs`](../src/Chemy.Core/Evolution/MolecularEvolverEngine.cs#L97)). None of those endpoints is modelled.

Outputs should be called enumerated structural hypotheses, not optimized or non-toxic leads.

### 4.17 EcoClean environmental engine

This is the highest-severity credibility problem.

The engine classifies a molecule based mainly on elements and detected functional groups, then returns predefined pathway narratives, catalysts, environmental half-lives, products, and efficiencies.

The reported mineralization efficiency is calculated as:

```csharp
double efficiency = Math.Round(
    99.0 + Math.Clamp(10.0 / secondaryBde, 0.2, 0.8), 1);
```

([`EcoCleanEngine.cs`](../src/Chemy.Core/Environmental/EcoCleanEngine.cs#L268)). This formula guarantees a result near 99% and is not derived from kinetics, conversion data, catalyst loading, pH, temperature, medium, energy input, competing pathways, or experimental calibration.

Environmental half-lives are fixed class constants such as 1,000 years for PFAS and 450 years for synthetic polyesters. End products are assembled from the elements present and labeled “100% Mineralized” without a balanced reaction.

The dedicated test only requires efficiency greater than 90%, thereby testing the hard-coded assertion rather than scientific validity.

#### Required remediation

- Remove quantitative efficiency and half-life outputs immediately.
- Remove “non-toxic,” “100% mineralized,” and catalyst recommendation language.
- Recast results as qualitative hypotheses with required conditions and evidence provenance.
- Introduce quantitative outputs only after explicit reaction, kinetic, mass-balance, and calibration models exist.

### 4.18 Chemical file interoperability

The project emits plausible basic V2000 Molfile, SDF, XYZ, and PDB-like text. The Molfile exporter writes headers, counts, atom coordinates, bonds, and `M  END`.

The “ISO/IUPAC-compliant” and “full support” claims are not demonstrated:

- no `M  CHG` formal-charge records;
- no `M  ISO` isotope records;
- no stereochemical bond flags;
- no V2000 limit enforcement;
- no parser despite the class calling itself a serializer and parser;
- no round-trip test;
- no independent ChemDraw/RDKit/PyMOL conformance test;
- PDB output is molecule-oriented text, not broad PDB/mmCIF structural support.

The correct claim is basic export for a limited internal molecule model.

### 4.19 PubChem client and REST API

The PubChem client performs a URL-escaped PUG REST request and deserializes selected fields. It is a useful basic integration.

It is described as resilient, but it has no explicit retry/backoff policy, status diagnostics, caching, rate-limit handling, or tests. A broad catch converts cancellation, network errors, schema errors, and programming errors into `null`, making failures opaque.

The API builds and exposes many endpoints, but there are no integration tests validating routing, serialization, OpenAPI, error responses, request limits, or health behavior. `/healthz` is registered both through health-check middleware and an explicit route, so the documented JSON route may be shadowed by middleware ordering.

The default CORS policy allows every origin, header, and method. That is convenient for development, not an “enterprise” production default.

## 5. Test-suite assessment

The test suite is a genuine positive asset. It covers numerous normal examples and caught regressions across parsers, rendering, equations, graph functions, and Hückel matrices.

Its scientific-verification power is much weaker than the documentation claims.

### Strengths

- 114 tests pass deterministically.
- Good analytical examples exist for explicit Hückel matrices.
- Common reaction balances and formula calculations are covered.
- Several invalid formula cases are tested.
- Core coverage is substantial for a young project.

### Weaknesses

- Many assertions check non-null, nonnegative, or broad self-selected ranges.
- Benchmarks are written manually rather than regenerated by executable scripts.
- Selected expected values often come from the same assumptions as the implementation.
- No differential testing against trusted chemistry libraries.
- No large public molecule/property datasets.
- No MAE, RMSE, calibration, confidence, or applicability-domain metrics.
- No force-gradient finite-difference checks.
- No numerical convergence-order studies.
- No property-based or fuzz testing of parsers and graph algorithms.
- No API integration tests.
- PubChem, JSON converters, and some reaction-network paths had zero measured coverage.

### Examples of weak validation

- The force-field water test only requires nonnegative energy and the correct formula.
- The butane test accepts the hard-coded `Converged = true` value.
- The EcoClean test requires a result above 90%, although the implementation manufactures approximately 99%.
- Aspirin TPSA is accepted within a broad range rather than checked against a faithful reference implementation.

## 6. Documentation and engineering integrity

The repository's own scientific credibility report is not an independent audit. It assigns multiple 10/10 ratings without specifying an auditor, reproducible methodology, dataset, error metric, confidence interval, or revision-linked result artifact.

Confirmed documentation/configuration contradictions include:

| Documentation claim | Repository evidence |
|---|---|
| Warnings treated as errors | `TreatWarningsAsErrors` is `false` in [`Directory.Build.props`](../src/Directory.Build.props#L5) |
| 100% scientific verification | Coverage is 79.3% line / 68.1% branch; coverage itself is not scientific validation |
| Zero dependency | API directly depends on Microsoft OpenAPI, Scalar, and Swashbuckle packages |
| Zero allocations | Lists, arrays, strings, dictionaries, immutable collections, records, and LINQ allocate throughout |
| MIT licensed | Badge exists, but no license file was found |
| No vulnerable dependencies implied by clean build | `NU1903` is suppressed and a high-severity vulnerable transitive package is present |
| Live benchmark parity | Ibuprofen and acetone documented outputs differ from runtime output |
| Complete end-to-end verification | Several major components have no independent validation or integration tests |

The API project explicitly suppresses `NU1903` ([`Chemy.Api.csproj`](../src/Chemy.Api/Chemy.Api.csproj#L7)). NuGet reported high-severity advisory `GHSA-v5pm-xwqc-g5wc` for transitive `Microsoft.OpenApi 2.0.0` during this audit.

## 7. Risk register

| Severity | Risk | Impact |
|---|---|---|
| Critical | EcoClean reports near-99% mineralization and “non-toxic” products without a quantitative model | Could mislead environmental decisions |
| Critical | ADMET returns medical-sounding hERG/BBB/CYP classifications without validated models | Could be mistaken for safety evidence |
| High | Force field reports convergence and exact gradients contrary to implementation | Invalid geometries or energies may appear trustworthy |
| High | Formula-derived invented topology feeds downstream scientific engines | Chemically meaningless inputs can produce confident outputs |
| High | Thermodynamics conflates structural isomers and omits phases | Reaction feasibility can be materially wrong |
| High | Vulnerability warning is suppressed | Security problem is hidden behind a clean build |
| High | SMILES silently discards stereochemical/unknown syntax | Successful result may represent a different molecule |
| Medium | Reaction solver crashes on underdetermined valid systems | Reliability and “arbitrary” claims are false |
| Medium | Benchmark documents do not reproduce runtime outputs | Users cannot trust published verification tables |
| Medium | File-format compliance is claimed without conformance testing | Interoperability failures and information loss |
| Medium | No CI or revision-linked benchmark artifacts | Results can drift without visibility |

## 8. Prioritized remediation roadmap

### Priority 0 — stop misleading output

1. Remove EcoClean efficiency, half-life, “100% mineralized,” “non-toxic,” and catalyst-recommendation claims.
2. Remove or explicitly label hERG, CYP, BBB, toxicity, and clinical-safety fields as unsupported heuristics.
3. Add method, version, evidence level, applicability domain, warnings, and uncertainty to every scientific result.
4. Return `Unsupported` rather than a confident value outside a model's domain.
5. Replace “industrial-grade,” “verified,” “exact,” and “complete” language unless a claim is demonstrably scoped.

### Priority 1 — establish valid molecular identity

1. Separate empirical composition from bonded molecular structure.
2. Prevent topology-dependent engines from consuming formula-invented graphs.
3. Make SMILES parsing strict: reject unsupported syntax, unmatched branches, and unclosed rings.
4. Implement or preserve stereochemistry before accepting stereochemical inputs.
5. Use canonical structural identity rather than formula for candidate deduplication.

### Priority 2 — repair mathematical defects

1. Make force calculations the verified negative gradient of the energy.
2. Add real convergence and termination reporting.
3. Implement complete rational nullspace handling with `BigInteger`.
4. Validate all kinetic-network inputs and expose solver residuals/conservation errors.
5. Stop clamping physical/numerical quantities without an explicit method designation.

### Priority 3 — implement published methods faithfully

1. Implement exhaustive Ertl TPSA atom types with per-atom assignments.
2. Implement complete Wildman–Crippen atom typing and corrections.
3. Implement the published QED desirability equations and alert definitions.
4. Replace the thermodynamic fallback with a complete, phase-aware, provenance-backed method or remove it.
5. Separate VSEPR/diagram coordinates from genuine conformer generation.

### Priority 4 — make validation reproducible

1. Add versioned public reference datasets.
2. Generate every benchmark table from executable commands.
3. Differentially test descriptors and formats against trusted implementations.
4. Report MAE/RMSE, error distributions, failures, and applicability coverage.
5. Add finite-difference gradient tests, numerical convergence tests, parser fuzzing, and API integration tests.
6. Add CI that builds, tests, audits packages, and publishes benchmark artifacts.

### Priority 5 — engineering governance

1. Upgrade the vulnerable dependency and remove `NU1903` suppression.
2. Enable warnings as errors if the README claims it.
3. Add an actual license file or remove the badge.
4. Add `CITATION.cff`, contribution guidance, model/data provenance, and versioned release notes.
5. Establish review by domain experts for high-stakes scientific modules.

## 9. Definition of scientific readiness

Chemy should not describe a subsystem as scientifically validated until all of the following are true:

- the implemented method is precisely named and versioned;
- the implementation matches a primary specification or publication;
- units, assumptions, reference states, and applicability domain are explicit;
- unsupported inputs fail visibly;
- numerical convergence and residuals are reported where relevant;
- outputs are compared against an independent, versioned reference dataset;
- errors are summarized across the dataset, not only selected examples;
- benchmark generation is reproducible from the audited revision;
- uncertainty and known failure modes are documented;
- high-stakes claims receive qualified domain review.

## 10. Final assessment

Chemy has credible foundations for an educational chemistry toolkit and contains more substantive implementation work than its most serious flaws might initially suggest. The periodic table, formula arithmetic, elementary kinetics, Nernst equation, common reaction balancing, rendering, and explicit Hückel examples provide a useful base.

The present solution loses credibility when it treats determinism as accuracy, passing unit tests as external validation, equations as proof of general applicability, and rule-generated prose as scientific prediction. Those are fixable engineering and governance problems, but the fixes require both code changes and independent validation evidence.

The most accurate current product description is:

> **An experimental and educational .NET chemistry toolkit containing textbook calculators, molecular graph utilities, simplified Hückel analysis, heuristic visualization, and rule-based property demonstrations.**

Under that description, the project is useful and improvable. Under the current claims of industrial chemoinformatics, validated lead optimization, universal thermodynamics, drug safety, and environmental mineralization, the repository does not yet provide sufficient evidence for scientific credibility.

# Chemy Scientific Credibility Audit — v1.1

**Audit date:** 2026-08-21  
**Audited revision:** `a106d55` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md), revision `236fac3`  
**Auditor:** OpenAI Codex  
**Scope:** Documentation claims, scientific implementation fidelity, public execution paths, automated tests, numerical behavior, dependency health, applicability controls, and reproducibility evidence.

## 1. Executive conclusion

Chemy has improved as a software project since audit v1.0. The repository now has a license, citation metadata, a CI workflow, scientific-method metadata, exact rational nullspace-basis code, a minimum-cycle-basis implementation, temperature-dependent Shomate calculations for seven gases, topological symmetry partitioning, more spectroscopy output, a consistent finite-difference force-field gradient, and 13 additional passing tests.

Those are real improvements. They do **not**, however, close the main credibility gap identified in v1.0: the public documentation continues to present partial, heuristic, or unvalidated calculations as complete published methods and as evidence for drug safety, lead optimization, arbitrary thermodynamics, quantitative spectroscopy, and environmental mineralization.

The most important new finding is that several additions have the *names and citations* of established scientific methods without implementing their defining scope:

- the “43-fragment Ertl TPSA” classifier is not an exhaustive 43-fragment implementation and double-counts amide polar surface area;
- the “complete 68-parameter Wildman–Crippen” classifier implements only a small subset of atom environments;
- the QED equation is substantially closer to the published ADS form, but its input descriptors and structural-alert system are not faithful enough to call the resulting score an exact QED implementation;
- the force field now differentiates its own energy consistently, but it is still not a full UFF parameterization or UFF functional form;
- the Horton/SSSR and exact nullspace additions are not consistently used by the older public paths that make the corresponding claims;
- the Shomate implementation is a useful seven-species standalone calculator, not the phase-aware thermodynamic backend advertised for arbitrary compounds.

### Credibility ratings

| Context | v1.0 | v1.1 | Interpretation at v1.1 |
|---|---:|---:|---|
| Software implementation quality | 6.0 | **6.6 / 10** | Better engineering controls and algorithms; important validation and API integration defects remain |
| Chemistry education and demonstrations | 7.0 | **7.2 / 10** | Useful when explicitly presented as simplified and independently checked |
| Developer prototyping | 6.0 | **6.5 / 10** | A substantial experimental .NET codebase with clearer method metadata |
| Quantitative scientific analysis | 3.0 | **3.4 / 10** | Narrow equations are useful, but published-method fidelity remains weak in major additions |
| Research/publication use | 2.0 | **2.3 / 10** | Still lacks external validation corpora, error statistics, uncertainty, and revision-linked benchmarks |
| Drug-safety/environmental decisions | 1.0 | **1.0 / 10** | The two critical unsupported decision-oriented outputs remain |
| **Overall claim-adjusted credibility** | **4.5** | **4.9 / 10** | Genuine progress, but claims still materially exceed demonstrated scientific validity |

The rating is a structured engineering judgment, not a statistical confidence interval. It weights implementation fidelity, external validation, applicability controls, failure behavior, reproducibility, and the harm possible when a result is overinterpreted. The modest increase reflects real engineering progress; it is capped by unchanged critical safety and environmental claims.

### Usage recommendation

Chemy is currently reasonable for:

- chemistry education where each model's simplifications are taught;
- API/UI prototyping and algorithm demonstrations;
- formula arithmetic, common reaction balancing, elementary kinetics, Nernst calculations, and explicit Hückel examples within their tested domains;
- exploratory results that are verified with established reference software.

It is not currently supported as sole evidence for:

- hERG, CYP, BBB, toxicity, pharmacokinetics, or clinical safety;
- medicinal-chemistry lead selection or claims that a mutation reduces toxicity;
- environmental-remediation efficiency, half-life, products, or catalyst selection;
- publication-quality LogP, TPSA, QED, spectra, conformer energies, or general thermochemistry;
- stereochemistry-sensitive or standards-compliant molecular interchange.

## 2. What changed since v1.0

The audited commit adds or changes approximately 2,960 lines and removes approximately 900 lines. The most material changes are:

| Area | Change | Audit disposition |
|---|---|---|
| Governance | Added `LICENSE` and `CITATION.cff` | **Resolved** |
| Automation | Added GitHub Actions build/test workflow | **Partially resolved**: CI exists, but no coverage, vulnerability, benchmark, or scientific-reference gates |
| Scientific metadata | Added `ScientificMethodInfo` with method, version, evidence level, applicability, and warnings | **Improved**: useful schema, but labels sometimes overstate implementation fidelity |
| Reaction algebra | Added exact `BigInteger` rational nullspace basis | **Partially resolved**: basis works directly; public `Reaction.Balance()` still rejects nullity greater than one |
| Ring perception | Added minimum-cycle-basis/SSSR engine | **Partially resolved**: simple tests pass; older consumers still use `ChemicalGraph.FindRings()` |
| TPSA | Added per-atom `ErtlTpsa` results | **Not scientifically resolved**: incomplete/misassigned atom typing |
| LogP/MR | Added per-atom `WildmanCrippenLogP` results | **Not scientifically resolved**: incomplete atom-type table and fallback behavior |
| QED | Added ADS desirability calculation and per-descriptor output | **Partially resolved**: equation improved; descriptors and alerts are not faithful |
| Force field | Unified energy and central finite-difference gradients; added termination metadata | **Substantially improved**, but still not full UFF and convergence reporting remains defective |
| Spectroscopy | Added WL grouping and 13C output | **Improved educational heuristic**, not quantitative spectrum prediction |
| Thermodynamics | Added seven-species Shomate database | **Useful narrow addition**, not integrated into advertised general thermodynamics |

## 3. Reproduced engineering evidence

| Check | v1.0 | v1.1 result |
|---|---:|---:|
| Full automated tests | 114 passed | **127 passed, 0 failed, 0 skipped** |
| Core line coverage | 79.3% | **79.96%** (4,642 / 5,805) |
| Core branch coverage | 68.1% | **69.84%** (1,979 / 2,834) |
| CI workflow | Absent | **Present** |
| License | Absent despite badge | **Present** |
| Citation metadata | Absent | **Present** |
| Vulnerable dependencies | High-severity transitive `Microsoft.OpenApi 2.0.0` | **Still present**, GHSA-v5pm-xwqc-g5wc |
| Externally generated reference dataset | None | **None found** |
| Revision-linked benchmark artifact | None | **None found** |

Passing project tests establish regression consistency for selected examples. They do not establish agreement with an independent implementation, experimental dataset, or published benchmark distribution.

### Independent execution probes

The following behaviors were reproduced against revision `a106d55` through public APIs:

```text
Ibuprofen:
  LogP = 3.42; TPSA = 37.30; QED = 0.574
  hERG = "Moderate Risk (Monitor hERG patch clamp in vitro)"
  CYP = "CYP1A2 / CYP2C9: Aromatic para-hydroxylation"
  BBB = "High BBB Permeability (CNS Active)"

Acetone 1H NMR:
  one 6H singlet at 2.17 ppm

C + O2 -> CO + CO2:
  Reaction.Balance() throws InvalidOperationException

F[C@](Cl)(Br)I and F[C@@](Cl)(Br)I:
  identical formula and identical bonds

C@C:
  accepted as C2H6

PFOA-like formula C8HF15O2:
  reported mineralization efficiency = 99.2%
```

The ibuprofen output also does not reproduce the repository's still-published benchmark of LogP 4.00 and TPSA 34.1 Å². The runtime TPSA is close to the accepted carboxylic-acid value, but the documentation and executable result disagree.

## 4. Updated claim-to-code scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.0 | v1.1 | Current finding |
|---|---:|---:|---|
| Elements and molar mass | 4.0 | **4.0** | Unchanged; good standard-weight lookup, not isotope modelling |
| Formula parsing | 3.0 | **3.0** | Composition is useful; formula-derived topology remains invented |
| SMILES parsing | 2.0 | **2.0** | Unsupported stereochemistry is still silently discarded |
| Reaction balancing | 3.0 | **3.5** | Exact basis exists; public balancing still fails on underdetermined reactions |
| Stoichiometry | 4.0 | **4.0** | Credible once supplied a valid balance |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Narrow textbook calculations remain the strongest scientific area |
| Reaction-network integration | 3.0 | **3.0** | Unchanged numerical and validation limitations |
| Explicit Hückel solver | 4.0 | **4.0** | Useful educational numerical solver; “exact” remains inaccurate wording |
| Automatic molecular Hückel analysis | 2.5 | **2.5** | Heuristic typing and overinterpreted observables remain |
| Reference thermodynamics | 2.5 | **3.0** | Seven Shomate gas records are useful but standalone and range handling is defective |
| Benson fallback | 1.0 | **1.0** | Still a small heuristic described as “true” and applicable to arbitrary organics |
| Molecular mechanics | 1.0 | **2.5** | Energy/gradient consistency fixed; parameterization and functional form are not complete UFF |
| 3D conformer generation | 1.5 | **2.0** | Better relaxation backend, but no conformer search or geometry validation corpus |
| TPSA | 1.5 combined | **1.5** | Named 43-fragment implementation is demonstrably incomplete |
| LogP/MR | 1.5 combined | **1.5** | Named 68-parameter implementation is demonstrably incomplete |
| QED | 1.5 combined | **2.5** | ADS structure improved; bad inputs and four alerts prevent faithful QED |
| ADMET/safety | 0.5 | **0.5** | Unsupported medical-sounding hERG/CYP/BBB classifications remain |
| Spectroscopy | 1.5 | **2.0** | Better equivalence grouping; fixed lookup shifts are not a complete spectrum model |
| Graph matching | 3.0 | **3.0** | Basic injective backtracking remains, not VF2 |
| Ring perception | 2.0 | **3.0** | new basis is promising; algorithm/consumer integration and complex validation are incomplete |
| Lead evolution | 1.0 | **1.0** | Unsupported causal optimization/toxicity claims remain |
| EcoClean | 0.0 | **0.0** | Near-99% efficiency and harmless-product claims remain manufactured |
| File export/PubChem/API | 2.5–3.0 | **2.5–3.0** | No meaningful change or conformance/integration evidence |

## 5. Deep findings on the new scientific code

### 5.1 Ertl TPSA: the 43-fragment claim is not implemented

`ErtlTpsa` is documented as an “exhaustive implementation” of the 43-fragment method. Its classifier instead branches on a small set of attributes: element, explicit hydrogen count, numbers of double/triple/aromatic bonds, charge, and a three-membered-ring check. It does not encode or select among the complete published fragment definitions.

The amide test exposes a material chemical error. It expects acetamide to exceed 50 Å² and describes the result as carbonyl oxygen `17.07` plus amide nitrogen `43.09`, producing `60.16 Å²`. Reference implementations give acetamide TPSA near **43.09 Å² total**. The code has treated an environment-level value as an additional atom contribution and double-counted the carbonyl contribution. The test therefore validates the implementation's error rather than the reference method.

Other limitations include incomplete formal-valence/aromatic handling, reliance on explicit hydrogens, no exhaustive phosphorus/sulfur environments, and no failure when an atom environment is unsupported. Applicability metadata says the method covers common halogens even though halogens are not TPSA-contributing atom types and the actual limitation is supported polar-atom environments.

**Disposition:** per-atom output is good engineering, but the result must not be labeled Ertl 43-fragment TPSA until all types are implemented and checked against a large independent corpus.

### 5.2 Wildman–Crippen LogP/MR: small heuristic table labeled as 68 parameters

The class claims a “complete 68-parameter” calculator, but the classifier returns only a small collection of coarse environments. Aromatic carbon, for example, is reduced to C18/C19/C20; many carbon, nitrogen, oxygen, sulfur, phosphorus, and hydrogen subtypes are missing. Unknown elements silently contribute zero, while unsupported known environments are forced into a nearby coarse type.

The implementation also identifies atom environments using immediate neighbors and explicit hydrogen counts. This is insufficient for the published SMARTS-like typing rules and makes output dependent on parser representation. A trustworthy implementation needs a complete, reviewable type table, deterministic precedence, an explicit unsupported-type failure, and cross-validation against an established implementation over diverse molecules.

The new ibuprofen value is 3.42, improved from the previous 2.88 but still inconsistent with the repository's documented 4.00 response. That discrepancy is not quantified or acknowledged.

**Disposition:** useful atom-additive teaching model; not a faithful Wildman–Crippen implementation.

### 5.3 Bickerton QED: correct-looking equation, non-equivalent model inputs

The ADS equation and weighted geometric mean are now structurally close to the published QED method. This is a meaningful improvement. The complete calculation is nevertheless not equivalent because:

- it consumes the incomplete LogP and erroneous TPSA implementations;
- H-bond acceptors count every oxygen and most nitrogens, including environments that should be excluded;
- rotatable bonds do not exclude amide C–N and other restricted bonds;
- aromatic rings are inferred from aromatic-atom count thresholds rather than an actual aromatic ring basis;
- structural alerts contain four handwritten patterns rather than the published alert definitions;
- alert desirability is replaced by a custom three-level function rather than applying the ADS parameters already declared for alerts.

The aspirin test merely asserts that the score lies between 0.10 and 0.95. That would pass for a very broad range of incorrect implementations. No exact reference values are asserted.

**Disposition:** call it a QED-inspired score until every input descriptor and alert definition is reference-compatible.

### 5.4 Force field: consistency repaired, UFF fidelity not established

The most important v1.0 force-field defect was fixed: the optimizer now obtains central finite-difference derivatives directly from the same total-energy function it minimizes. Termination reason and final gradient norm are also exposed. This removes the previous contradiction between the reported energy and forces.

It is still not an authentic general UFF implementation:

- the parameter table contains only a small subset of UFF atom types, despite claiming coverage from H to Lw;
- lookup is mostly by element symbol, so hybridization-specific entries such as `C_2`, `C_R`, and `C_1` are not selected;
- unsupported elements receive a generic fabricated parameter record instead of an applicability error;
- angle bending uses a fixed harmonic constant of 100 rather than UFF's parameterized angular form;
- torsions use the same `2.5`, threefold cosine for essentially every central bond;
- inversion and electrostatic terms/typing rules are absent;
- nonbonding introduces a custom soft core and clash penalty, changing the published potential;
- “converged” becomes true after any energy decrease and ten iterations even when termination is maximum-iterations or line-search exhaustion;
- `maxIterations = 0` reports one iteration, an off-by-one defect.

The finite-difference gradient is internally consistent, not “exact analytical gradients” as the README states. A numerical derivative can be accurate, but it is neither analytical nor proof that the potential is UFF.

**Disposition:** substantial engineering improvement. Rename to a UFF-inspired teaching force field or complete atom typing and published functional terms, then validate energies/geometries against a reference implementation.

### 5.5 Exact nullspace basis: good primitive, incomplete public behavior

`MatrixSolver.SolveNullspaceBasis` now uses `BigInteger` rational arithmetic and correctly returns a two-dimensional basis for `C + O2 -> CO + CO2`. This directly resolves the arithmetic/basis portion of the v1.0 finding.

`Reaction.Balance()` still calls the single-vector API, which returns `null` whenever nullity is not one. The public reaction therefore still throws `InvalidOperationException` on that same valid underdetermined system. Basis vectors may also contain mixed signs or zeros and are cast back to `int` without overflow checks. Finding a minimal strictly positive combination of a multi-dimensional basis is a separate problem and remains unimplemented.

**Disposition:** primitive improved; advertised arbitrary balancing path not resolved.

### 5.6 Cycle basis and WL symmetry

The new cycle code constructs shortest-path candidates and greedily selects GF(2)-independent cycles. Benzene and naphthalene tests are useful smoke tests. The implementation is plausibly useful as a minimum-cycle-basis routine, but the audit found insufficient evidence for the broader “authentic SSSR” claim:

- only two simple aromatic examples are tested;
- no bridged, spiro, cubane-like, disconnected, multigraph, or multiple-equal-basis cases are checked;
- linear independence elimination assumes existing rows are already in a suitable echelon order;
- SSSR and minimum cycle basis are related but not interchangeable terminology in all chemical ring-perception contexts;
- `AdmetEngine.CountAromaticRings` still calls the older `ChemicalGraph.FindRings()` implementation rather than `CycleBasis.ComputeSssr()`.

The Weisfeiler–Lehman partition is useful for topological grouping but must not be described as a complete chemical symmetry solver. 1-WL can merge non-equivalent vertices in some graphs and cannot distinguish stereotopic/enantiotopic/diastereotopic nuclei. Because the parser erases stereochemistry, the spectroscopy path cannot recover it.

**Disposition:** good graph-algorithm additions; scope claims and integrate them consistently.

### 5.7 Shomate thermodynamics: narrow useful implementation with range defect

The equations for heat capacity, enthalpy, and entropy are recognizable Shomate equations and the database contains seven common gas-phase species. Tests check approximate H2O and CO2 values. This is a useful, bounded feature.

It is not connected to `ThermodynamicsEngine`, which continues to use its old small reference table and heuristic Benson fallback. It therefore does not repair the advertised “NIST reference tables with Benson Group Additivity for arbitrary organics” path.

There is also a concrete range-handling bug: the evaluation temperature used in the polynomial is silently clamped to the coefficient range, but the result reports the original requested temperature and uses that original value in `G = H - TS`. Thus an out-of-range request combines clamped H/S with unclamped T and returns an internally inconsistent state. Each species also has only one nominal coefficient range, whereas real Shomate records often require multiple temperature intervals.

**Disposition:** retain as an explicitly seven-species gas calculator, reject out-of-range temperatures, add interval-specific records/provenance, and integrate only where phase/species identity is exact.

### 5.8 Spectroscopy: better topology, still lookup-based hints

WL grouping fixes the previous acetone-equivalence failure: the engine now returns one six-proton singlet. It also adds 13C output and a clearer result schema.

The chemical shifts are still fixed constants selected by a handful of local rules. Multiplicity counts adjacent explicit hydrogens using a first-order `n+1` rule. IR returns characteristic functional-group bands rather than computing normal modes and intensities. The method title refers to “Curphey-Morrison” increments, but no complete published increment table, solvent/frequency conditions, uncertainty, or validation set is supplied.

The API comment says it predicts complete spectra “for any molecule.” It does not model stereotopic nuclei, long-range coupling, overlapping/non-first-order spin systems, exchange/solvent effects, conformational averaging, isotope effects, peak widths, or actual IR normal modes.

**Disposition:** improved functional-group/topological spectrum sketcher, not complete or quantitative spectroscopy.

### 5.9 Scientific metadata: valuable structure, not validation

`ScientificMethodInfo` is a positive governance feature. It creates a place to state provenance, evidence category, applicability, and warnings. However, metadata is currently authored as an assertion rather than derived from validation evidence. Examples include:

- partial force field labeled for H–Lw;
- TPSA labeled as the 43-fragment method;
- QED labeled exact;
- cycle basis labeled authentic Horton/SSSR;
- heuristic spectroscopy labeled as an empirical published increment model.

Method names and citations do not establish implementation fidelity. Each record should link to a machine-readable validation artifact: reference version, test dataset hash, metrics, tolerances, supported atom/environment set, and known failures.

## 6. Critical unchanged findings

### 6.1 ADMET still makes unsupported safety conclusions

The public result still includes hERG cardiac risk, CYP metabolism site, and BBB permeability. These outputs are created by thresholds and string rules, not trained/validated models. No training set, endpoint definition, calibration, applicability domain, confusion matrix, uncertainty, or external validation is present.

The reproduced ibuprofen response tells a user to monitor hERG patch clamp, identifies CYP enzymes and a metabolic transformation, and labels the compound CNS active. None of those conclusions follows from the calculations in the repository. Adding published TPSA/LogP/QED names does not validate these separate endpoints.

**Severity: Critical.** Remove these fields from production-facing APIs, or rename them as explicitly unsupported educational heuristics and prevent safety language.

### 6.2 EcoClean still manufactures environmental outcomes

The public PFAS-like probe still returns 99.2% mineralization. The value remains generated by a formula constructed to produce a result near 99%, without reaction kinetics, catalyst loading, medium, pH, temperature, mass balance, competing products, experimental calibration, or uncertainty.

Documentation continues to claim complete mineralization into harmless products and offers catalyst/pathway language. This is not a scientific remediation model and could be dangerously misread as process evidence.

**Severity: Critical.** Remove quantitative efficiency, half-life, harmless/non-toxic product, and catalyst recommendation outputs until supported by a calibrated model and experimental validation.

### 6.3 Formula-derived topology and SMILES information loss remain

An empirical formula still generates arbitrary bonds and can enter topology-dependent engines. The SMILES parser still silently ignores `@`/`@@`; opposite stereoisomers become identical graphs, and even `C@C` is accepted. These behaviors invalidate downstream claims without an error signal.

**Severity: High.** Separate composition from structure and reject all unsupported SMILES syntax at its exact input position.

### 6.4 Documentation is still materially inconsistent with runtime and code

Examples include:

- README says “exact analytical gradients”; code uses numerical central differences;
- README says a 43-fragment TPSA implementation; code is not exhaustive;
- README says complete/published ADMET standards can assess whether a molecule can “safely act as an oral medicine”; no safety model exists;
- README says “true Benson” for arbitrary organics; fallback is a small heuristic;
- verification document calls ibuprofen LogP 4.00/TPSA 34.1 verified; current runtime returns 3.42/37.30;
- verification document still calls PFOA 99.4% mineralization verified;
- showcase claims lead mutations eliminate hERG/CYP/toxicity liabilities that are not modelled;
- Hückel/Jacobi and other numerical methods are repeatedly called exact.

**Severity: High.** Documentation corrections are necessary even if future implementation work is planned. Current text changes the safe interpretation of outputs.

## 7. Test and validation quality

The extra tests are welcome but mostly demonstrate internal behavior:

- TPSA tests assert locally chosen fragment sums; the acetamide test embeds a reference error.
- LogP tests assert positivity, ordering, or broad ranges rather than exact external values.
- QED only checks a wide `0.10–0.95` interval.
- SSSR covers benzene and naphthalene only.
- Shomate tests use broad value ranges and do not check range rejection or interval changes.
- no test compares hundreds or thousands of molecules against RDKit, CDK, Open Babel, or a frozen published dataset;
- no force-field test compares atom typing, component energies, optimized geometries, or gradients to an independent UFF implementation;
- no ADMET/EcoClean test can establish predictive validity because no labeled dataset exists.

Coverage rose only slightly despite the new code: line coverage is 79.96% and branch coverage 69.84%. Coverage is useful for finding unexecuted code, not measuring scientific correctness.

The new CI workflow builds and tests on one operating system and one .NET version. It does not enforce warnings as errors despite the step's name, because `--warnaserror` or equivalent configuration is absent. It also lacks coverage thresholds, dependency vulnerability failure, formatting/static analysis, API integration tests, documentation-output checks, and reproducible scientific benchmark generation.

## 8. Prioritized remediation plan

### P0 — prevent harmful interpretation

1. Remove or quarantine hERG, CYP, BBB, “safe oral medicine,” toxicity, and CNS-active conclusions.
2. Remove EcoClean efficiency, half-life, catalyst recommendation, complete-mineralization, harmless, and non-toxic claims.
3. Reject unsupported SMILES characters and incomplete rings/branches; preserve or explicitly reject stereochemistry.
4. Prevent composition-only/formula-derived molecules from entering topology-dependent engines.
5. Correct README, showcase, getting-started, benchmark, XML documentation, and API descriptions to match actual scope.

### P1 — make named methods faithful or honestly renamed

1. Replace TPSA typing with the complete published fragment table and precedence rules; validate exact per-atom types and totals against an independent corpus.
2. Replace LogP/MR typing with all Wildman–Crippen atom types and explicit unsupported-environment failures.
3. Implement QED's exact descriptor definitions and structural-alert catalog; test exact values for a frozen reference set.
4. Either implement full UFF atom typing and terms or rename the current engine “UFF-inspired educational force field.”
5. Integrate cycle-basis and nullspace-basis primitives into public consumers, including a policy for multi-dimensional reaction solutions.
6. Reject Shomate temperatures outside valid intervals and use phase- and interval-specific coefficients.

### P2 — establish scientific evidence

1. Create versioned benchmark datasets with source, license, hashes, expected values, tolerances, and generation scripts.
2. Report MAE/RMSE, bias, percentiles, failure rate, applicability coverage, and confidence intervals per method.
3. Compare against at least one established independent implementation and pin its version.
4. Add adversarial cases: charges, salts, tautomers, stereochemistry, fused/bridged rings, uncommon atom types, disconnected species, and out-of-domain inputs.
5. Make every scientific result return applicability status, warnings, method version, and an unrounded raw value.
6. Treat unsupported atom types or chemistry as errors, never zero-contribution or generic-parameter fallbacks.

### P3 — engineering and supply-chain controls

1. Resolve the high-severity transitive `Microsoft.OpenApi 2.0.0` advisory and make CI fail on high/critical advisories.
2. Add true warnings-as-errors configuration and coverage thresholds.
3. Add API integration and serialization round-trip tests.
4. Generate documentation examples and benchmark tables from executable tests so they cannot drift.
5. Record OS, SDK, dependency lock, commit, random seed, data hashes, and command line in benchmark artifacts.

## 9. Acceptance criteria for the next credibility increase

A future audit should not award “supported” status to a named scientific method until all of these exist:

1. a precise, bounded claim and applicability domain;
2. a complete mapping from the cited method to source code;
3. explicit errors for unsupported inputs;
4. independent reference values over a representative dataset;
5. predeclared metrics and tolerances;
6. machine-reproducible, revision-linked results;
7. uncertainty or at least observed error distribution;
8. tests for scientifically difficult and adversarial cases;
9. documentation generated from or checked against runtime output;
10. domain-expert review for safety-, drug-, and environment-facing claims.

Suggested rating gates:

| Target | Minimum evidence |
|---|---|
| **6 / 10 overall** | P0 completed; named methods honestly scoped; no critical misleading outputs; vulnerability resolved |
| **7 / 10 overall** | Independent validation datasets and reference comparisons for every quantitative headline method |
| **8+ / 10 overall** | Broad external validation, uncertainty/applicability controls, expert review, reproducible releases, and demonstrated real-world fitness |

## 10. Final verdict

Revision `a106d55` is more credible than `236fac3` as an engineering artifact. The project now shows a serious attempt to respond to the first audit, and the additions around CI, licensing, citation, provenance, exact algebra, cycle bases, and energy/gradient consistency deserve recognition.

The scientific credibility increase is smaller than the code volume suggests. Several changes wrap simplified rules in the names of published methods, and the new tests often confirm self-consistency rather than external accuracy. The two most consequential unsupported features—drug-safety classification and environmental mineralization—remain unchanged. Documentation continues to amplify these outputs beyond what the code can establish.

**Overall claim-adjusted credibility: 4.9 / 10.**

Chemy remains best described as a broad educational and experimental chemistry toolkit containing several credible narrow calculations and several unvalidated heuristics. It should not yet be represented as a validated research, drug-safety, lead-optimization, or environmental-remediation solution.

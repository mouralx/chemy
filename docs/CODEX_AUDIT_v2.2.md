# Chemy Scientific Credibility Audit — v2.2

**Audit date:** 2026-08-21  
**Audited revision:** `b6cfec3` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v2.1.md`](CODEX_AUDIT_v2.1.md), revision `4c67cf3`  
**Auditor:** OpenAI Codex  
**Scope:** Delta review against v2.1 plus direct inspection of implementation and tests, strict build, complete test and coverage run, clean-order CI simulation, independent artifact regeneration, bidirectional CTfile verification, dependency advisory scan, and verification of newly claimed external provenance.

## 1. Executive conclusion

Revision `b6cfec3` is a real improvement. It resolves the v2.1 clean-CI sequencing defect, asserts Molfile dimensional headers, broadens the UFF comparison to sulfur and halogens, strengthens reverse CTfile assertions, uses a small-sample t interval for descriptor MAE, adds a usable standard-potential API, and makes NMR peak matching one-to-one with multiplicity and integration checks.

The implementation is healthy by normal software-engineering measures: a fresh build completed with zero warnings, all 151 tests passed, coverage exceeded both enforced floors, both RDKit-derived artifacts reproduced exactly, and no vulnerable NuGet dependency was reported.

The most important remaining defect is scientific provenance. The NMR benchmark now presents exact-looking SDBS accessions and experimental conditions, but at least two accessions are demonstrably wrong. The official AIST records identify ethanol as [SDBS-1300](https://sdbs.db.aist.go.jp/CompoundLanding.aspx?sdbsno=1300), not SDBS-412, and acetone as [SDBS-319](https://sdbs.db.aist.go.jp/CompoundLanding.aspx?sdbsno=319), not SDBS-396. The cited compound records also link to separate spectrum identifiers, reinforcing that a compound number is not a substitute for an exact spectrum accession. Consequently, the reported five-point NMR MAE of 0.094 ppm is reproducible from literals in the test, but is not currently traceable to the external records claimed by the documentation.

The electrochemistry test now genuinely queries Chemy, but its expected literals duplicate the newly added table values. It proves API/table regression and calculation wiring, not independent accuracy of all 29 database entries. The UFF breadth is better, although all new cases are tiny fixed geometries and the ethylene absolute error remains 1.0024 kcal/mol against a 0.2112 kcal/mol reference energy.

**Overall claim-adjusted credibility: 7.7 / 10.** This is up from 7.5 because CI is functional again and several checks are materially stronger. The increase is intentionally limited by the false NMR provenance and by continued overstatement of narrow numerical comparisons. Chemy is credible for education and developer prototyping within documented limits; it is not yet broadly validated for research-grade quantitative prediction.

### Credibility ratings

| Context | v2.1 | v2.2 | Interpretation at v2.2 |
|---|---:|---:|---|
| Software implementation quality | 8.7 | **9.1 / 10** | Clean CI order, warning-free build, strong local suite and coverage gates |
| Chemistry education/demonstrations | 8.9 | **8.9 / 10** | Broad and useful, but false citations are especially harmful to learners |
| Developer prototyping | 8.7 | **9.0 / 10** | Reliable automation and clearer executable contracts |
| Quantitative scientific analysis | 6.5 | **6.7 / 10** | UFF and electrochemistry breadth improve; external evidence remains narrow |
| Research/publication use | 5.0 | **4.9 / 10** | Exact experimental provenance must be trustworthy before publication use |
| Safety-of-scope | 4.5/5 | **4.5 / 5** | Heuristic domains remain visibly qualified |
| Environmental decisions | 3.0 | **3.0 / 10** | No new empirical pathway or outcome validation |
| **Overall claim-adjusted credibility** | **7.5** | **7.7 / 10** | Engineering fixes outweigh, but do not erase, evidence-quality defects |

This score is a structured engineering judgment, not a statistical confidence interval.

## 2. Reproduced evidence

| Check | v2.2 result |
|---|---|
| Build | **Passed: 0 warnings, 0 errors** (`net10.0`) |
| Automated tests | **151 passed, 0 failed, 0 skipped** in 1m23s |
| Fresh line coverage | **81.67%** (5,074 / 6,213), floor 80% |
| Fresh branch coverage | **72.78%** (2,318 / 3,185), floor 70% |
| Descriptor reference artifact | **48 records reproduced with RDKit 2025.09.2** |
| Descriptor SHA-256 | **`3d579feb7fbe159de194764556f0f31821cd69ffedee90e19a6165889b9452c5`** |
| UFF reference artifact | **4 butane conformers + 7 molecules reproduced** |
| UFF SHA-256 | **`afea038071f45ee76078d730fb7146bd6734528dc4ef38adf7b4ea80cde1eba3`** |
| Pre-build interop mode | **Passed independently** with `--generate-rdkit` |
| Post-test interop mode | **Passed** with formulas, charges, counts and 2D/3D headers |
| Dependency audit | **No vulnerable direct or transitive NuGet packages reported** |

The audit followed the committed ordering: reproduce Python/RDKit artifacts, generate RDKit fixtures, build and test, then verify Chemy exports. This directly exercises the v2.1 failure mode.

## 3. v2.1 finding disposition

| v2.1 finding | v2.2 status | Evidence |
|---|---|---|
| Clean CI fails before build | **Resolved** | Separate `--generate-rdkit` and `--verify-chemy` modes are correctly sequenced |
| Dimensional headers only printed | **Resolved** | Expected 2D/3D value is asserted per fixture |
| Electrochemistry literals do not query Chemy | **Resolved functionally** | Test queries the table and cell API; independence remains weak |
| NMR predictions can be reused | **Resolved** | Each matched prediction is removed from the available set |
| NMR multiplicity/integration not gated | **Resolved for selected peaks** | Both values are asserted |
| NMR provenance absent | **Attempted but incorrect** | Exact-looking identifiers were added, at least two contradict AIST |
| UFF element breadth weak | **Improved** | H2S, CH3Cl and CH3F add S, Cl and F |
| Ethylene UFF discrepancy obscured | **Unresolved** | 1.0024 kcal/mol still receives an undifferentiated “Verified” label |
| Reverse CTfile topology/charge placement shallow | **Improved** | Bond counts and charged atom types are now asserted |
| Approximate MAE interval method weak | **Improved** | Sample variance and t critical values replace population SD and 1.96 |
| Environmental/fallback empirical evidence absent | **Unresolved** | No relevant new validation |

## 4. Detailed findings

### 4.1 CI sequencing is repaired

The workflow now generates RDKit inputs before the .NET build and verifies Chemy outputs only after Release tests. This eliminates the deterministic clean-run failure reported in v2.1. The two modes also fail independently, which is preferable to weakening the missing-output checks.

A residual reproducibility issue remains: `verify_chemy_exports` searches the requested directory and then source, Release, and Debug fallback directories. Even when `--chemy-dir` is supplied, a missing requested path can silently resolve to another build's output. The audit observed this locally when a Debug test run was followed by selection of an older Release directory. CI currently avoids the problem because its runner is clean and builds Release, but explicit-path mode should be strict and report the selected artifact's freshness or provenance.

### 4.2 CTfile evidence is materially stronger

Chemy → RDKit now fails on incorrect dimensional headers rather than merely printing them. RDKit → Chemy adds bond-count checks and verifies that formal charges reside on the expected nitrogen or oxygen atom types. These changes close specific claim-to-test gaps.

The suite still does not compare the complete bond graph, bond orders, per-atom coordinates within V2000 precision, stereochemistry, isotopes, or SDF property fields. It supports the tested neutral/anion/cation/zwitterion and simple multi-record cases, not universal CTfile interoperability.

### 4.3 UFF coverage expands, but applicability remains narrow

The regenerated artifact and .NET comparisons now cover C, H, O, S, Cl and F. New absolute differences are 0.0503 kcal/mol for H2S, 0.0009 for chloromethane, and 0.0045 for fluoromethane. This is useful evidence that the newly exercised parameter paths agree for those fixed geometries.

However:

- all seven molecules are very small and use manually chosen coordinates;
- N, P, Br, I, charged, aromatic and more complex torsional environments are absent;
- a single universal 1.20 kcal/mol tolerance ignores scale and relative error;
- ethylene differs by 1.0024 kcal/mol, approximately 4.75 times the RDKit reference energy.

The blanket “Verified” label should be replaced by pass/fail plus absolute and relative error, with molecule-specific acceptance rationale. The result validates selected energy evaluations, not general optimization quality or full UFF fidelity.

### 4.4 Electrochemistry is now executable, but not independently validated

`StandardReductionPotentials` exposes 29 couples; lookup is case-insensitive, unknown couples fail, and standard cell potential correctly uses cathode minus anode. The benchmark queries seven entries and uses the returned Cu/Zn values in the Nernst calculation. This closes the prior defect where reference rows never touched Chemy.

The expected seven values were introduced as literals in the same test change as the implementation table and are numerically identical. No immutable CRC/IUPAC extract, page/table citation, edition metadata artifact, or independently parsed dataset is present. Thus the test detects later table changes but cannot detect a transcription error shared by implementation and test. Conditions and reaction conventions also need explicit metadata for a scientific database.

Classify this as **reference-table regression plus analytical identity**, and reserve “external numerical validation” for a separately sourced artifact whose provenance can be audited.

### 4.5 NMR matching improved, but the new provenance is not credible

The algorithm now assigns expected peaks to distinct predictions and checks shift tolerance, multiplicity and integration. That is substantially better than independent nearest-neighbor matching.

The provenance layer fails verification:

- Chemy cites `SDBS-412` for ethanol; AIST lists ethanol as **SDBS-1300**.
- Chemy cites `SDBS-396` for acetone; AIST lists acetone as **SDBS-319**.
- the official compound pages link to spectrum-level identifiers distinct from compound numbers;
- the repository provides no downloaded spectrum, peak table, retrieval date, stable spectrum identifier, or derivation record for its ppm values;
- blanket `CDCl3, 400 MHz` conditions are asserted without record-level evidence.

The “SDBS / NIST” wording is also ambiguous: a value must identify which source supplied it. Until every row is corrected and reproducible, remove the accessions and “Sourced/Verified” claims or mark the dataset unverified. Passing assertions over handwritten literals do not repair false provenance.

The matching still does not assert that no unexplained predicted peaks remain or map an expected chemical group to a specific predicted atom environment. Only five non-exchangeable peak positions across four simple molecules are assessed. The reported MAE is descriptive for those five selected points only.

### 4.6 Descriptor uncertainty calculation is better, not a generalization guarantee

The calculation now uses sample variance and Student's t critical values for the known N values. This is a defensible classical interval for the mean of the selected absolute errors under its assumptions.

Absolute errors are bounded, non-negative and may be skewed; the partitions are selected chemical records rather than random samples of a declared population. The interval therefore quantifies sampling-style uncertainty within this benchmark calculation, not applicability-domain or prospective generalization uncertainty. A bootstrap interval and explicit chemical-domain sampling design would be stronger.

### 4.7 Passing tests are strong regression evidence, not universal scientific validation

The repository's 151 tests and coverage gates are valuable. They demonstrate deterministic behavior over many implemented paths. Coverage percentages measure executed code, not correctness of scientific models. Most subsystem claims remain supported by analytical identities, curated examples, or narrow reference sets; only selected descriptor, UFF, interoperability and spectroscopy paths touch external numerical evidence.

The appropriate product claim remains: **educational and prototyping chemistry toolkit with selected externally compared calculations**. “Research validated”, “publication ready”, or broad predictive-accuracy claims would exceed the evidence.

## 5. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v2.1 | v2.2 | Principal finding |
|---|---:|---:|---|
| Elements/molar mass | 4.6 | **4.6** | Stable 48-record evidence |
| SMILES parsing | 3.3 | **3.3** | No material scientific change |
| Reaction balancing/stoichiometry | 4.0–4.2 | **4.0–4.2** | Strong identities; limited external corpus |
| Solutions/basic kinetics | 4.0–4.5 | **4.0–4.5** | No material change |
| Electrochemistry | 4.2 | **4.3** | Real API and cell wiring; table independence incomplete |
| Reaction-network integration | 3.0 | **3.0** | No external dynamic benchmark |
| Hückel solver/interpretation | 2.6–4.2 | **2.6–4.2** | No material change |
| Shomate thermodynamics | 4.2 | **4.2** | Stable external evidence |
| Empirical thermodynamic fallback | 1.5 | **1.5** | Still unvalidated |
| Molecular mechanics | 3.9 | **4.0** | More elements; tiny systems and ethylene discrepancy limit inference |
| 3D geometry/conformers | 3.3 | **3.4** | Header contract gated; coordinate/optimization accuracy remains shallow |
| TPSA subset | 4.2 | **4.2** | Statistical method improves; data unchanged |
| LogP/MR subset | 3.7 | **3.7** | Same limited post-development dataset |
| QED-inspired score | 4.0 | **4.0** | Stable evidence |
| Physicochemical profile | 4.2 | **4.2** | Stable post-development evidence |
| Spectroscopy | 2.8 | **2.6** | Better assertions, but claimed source records are inaccurate |
| Ring perception | 3.8 | **3.8** | No material change |
| Lead exploration | 2.2 | **2.2** | No validation of discovery utility |
| EcoClean | 2.2 | **2.2** | Heuristic and empirically unvalidated |
| Molfile/SDF parser/exporter | 4.2 | **4.4** | Correct CI order and stronger assertions; contract still narrow |

## 6. Priority remediation

### P0 — correct scientific provenance

1. Remove or correct every NMR accession and condition after checking the exact AIST/NIST spectrum record.
2. Store an immutable machine-readable reference file containing source, compound ID, spectrum ID, solvent, frequency, temperature if known, peak assignment, retrieval date and license/terms note.
3. Make the test consume that artifact and checksum it; do not duplicate implementation-facing values as anonymous literals.
4. Correct the public benchmark table and release notes so false source claims are not propagated.

### P1 — make external comparisons independent

1. Add a versioned electrochemistry reference artifact with precise edition/page/table provenance and compare all supported entries.
2. Keep implementation and expected data in separate derivation paths so one transcription cannot populate both.
3. For NMR, assert assignment identity and unexplained/missing peaks, then expand to chemically diverse held-out molecules.
4. For UFF, add N/P/Br/aromatic/charged cases, optimized geometries, force/gradient checks, and relative-error criteria.

### P2 — harden reproducibility and scope

1. Make `--chemy-dir` strict; disable fallback discovery when a directory is explicitly supplied.
2. Compare complete CTfile topology, bond orders, atom-level charges, coordinate tolerances and SDF properties.
3. Replace hard-coded t-value switching with a tested statistical routine or bootstrap implementation.
4. Publish explicit applicability domains and unsupported-input behavior for each predictive subsystem.
5. Add empirical validation for thermodynamic fallbacks and environmental pathways before using them for decisions.

## 7. Acceptance criteria for v2.3

A further credibility increase should require:

- corrected, record-level NMR provenance that a reviewer can independently retrieve;
- a reference artifact independent of both tests and implementation for electrochemical data;
- strict artifact-path selection in interoperability CI;
- UFF evaluation across larger and chemically distinct systems with scale-aware errors;
- no “Verified” label where the test establishes only execution or tolerance compliance.

## 8. Final verdict

The developers resolved the central engineering regression from v2.1 and strengthened multiple claim-to-test links. The project builds cleanly, all tests pass, coverage gates are meaningful, reference generators reproduce, and the CI workflow is now correctly ordered.

Scientific credibility depends on traceability as much as passing code. The newly asserted NMR provenance is inaccurate, and the electrochemistry references are not independently encoded. Those are correctable evidence-engineering defects, but they prevent the new tables from carrying the weight the documentation assigns to them.

Chemy v2.2 is therefore assessed at **7.7 / 10 overall claim-adjusted credibility**: strong for education and prototyping, increasingly disciplined as software, but still requiring verified source artifacts and broader independent benchmarks before research-grade quantitative reliance.

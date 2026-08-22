# Chemy Scientific Credibility Audit — v2.6

**Audit date:** 2026-08-22
**Audited state:** uncommitted working tree based on `c932190` (`main`)
**Previous audit:** [`CODEX_AUDIT_v2.5.md`](CODEX_AUDIT_v2.5.md), uncommitted candidate based on `c932190`
**Auditor:** OpenAI Codex
**Scope:** Delta audit against v2.5; direct source/claim inspection; warning-free build; complete tests with and without coverage; fresh coverage measurement; exact UFF output capture; live RDKit regeneration; strict positive/negative interoperability; dependency advisory scan; and primary-source review of the new UFF and NIST Shomate claims.

## 1. Executive conclusion

The v2.6 candidate removes the benchmark-fitted `2.4979` kcal/mol inversion offset identified in v2.5 and replaces it with three neighbor permutations at `6/3` kcal/mol. Method metadata now acknowledges the inversion term. Electrochemistry also removes the irrelevant NIST SRD 46 citation and the unsupported “100% reconciliation” assertions, while NMR records explicitly distinguish measured `HSP` spectra from generated `HPM` spectra. These are genuine corrections.

Engineering health remains excellent. The solution builds with zero warnings, all 154 tests pass, fresh coverage clears both gates, the enlarged RDKit artifact reproduces, the explicit interoperability path passes and its missing-directory negative path fails closed. The cached force-field implementation remains dramatically faster than v2.4.

The candidate nevertheless introduces a new P0 scientific-reference defect. The `C2H4(g)` coefficients and passing expected values in the test do not match NIST's ethylene Shomate table. Chemy asserts approximately 60.35 J/(mol·K) at 298 K and 139.35 J/(mol·K) at 1000 K, whereas NIST reports approximately 42.90 and 93.88 J/(mol·K). The production constants, expected values and test were added together, so the test proves internal agreement with the wrong row rather than NIST agreement. All species also inherit a generic 298.15–2000 K range instead of the actual coefficient interval.

The eight new UFF cases are useful post-development regression evidence, but the repository cannot support the labels “held-out,” “untouched,” or “prospective”: cases, implementation changes, outputs and tolerances arrive in the same candidate. Several tolerances are selected above large observed errors. Furan differs by 15.4147 kcal/mol, thiophene by 14.0202 kcal/mol and acetonitrile by 0.5309 kcal/mol against a 0.1406 kcal/mol reference, yet all are labeled “Held-Out Verified.”

**Overall claim-adjusted credibility: 7.8 / 10.** This decreases from 7.9 despite several valid remediations because a newly advertised external NIST benchmark is demonstrably false and the candidate repeats the previously corrected misuse of held-out/prospective terminology.

## 2. Reproduced evidence

| Check | v2.6 candidate result |
|---|---|
| Build | **Passed: 0 warnings, 0 errors** (`net10.0`) |
| Tests without coverage | **154 passed, 0 failed, 0 skipped** in about 1s |
| Tests with coverage | **154 passed, 0 failed, 0 skipped** in about 15s |
| Fresh line coverage | **82.00%** (5,239 / 6,389), floor 80% |
| Fresh branch coverage | **73.64%** (2,506 / 3,403), floor 70% |
| UFF artifact regeneration | **4 butane conformers + 12 standard + 8 new cases reproduced with RDKit 2025.09.2** |
| UFF on-disk SHA-256 | **`2a546f64cd8210eaa873a6f68130bfcdcbaa4c921ef847b634111d3224363306`** |
| UFF embedded canonical SHA-256 | **`b88913a3c1a05b48fc27413c64c78a13bd4ca17e61e862544f923a1f42a7cbeb`** |
| Strict explicit interop | **Passed against the requested Debug output** |
| Missing explicit interop | **Correctly failed with exit code 1 and no fallback** |
| Dependency audit | **No vulnerable direct or transitive NuGet packages reported** |
| Diff hygiene | **Passed** (`git diff --check`) |

The fresh coverage file was selected by modification time and verified directly, rather than aggregating historical `TestResults` files.

## 3. v2.5 acceptance-criteria disposition

| v2.5 criterion | Status | Evidence |
|---|---|---|
| Cited transferable inversion without fitted offset | **Improved / partial** | Fitted `2.4979` removed; three `6/3` permutations added, but carbonyl-specific UFF force constants and component validation remain absent |
| Formamide classified as calibration if fitted | **Resolved in code** | Fitted offset removed; result again differs from RDKit |
| Untouched external UFF evaluation | **Unresolved / mislabeled** | Eight cases added with implementation and thresholds in the same candidate |
| Factual method/public claims | **Partial** | Method metadata fixed; README still says four terms and zero allocations; benchmark values are stale |
| Remove or justify NIST SRD 46 | **Resolved** | SRD 46 removed from the redox artifact and public source list |
| Identifiable electrochemistry review artifact | **Unresolved** | Neutral bibliography retained, but no reviewer, extraction or reconciliation artifact exists |
| Explicit measured/generated NMR provenance | **Improved strongly** | `spectrum_kind` and record-specific notes distinguish HSP measured from HPM generated |
| Geometry convergence evidence | **Unresolved** | Budgets are now 100, but generator still discards convergence status and no comparison demonstrates sufficiency |

## 4. Detailed findings

### 4.1 P0 — the new ethylene “NIST held-out” benchmark is wrong

`ShomateThermodynamics.cs` adds this ethylene coefficient vector:

```text
-6.086505, 249.8338, -130.5661, 25.92500,
0.255860, 42.53026, 179.8000, 52.46694
```

The official [NIST Chemistry WebBook ethylene record](https://webbook.nist.gov/cgi/cbook.cgi?ID=C74851&Mask=8F&Units=SI) gives the 298–1200 K vector:

```text
-6.387880, 184.4019, -112.9718, 28.49593,
0.315540, 48.17332, 163.1568, 52.46694
```

NIST's table reports `Cp = 42.90` J/(mol·K) at 298.15 K and `93.88` at 1000 K. The new test instead accepts 60.30–60.40 and 139.30–139.40. Entropy and enthalpy expectations are likewise generated from the non-NIST vector. Because the test and production row share the same error, 154 passing tests do not protect this claim.

The `HeldOutSpecies` name also has no demonstrated holdout chronology. CO and NH3 use recognizable NIST low-temperature vectors, but they are added to production and accepted by new literals in the same change.

### 4.2 P0 — Shomate validity intervals remain species-blind

Every `ShomateCoefficients` record defaults to 298.15–2000 K, and none of the nine database rows supplies an actual interval. NIST publishes piecewise ranges. For example:

- [CO](https://webbook.nist.gov/cgi/cbook.cgi?ID=C630080&Mask=2381&Units=SI): 298–1300 K and 1300–6000 K;
- [NH3](https://webbook.nist.gov/cgi/cbook.cgi?ID=C7664417&Mask=2787): 298–1400 K and 1400–6000 K;
- [C2H4](https://webbook.nist.gov/cgi/cbook.cgi?ID=C74851&Mask=8F&Units=SI): 298–1200 K and 1200–6000 K.

The API therefore accepts, for example, 2000 K for the stored low-temperature CO, NH3 and ethylene vectors even though each has already crossed its interval boundary. The method metadata and README's universal “298.15 K to 2000 K” statement overclaim the stored piecewise data.

Each database entry needs its real `TMinKelvin`/`TMaxKelvin`, with a second coefficient segment where the public API claims wider coverage. Add boundary tests immediately below, at and above each transition.

### 4.3 The fitted inversion offset is removed, but full UFF fidelity is not established

The revised planar inversion expression is much more defensible than v2.5. It creates three neighbor permutations and divides the 6 kcal/mol constant by three. For ordinary trivalent sp2 C/N/O, this is structurally consistent with RDKit's `C0=1, C1=-1, C2=0` path and three-contribution construction; see RDKit's official [inversion coefficient implementation](https://raw.githubusercontent.com/rdkit/rdkit/master/Code/ForceField/UFF/Utils.cpp), [energy expression](https://raw.githubusercontent.com/rdkit/rdkit/master/Code/ForceField/UFF/Inversion.cpp), and [builder](https://raw.githubusercontent.com/rdkit/rdkit/master/Code/GraphMol/ForceFieldHelpers/UFF/Builder.cpp). The cited [Rappé et al. UFF paper](https://pubs.acs.org/doi/10.1021/ja00051a040) remains the primary method source.

Important gaps remain:

- UFF/RDKit uses 50 kcal/mol for an sp2 carbon bound to sp2 oxygen; Chemy assigns 6 to all selected centers, including carbonyl carbon;
- Chemy includes `S_2` in this inversion path, while the cited implementation's supported center/type logic differs;
- the engine still uses a fixed harmonic angle constant and an inspired/subset parameterization, so “UFF-inspired” remains the correct name;
- no component-level inversion test, out-of-plane scan, optimized geometry or gradient comparison was added.

The live formamide output is now Chemy `2.4600`, RDKit `4.9579`, difference `2.4979` kcal/mol. That is scientifically more honest than fitting the total, but the documentation still reports `2.1369`, `2.8210` and a 2.90 threshold while the executable threshold is 2.60.

### 4.4 The eight UFF cases are expanded regression, not held-out validation

The new cases materially broaden fixed-geometry execution: methanol, acetone, toluene, pyridine, dichloromethane, furan, thiophene and acetonitrile. The RDKit generator reproduces every reference total.

However, the cases, implementation changes, generator outputs, thresholds, tests and “Held-Out Verified” table are all absent from `c932190` and present together in the current uncommitted candidate. There is no earlier frozen manifest, checksum, preregistration or separate evaluator. The comments even call the thresholds “prospective” without evidence they preceded observation. As in audit v1.7, these are post-development expanded regressions.

The observed errors also show why pass labels need effect-size criteria:

| Molecule | Chemy | RDKit | Absolute error | Approx. relative error | Gate |
|---|---:|---:|---:|---:|---:|
| Methanol | 3.3138 | 2.1346 | 1.1792 | 55% | 1.50 |
| Acetone | 4.6731 | 3.4756 | 1.1975 | 34% | 1.50 |
| Furan | 18.8692 | 34.2839 | 15.4147 | 45% | 20.00 |
| Thiophene | 43.0625 | 57.0827 | 14.0202 | 25% | 20.00 |
| Acetonitrile | 0.6715 | 0.1406 | 0.5309 | 378% | 1.00 |

These results are valuable diagnostic evidence that the inspired implementation is not quantitatively interchangeable with RDKit UFF across these classes. Labeling all of them verified obscures that conclusion.

### 4.5 Public and executable UFF evidence disagree

The published standard table is stale for formamide and slightly stale for chloromethane/fluoromethane. More importantly, the test prints the iodomethane difference but contains no `Assert` for `eCH3I`, despite the public table advertising a `<= 0.10` gate. The test asserts bromomethane and then formamide, skipping iodine entirely.

The method metadata now correctly lists five terms, while README sections still call the engine a four-term UFF potential. README also retains “Zero Allocations,” although topology lists/arrays, position arrays, gradients, candidates and result arrays are allocated. Synchronize public tables from live test output or a generated report, and make every advertised threshold executable.

### 4.6 Provenance wording improves

NMR now explicitly records `spectrum_kind: measured` for ethanol, benzene and acetic acid and `generated` for acetone. Acetone remains correctly identified as the AIST 300 MHz generated `HPM-00-026` spectrum. This directly resolves the v2.5 semantic gap, though immutable spectrum-level source captures for every record would still improve independent reviewability.

Electrochemistry removes NIST SRD 46 and the self-attested audit/reconciliation fields. All 29 rows retain CRC table/page coordinates and neutral source metadata. This is the correct evidentiary classification: a hash-locked, specifically cited regression table. It still lacks an identifiable reviewer or reproducible source extraction, so independent derivation remains unproven.

### 4.7 Geometry iteration budgets recover, but convergence is still hidden

The v2.5 audit observed budgets of 50. The current candidate uses 100 iterations for both paths, which is a safer correction. One path rises from the historical 80, while the multi-center path remains below its historical 150. `GenerateConformer3D` still returns only `MinimizedMolecule`, discarding `Converged`, termination reason and final gradient. No new test demonstrates convergence distributions or output equivalence at 100 versus 150 iterations.

## 5. Updated scorecard

| Area | v2.5 | v2.6 | Principal reason |
|---|---:|---:|---|
| Software implementation quality | 9.3 | **9.3 / 10** | Fast clean suite, reproducible gates, two additional tests; scientific fixture defect is not a build defect |
| Chemistry education/demonstrations | 8.8 | **8.6 / 10** | Better NMR/provenance semantics, but false ethylene reference can teach wrong thermodynamics |
| Developer prototyping | 9.2 | **9.2 / 10** | Stable and fast, with clearer force-field method metadata |
| Quantitative scientific analysis | 6.9 | **6.7 / 10** | Honest inversion revision and broader diagnostics offset by false NIST benchmark and fitted gates |
| Research/publication use | 5.0 | **4.8 / 10** | Incorrect external-reference claim and invalid interval handling are publication blockers |
| Overall claim-adjusted credibility | 7.9 | **7.8 / 10** | Several remediations, but new P0 reference truth defect |

## 6. Priority remediation

### P0 — repair Shomate source truth

1. Replace ethylene with the exact NIST 298–1200 K vector and correct all expected values from an independently transcribed/source-captured table.
2. Store the real interval on every coefficient record and add additional high-temperature segments where advertised.
3. Test every interval boundary and make out-of-segment calls reject or select the correct segment.
4. Rename the new species partition from `HeldOutSpecies` unless its selection chronology can be independently demonstrated.

### P0 — make UFF evaluation labels statistically honest

1. Rename `held_out_molecules` to `expanded_regression` or `post_development_evaluation`.
2. Remove “untouched,” “prospective” and blanket “verified” labels from the current partition.
3. Report absolute and relative errors without converting permissive ceilings into scientific-equivalence claims.
4. Add a genuinely frozen evaluation protocol in one revision, then publish outputs in a later revision without changing parameters or thresholds.

### P1 — finish force-field fidelity and claim synchronization

1. Implement and test carbonyl-specific inversion behavior or document the intentional departure.
2. Add per-component comparisons and out-of-plane scans against pinned RDKit.
3. Add the missing iodomethane assertion.
4. Regenerate the benchmark table from live outputs; correct formamide values and threshold.
5. Change README from four to five terms and remove the zero-allocation claim.

### P1 — expose optimization quality

1. Return convergence metadata from the conformer-generation API or provide a detailed companion API.
2. Compare 100- and 150-iteration outcomes over a declared molecule set using termination reason, final gradient, energy and geometry metrics.

### P2 — complete provenance review

1. Add immutable record-level NMR source evidence for all four compounds.
2. Add an identifiable electrochemistry review worksheet, reviewer/date and discrepancy disposition if independent review is claimed.

## 7. Acceptance criteria for v2.7

A credibility increase should require:

- NIST-correct ethylene coefficients and values;
- real per-species Shomate intervals with transition tests;
- no unsupported held-out/prospective/untouched terminology;
- UFF tables that match live outputs and executable gates, including iodine;
- carbonyl inversion behavior either implemented from the cited method or explicitly classified as a departure;
- README/method metadata agreement on terms and allocations;
- externally reviewable freeze chronology for any future held-out evaluation;
- convergence evidence for geometry's 100-iteration budget.

## 8. Final verdict

The developers responded constructively to v2.5: they removed the fitted formamide offset, corrected method metadata, restored a safer iteration budget, clarified NMR spectrum kinds and removed misleading electrochemistry provenance. The new UFF cases are also useful because their large discrepancies expose the actual applicability boundary rather than hiding it.

The new NIST ethylene benchmark, however, is not a small documentation issue. It places incorrect coefficients in production, asserts incorrect external values, and passes because the test duplicates the error. Together with species-blind validity intervals and unsupported held-out language, that prevents the candidate from receiving a higher credibility score.

The v2.6 candidate is therefore assessed at **7.8 / 10 overall claim-adjusted credibility**: excellent software engineering for education and prototyping, but not yet reliable for broad quantitative force-field equivalence or publication-grade thermodynamic reference work.

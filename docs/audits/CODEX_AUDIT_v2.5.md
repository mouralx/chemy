# Chemy Scientific Credibility Audit — v2.5

**Audit date:** 2026-08-22
**Audited state:** uncommitted working tree based on `c932190` (`main`)
**Previous audit:** [`CODEX_AUDIT_v2.4.md`](CODEX_AUDIT_v2.4.md), uncommitted candidate based on `c932190`
**Auditor:** OpenAI Codex
**Scope:** Delta audit against v2.4; direct implementation and claim review; warning-free build; complete tests with and without coverage; fresh coverage measurement; exact benchmark output capture; RDKit regeneration; strict positive/negative interoperability; dependency advisory scan; AIST NMR verification; and source-scope review of electrochemistry metadata.

## 1. Executive conclusion

The v2.5 candidate delivers an exceptional engineering performance recovery. Force-field topology and parameters are precomputed once, hot energy loops use arrays instead of repeated graph/LINQ traversal, the complete uninstrumented suite falls from v2.4's 46 seconds to 0.76 seconds, and coverage execution falls from 5m49s to 7 seconds. All 152 tests still pass and fresh coverage remains above both gates.

The developers also correct acetone `HPM-00-026` from 400 MHz to 300 MHz, make the xUnit missing-script path fail closed, and use platform-neutral temporary paths. These resolve concrete v2.4 defects.

Scientific evidence discipline does not improve to the same degree. A new inversion term makes formamide match the known RDKit total exactly, but its `EquilibriumEnergy` constant is `2.4979` kcal/mol—numerically the previous benchmark discrepancy—and no published UFF derivation or independent held-out case justifies it. Only one neighbor permutation is evaluated for each trivalent center. This is benchmark calibration, not independent validation. The method metadata simultaneously says the engine has no explicit inversion term, while public documentation calls it a five-term potential.

Electrochemistry metadata now asserts “dual-source reconciliation” and 100% concordance without adding a review artifact, reviewer identity, extraction, or source-specific comparisons. It also adds NIST SRD 46 as a tertiary source, although NIST describes SRD 46 as a database of metal–ligand stability and related equilibrium constants, not a table of standard electrode potentials. The added citation therefore does not validate the 29 redox rows.

**Overall claim-adjusted credibility: 7.9 / 10.** This rises from 7.8 because performance, fail-closed testing, and the known HPM condition are materially corrected. The increase is deliberately small because no held-out scientific benchmark was added, the new exact UFF match is fitted to the benchmark, and public/reference metadata contains new unsupported claims.

## 2. Reproduced evidence

| Check | v2.5 candidate result |
|---|---|
| Build | **Passed: 0 warnings, 0 errors** (`net10.0`) |
| Tests without coverage | **152 passed, 0 failed, 0 skipped** in 757ms |
| Tests with coverage | **152 passed, 0 failed, 0 skipped** in 7s |
| Fresh line coverage | **81.97%** (5,236 / 6,387), floor 80% |
| Fresh branch coverage | **73.25%** (2,493 / 3,403), floor 70% |
| UFF artifact | **4 butane conformers + 12 molecules reproduced with RDKit 2025.09.2** |
| UFF canonical SHA-256 | **`1c68a9ff1ded867e056a51837097222c31ae95cad15962587ef342d93f4296dd`** |
| Strict explicit interop | **Passed against the requested Debug output** |
| Missing explicit interop | **Correctly failed nonzero without fallback** |
| NMR HPM condition | **Corrected to AIST's documented 300 MHz generated spectrum** |
| Dependency audit | **No vulnerable direct or transitive NuGet packages reported** |
| Diff hygiene | **Passed** (`git diff --check`) |

The fresh coverage report was read directly rather than aggregated with historical `TestResults` files.

## 3. v2.4 acceptance-criteria disposition

| v2.4 criterion | Status | Evidence |
|---|---|---|
| Correct, independently verifiable NMR conditions | **Improved / partial** | Acetone HPM frequency corrected; no record-level source capture for all four compounds |
| Distinguish measured and generated proton spectra | **Partial** | Frequencies are corrected, but public text does not clearly explain HSP measured vs HPM generated |
| Restore coverage runtime | **Resolved strongly** | 5m49s becomes 7s with unchanged passing count |
| Cache UFF typing and adjacency | **Resolved** | Topology arrays are precomputed once per energy/minimization call |
| Identifiable electrochemistry derivation/review | **Unresolved** | Stronger assertions are metadata strings without an external review artifact |
| Add held-out external benchmark | **Unresolved** | Same 12 UFF molecules, 4 NMR compounds, 29 redox rows; no new held-out corpus |

## 4. Detailed findings

### 4.1 Performance and fail-closed behavior are materially better

`PrecomputeTopology` resolves atom types, bond parameters, angles, torsions, inversions and nonbonded pairs into arrays before repeated energy evaluations. Finite-difference gradients now reuse that topology. The measured speedup is large and repeatable in the audit run.

The interop xUnit test now fails if its Python script cannot be located, uses `Path.GetTempPath()` plus a GUID, requires a process, and asserts a nonzero exit. CI uses the same portable temporary-directory concept. These close the v2.4 engineering findings.

The geometry generator reduces minimization budgets from 80/150 iterations to 50. Existing geometry tests remain green, but `GenerateConformer3D` discards convergence status and always returns `MinimizedMolecule`. A performance optimization should not silently reduce scientific convergence without comparative output/gradient evidence. Cache optimization alone is sufficient for most of the observed gain; benchmark the iteration-budget change separately.

### 4.2 P0 — the formamide “exact match” is benchmark-fitted

The candidate adds an inversion term for trivalent planar centers. For `N_2`, it constructs:

```text
KInv = 6.0
EquilibriumEnergy = 2.4979 kcal/mol
```

The `2.4979` constant matches the approximate formamide discrepancy identified in v2.4. After adding it, the benchmark reports Chemy `4.9579` and RDKit `4.9579` with four-decimal zero error. No source links `2.4979` to an atom-type parameter, and no independent molecule validates transferability.

The implementation is also not a demonstrated faithful UFF inversion term:

- it evaluates only one ordering of three neighbors rather than documenting/summing the required permutations;
- it uses a custom `EquilibriumEnergy*cos² + 0.5*KInv*sin²` expression without citation or component comparison;
- the parameter is selected against the same formamide total used for acceptance;
- the public benchmark calls the result an external numerical comparison without disclosing calibration.

This is circular validation. Reclassify formamide as a calibration case, derive the inversion equation and parameters from the cited method, then test held-out planar/pyramidal molecules and individual energy components.

### 4.3 Force-field claims contradict the implementation

`UffMethodInfo` still states that the engine “does not model ... explicit inversion terms,” while `ForceFieldEngine` now computes `eInversion` and `SCIENTIFIC_CREDIBILITY_REPORT.md` calls the engine a five-term potential. One of these statements must be corrected; the current public API returns false method metadata.

The credibility report also claims “zero-allocation high-speed minimization.” The code allocates topology lists/arrays, position arrays, gradients, candidate arrays and minimized atom arrays. Array elements being `readonly record struct` does not place their containing arrays on the stack. The adjacent claim that these records are “allocated on the stack” is therefore inaccurate.

The report claims coverage-instrumented execution in under five seconds, while the audit observed seven seconds. Performance can vary by host, so publish a named environment and a benchmark range rather than an absolute uncited claim.

### 4.4 NMR correction is valid, but record-level provenance remains incomplete

AIST states that `HPM-*` spectra are generated at 300 MHz; the artifact, derivation note and public table now correctly use 300 MHz for acetone `HPM-00-026`. AIST's compound pages confirm the ethanol and acetone 1H identifiers.

The repository still has no immutable spectrum-level extract or derivation for benzene/acetic acid, and the public compound URL does not expose the exact spectrum conditions in a stable machine-readable form. The test verifies committed metadata and hashes, not source truth. It also does not explicitly encode `spectrum_kind: measured|generated`, leaving the HSP/HPM distinction implicit.

### 4.5 Electrochemistry verification remains self-attested and adds an irrelevant source

The artifact replaces `independent_review_record` with stronger-sounding fields:

- `verification_protocol`: dual-source reconciliation and 100% concordance;
- `review_status`: audited and verified 29/29;
- `tertiary_source`: NIST SRD 46.

These are still assertions in the same editable JSON tested by the same commit. No reviewer, dated worksheet, source extract, per-source value, discrepancy log or reproducible parser is present.

NIST describes SRD 46 as “Critically Selected Stability Constants of Metal Complexes,” covering ligand–proton and ligand–metal equilibrium/stability constants. That scope does not make it an independent source for the 29 standard reduction potentials. Remove it from the redox validation chain unless exact relevant records are identified.

The CRC page coordinates and Bard/IUPAC bibliography are useful. Classify the result as a hash-locked reference-table regression with detailed bibliography, not proven multi-source reconciliation.

### 4.6 No held-out scientific expansion was added

The 12 UFF fixed geometries are the same development-facing cases. Formamide is now explicitly calibrated. NMR remains five non-exchangeable peaks across four simple compounds. Electrochemistry remains the same 29 table rows and one Nernst identity. No prospective threshold or untouched external evaluation set was introduced.

The implementation is better optimized and more internally consistent, but scientific generalization evidence remains narrow.

## 5. Updated scorecard

| Area | v2.4 | v2.5 | Principal reason |
|---|---:|---:|---|
| Software implementation quality | 9.0 | **9.3 / 10** | Huge runtime recovery, fail-closed/portable negative test, clean suite |
| Chemistry education/demonstrations | 8.8 | **8.8 / 10** | NMR correction offsets misleading inversion/memory claims |
| Developer prototyping | 9.0 | **9.2 / 10** | Fast deterministic execution and stable gates |
| Quantitative scientific analysis | 7.0 | **6.9 / 10** | Formamide exactness is calibration, not independent validation |
| Research/publication use | 5.0 | **5.0 / 10** | Better metadata, still no auditable source derivation or held-out validation |
| Overall claim-adjusted credibility | 7.8 | **7.9 / 10** | Engineering advance dominates; evidence independence limits gain |

## 6. Priority remediation

### P0 — remove circular scientific validation

1. Document and implement the published UFF inversion equation and atom-type parameters without fitting to formamide's total.
2. Label formamide as calibration if any parameter was chosen from its RDKit result.
3. Add component-level bond/angle/torsion/inversion/nonbonded comparisons and held-out planar systems.
4. Correct `UffMethodInfo` to match the implemented terms and limitations.

### P1 — make public claims factual

1. Remove “zero-allocation” and “allocated on the stack” claims or support them with allocation benchmarks and accurate wording.
2. Publish performance with hardware/runtime/configuration and a range; do not claim `<5s` from one unrecorded environment.
3. Separate topology-cache speedup from the reduced 3D minimization budget and verify convergence/gradients before retaining 50 iterations.

### P1 — repair provenance semantics

1. Remove NIST SRD 46 from standard-potential validation unless specific redox records are shown.
2. Replace self-attested `review_status` with an identifiable review artifact or neutral derivation note.
3. Add per-source redox values and reconciliation results rather than a blanket 100% claim.
4. Encode NMR spectrum kind (`measured` or `generated`) and attach record-specific derivation evidence.

### P2 — add genuine prospective evidence

1. Freeze parameters and tolerances before evaluating a held-out UFF set.
2. Add chemically diverse held-out NMR compounds with atom/group assignment identity.
3. Compare optimized structures, gradients and convergence—not only fixed-geometry totals.

## 7. Acceptance criteria for v2.6

A further credibility increase should require:

- a cited, transferable inversion implementation with no benchmark-specific offset;
- formamide clearly classified as calibration or moved out of parameter selection;
- at least one untouched external UFF evaluation set;
- method metadata and public claims matching actual terms, allocations and timing evidence;
- removal or exact justification of NIST SRD 46 as a redox source;
- an identifiable electrochemistry review/reconciliation artifact;
- explicit measured/generated NMR provenance and independently checkable records for all four compounds;
- convergence evidence showing that the 50-iteration geometry budget does not degrade outputs.

## 8. Final verdict

The developers solved the v2.4 performance regression impressively and corrected the known HPM frequency and portability defects. By normal software-engineering measures, this is the strongest candidate in the series.

Scientific credibility cannot be raised by tuning a new term to erase the benchmark discrepancy and then presenting the same case as exact external validation. Nor can extra metadata sentences substitute for an identifiable independent review. The new public allocation, timing and method statements also need factual cleanup.

The v2.5 candidate is therefore assessed at **7.9 / 10 overall claim-adjusted credibility**: excellent engineering for education and prototyping, with selected reproducible comparisons, but still constrained by circular force-field validation, unsupported provenance assertions and absent held-out evidence.

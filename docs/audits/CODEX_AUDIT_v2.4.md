# Chemy Scientific Credibility Audit — v2.4

**Audit date:** 2026-08-22
**Audited state:** uncommitted working tree based on `c932190` (`main`)
**Previous audit:** [`CODEX_AUDIT_v2.3.md`](CODEX_AUDIT_v2.3.md), revision `c932190`
**Auditor:** OpenAI Codex
**Scope:** Delta audit against v2.3; direct review of the candidate working tree; warning-free build; complete tests with and without coverage; fresh coverage measurement; independent RDKit regeneration; strict positive- and negative-path interoperability checks; dependency advisory scan; primary-source NMR verification; and review of UFF typing and electrochemistry traceability.

## 1. Executive conclusion

The candidate working tree is a meaningful improvement over v2.3. Strict interoperability selection is now reachable and used by CI, an explicit missing directory fails, the NMR artifact uses real proton-spectrum families rather than carbon-13 `CDS-*` identifiers, reference files are hash-locked, electrochemistry rows carry page coordinates, UFF tests add iodine and planar formamide, and executable molecule-specific tolerances now match the public benchmark table.

The two previously central failures are therefore no longer wholesale failures. Strict artifact selection is resolved. NMR provenance is substantially improved: AIST directly confirms ethanol `HSP-01-876` and acetone `HPM-00-026` as 1H records. However, the artifact describes acetone's `HPM-00-026` spectrum as 400 MHz, while AIST states that all `HPM-*` spectra are generated at **300 MHz**. A hash and structural assertions preserve that incorrect metadata rather than independently validating it. The exact benzene and acetic-acid spectrum IDs, peak transcriptions, and per-record conditions also remain unsupported by an immutable source capture or extraction path in the repository.

Routine engineering checks pass, but coverage-enabled execution regressed sharply. The uninstrumented suite passes all 152 tests in 46 seconds. The same suite with the CI coverage collector takes 5m49s, versus 1m25s recorded in v2.3. Atom typing is recomputed through repeated LINQ scans inside every energy and finite-difference gradient evaluation, a plausible contributor. CI remains functional, but the regression should be profiled and bounded.

**Overall claim-adjusted credibility: 7.8 / 10.** This rises from 7.6 because strict reproducibility, executable thresholds, atom-environment handling, and provenance structure all improve materially. The increase is limited by one demonstrably false NMR condition, self-attested rather than independently reviewable reference derivations, narrow benchmark breadth, and the coverage-time regression.

## 2. Reproduced evidence

| Check | v2.4 candidate result |
|---|---|
| Build | **Passed: 0 warnings, 0 errors** (`net10.0`) |
| Automated tests without coverage | **152 passed, 0 failed, 0 skipped** in 46s |
| Automated tests with coverage | **152 passed, 0 failed, 0 skipped** in 5m49s |
| Fresh line coverage | **81.75%** (5,126 / 6,270), floor 80% |
| Fresh branch coverage | **72.93%** (2,444 / 3,351), floor 70% |
| UFF artifact | **4 butane conformers + 12 molecules reproduced with RDKit 2025.09.2** |
| UFF canonical SHA-256 | **`1c68a9ff1ded867e056a51837097222c31ae95cad15962587ef342d93f4296dd`** |
| Strict explicit interop path | **Passed against the requested Debug export directory** |
| Missing explicit interop path | **Correctly failed nonzero; no fallback selected** |
| NMR identifier verification | **Ethanol and acetone IDs confirmed; acetone frequency contradicted by AIST** |
| Dependency audit | **No vulnerable direct or transitive NuGet packages reported** |
| Diff hygiene | **Passed** (`git diff --check`) |

The coverage figures above come directly from the fresh Cobertura report generated during this audit, not from merging historical `TestResults` directories.

## 3. v2.3 acceptance-criteria disposition

| v2.3 criterion | Status | Evidence |
|---|---|---|
| Exact verified 1H IDs and conditions | **Partially resolved** | Correct record family and confirmed ethanol/acetone IDs; acetone HPM frequency is wrong and remaining record-level facts are not independently captured |
| Missing explicit interop directory must fail | **Resolved** | CLI negative path exits nonzero even when fallback artifacts exist |
| CI must verify one freshly built exact directory | **Resolved** | Workflow passes strict Release output path after Release tests |
| Page/table-level electrochemistry traceability | **Improved, not independently proven** | All rows name CRC table/page; no source extract, reproducible derivation, or reviewer identity supports the assertion |
| Environment-sensitive UFF typing and planar-N regression | **Resolved for the requested case** | Public atom typer distinguishes aromatic/double/resonant nitrogen; formamide exercises planar amide N |
| Executable scale-aware UFF gates | **Resolved** | Per-molecule thresholds now mirror the published table |

## 4. Detailed findings

### 4.1 P0 — acetone NMR frequency is still false

The candidate corrects the most serious v2.3 category error. Official AIST compound pages identify:

- ethanol, SDBS-1300: 1H NMR `HSP-01-876`;
- acetone, SDBS-319: 1H NMR `HPM-00-026`.

Those identifiers now match the artifact. AIST's own introduction, however, says spectra whose codes begin `HPM` are **generated spectra at 300 MHz**. The candidate assigns `HPM-00-026` a frequency of `400 MHz` in its JSON, derivation note, tests, and public benchmark table. That condition must be corrected to 300 MHz unless an exact record provides contrary evidence and the discrepancy is explained.

The new tests validate metadata shape and expected literals, not truth against AIST. For example, they require `HSP-*` or `HPM-*`, non-empty frequency, 303.15 K, and an AIST-looking URL. A plausible but false value passes as long as the colocated hash is updated. Hash locking detects later file mutation; it does not establish source accuracy.

The public documentation should distinguish direct measurements (`HSP`) from generated 300 MHz spectra (`HPM`). The broad statement that the artifact was “transcribed directly” is too strong until every peak and condition has a record-level derivation that a reviewer can reproduce.

### 4.2 Strict interoperability is now correctly fail-closed

The CLI exposes `--strict`, automatically enables strict behavior when `--chemy-dir` is explicitly present, and passes the flag into `verify_chemy_exports`. CI names the exact Release export directory. A direct negative reproduction with `/private/tmp/definitely-missing-chemy-audit` failed instead of selecting an older artifact.

The new xUnit negative test has one weakness: if the script cannot be found, it returns successfully rather than skipping or failing. CI's explicit shell negative test covers the repository workflow, but the .NET test should fail closed too. The CI script also hard-codes a Unix `/private/tmp` path; use a repository-local guaranteed-missing path or platform temporary-directory API if cross-platform runners are intended.

### 4.3 Coverage-enabled runtime regressed by approximately 4.1×

The v2.3 audit recorded 1m25s for the coverage suite. The candidate required 5m49s under the same `XPlat Code Coverage` collector, while completing without coverage in 46s. Two initially observed coverage runs appeared stalled for several minutes; the second eventually passed.

The new atom-typing method repeatedly scans all molecular bonds, including nested scans for resonant nitrogen. It is called inside bond, angle, torsion and nonbonded loops and again for every finite-difference energy evaluation during minimization. Coverage instrumentation magnifies this allocation and branch overhead.

Cache atom types once per molecule/energy context and add a CI duration budget or benchmark. This is not a correctness failure, but it materially slows the mandatory quality gate and increases timeout risk.

### 4.4 UFF applicability is better but remains UFF-inspired

The production code now distinguishes carbon, nitrogen, oxygen and sulfur types by aromaticity, bond order, coordination and a resonance heuristic. This is much better than element-plus-coordination selection. Formamide specifically verifies that an amide nitrogen is planar, and iodine expands elemental breadth.

Limitations remain:

- typing is a hand-built subset, not the complete published UFF atom-type system;
- amide/resonance recognition is local and heuristic;
- phosphorus remains a single `P_3` parameter family;
- the angle term is harmonic and the engine omits explicit inversion and electrostatics, as its method metadata now acknowledges;
- formamide differs from RDKit by roughly 2.5 kcal/mol and receives a 2.60 kcal/mol ceiling selected around that known result;
- ethylene retains a 1.20 kcal/mol floor despite a 474.6% relative error.

The “UFF-Inspired” name is appropriate. These comparisons support selected fixed-geometry energy agreement, not general UFF equivalence or optimized-structure fidelity.

### 4.5 Electrochemistry traceability is more specific but self-attested

Every reference row now carries a CRC page and table, and the test enforces the artifact checksum, metadata, electron counts, and page presence. This is a strong integrity and regression improvement.

The field `independent_review_record` contains only the sentence that traceability was verified. It provides no reviewer, date, method, signed record, source extract, or reproducible transcription. The expected checksum is embedded in the same test file changed alongside the artifact. Consequently, the repository demonstrates internal consistency and specific bibliography, but not an independent audit of all 29 values.

Keep the improved page coordinates, but rename that field to a neutral derivation note unless a real review record is added.

### 4.6 Scientific breadth remains narrow

The NMR result remains five selected non-exchangeable peak groups across four simple molecules. The improved test now requires no unmatched non-exchangeable predictions, which closes a real gap, but nearest-shift assignment still does not prove atom/group identity or broader generalization.

UFF covers 12 small fixed geometries and four butane conformers. Electrochemistry is table validation plus one Nernst identity. Coverage percentages and passing tests remain strong software evidence, not universal scientific validation.

## 5. Updated scorecard

| Area | v2.3 | v2.4 | Principal reason |
|---|---:|---:|---|
| Software implementation quality | 9.0 | **9.0 / 10** | Fail-closed interop and stronger tests offset coverage-runtime regression |
| Chemistry education/demonstrations | 8.7 | **8.8 / 10** | Correct spectrum families and clearer UFF limits; acetone frequency still misleading |
| Developer prototyping | 8.9 | **9.0 / 10** | Better artifact determinism and typed force-field paths |
| Quantitative scientific analysis | 6.8 | **7.0 / 10** | Broader executable comparisons and traceability structure, still narrow/self-attested |
| Research/publication use | 4.7 | **5.0 / 10** | Record IDs and page coordinates improve; exact source derivations remain insufficient |
| Overall claim-adjusted credibility | 7.6 | **7.8 / 10** | Most acceptance gates close, with provenance truth and performance still limiting |

## 6. Priority remediation

### P0 — correct remaining provenance facts

1. Correct acetone `HPM-00-026` to 300 MHz in the artifact, derivation note, test expectations and public table, or attach exact contrary record evidence.
2. Independently verify benzene and acetic-acid spectrum IDs, frequencies, solvents, temperatures, peaks and multiplicities.
3. Store record-specific derivation evidence permitted by AIST terms; do not rely on compound landing URLs alone for spectrum conditions.
4. Distinguish measured `HSP` spectra from generated `HPM` spectra in documentation.

### P1 — restore CI performance

1. Precompute UFF atom types and adjacency once rather than rescanning bonds inside every energy/gradient inner loop.
2. Profile coverage-enabled force-field tests and publish a repeatable timing baseline.
3. Add a generous but meaningful CI timeout so genuine hangs fail predictably.

### P1 — make provenance review genuinely independent

1. Replace the self-attested electrochemistry `independent_review_record` string with an identifiable review artifact or neutral derivation note.
2. Keep checksum expectations in a separately reviewed manifest or verification script.
3. Record edition, page, row/reaction convention and reviewer disposition for each table entry.

### P2 — deepen scientific validation

1. Expand NMR to held-out, chemically diverse molecules with atom/group assignment identity.
2. Expand UFF to optimized geometries, gradients, charged/aromatic systems and larger molecules.
3. Justify thresholds prospectively rather than fitting them just above observed errors.
4. Fail rather than return when the xUnit interop script cannot be found.

## 7. Acceptance criteria for v2.5

A further credibility increase should require:

- corrected and independently verifiable conditions for every NMR record;
- an explicit distinction between measured and generated proton spectra;
- coverage-enabled test time restored near the previous baseline or justified by a repeatable benchmark;
- cached UFF typing/adjacency with unchanged numerical outputs;
- an identifiable electrochemistry derivation or review record rather than a self-asserting metadata string;
- at least one held-out external benchmark expansion not used to choose implementation parameters or tolerances.

## 8. Final verdict

The developers resolved strict artifact selection and materially improved the benchmark infrastructure. Hash locking, page coordinates, complete non-exchangeable NMR matching, scale-aware executable UFF gates, and environment-sensitive atom typing are real advances.

Scientific provenance still requires fact-level verification. AIST confirms the corrected ethanol and acetone identifiers, but contradicts the candidate's 400 MHz condition for the acetone `HPM` record. Meanwhile, mandatory coverage execution is now substantially slower and should be treated as a CI performance regression.

The v2.4 candidate is therefore assessed at **7.8 / 10 overall claim-adjusted credibility**: robust educational and prototyping software with improving evidence discipline, but still short of research-grade quantitative validation and fully independent provenance.

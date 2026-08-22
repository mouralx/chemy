# Chemy Scientific Credibility Audit — v2.3

**Audit date:** 2026-08-21
**Audited revision:** `c932190` (`main`)
**Previous audit:** [`CODEX_AUDIT_v2.2.md`](CODEX_AUDIT_v2.2.md), revision `b6cfec3`
**Auditor:** OpenAI Codex
**Scope:** Delta audit against v2.2; direct implementation and test inspection; warning-free build; complete test and coverage run; independent RDKit regeneration; negative-path interoperability test; primary-source verification of NMR identifiers and conditions; and review of electrochemistry evidence independence.

## 1. Executive conclusion

Revision `c932190` improves benchmark packaging and presentation, but it does not resolve the central v2.2 provenance finding. The new NMR artifact contains correct compound numbers but assigns `CDS-*` identifiers to proton spectra. AIST defines `CDS` records as reconstructed **13C NMR** spectra. Its compound pages instead list ethanol's proton record as `HSP-01-876` and acetone's as `HPM-00-026`; neither matches the committed IDs. The artifact also declares 298.15 K globally while AIST describes its normal NMR measurement temperature as 30 °C (303.15 K), subject to record-level conditions.

The claimed strict interoperability behavior is likewise not active. `verify_chemy_exports(..., strict=True)` exists, but the CLI never passes `strict=True` and exposes no strict option. Supplying an explicitly missing `--chemy-dir` still succeeds by selecting an older Release artifact. This directly fails a v2.3 acceptance criterion.

Engineering health remains strong: the solution builds with zero warnings, all 151 tests pass, coverage clears both gates, and the expanded RDKit UFF artifact reproduces. Electrochemistry now compares all 29 implementation entries with a separate JSON artifact, a useful regression improvement, although the artifact was added in the same commit, lacks page/table-level source coordinates, and its checksum is documented rather than enforced.

**Overall claim-adjusted credibility: 7.6 / 10.** The score decreases slightly from 7.7. More benchmark paths are executable, but exact-looking false NMR identifiers and a nonfunctional strict-mode claim repeat the two most important evidence failures from v2.2.

## 2. Reproduced evidence

| Check | v2.3 result |
|---|---|
| Build | **Passed: 0 warnings, 0 errors** (`net10.0`) |
| Automated tests | **151 passed, 0 failed, 0 skipped** in 1m25s |
| Fresh line coverage | **82.16%** (5,191 / 6,318), floor 80% |
| Fresh branch coverage | **74.13%** (2,631 / 3,549), floor 70% |
| UFF artifact | **4 butane conformers + 10 molecules reproduced with RDKit 2025.09.2** |
| UFF canonical SHA-256 | **`cebb5e0dc388bc3f3375fb0e0ec6fa730382d817acd41dc1f6fb5ae174450f2b`** |
| Explicit missing interop path | **Incorrectly passed by falling back to Release output** |
| NMR record verification | **Failed: committed `CDS-*` IDs are not the claimed 1H records** |
| Diff hygiene | **Passed** (`git diff --check`) |

Coverage was measured from the fresh report generated during this audit. The repository coverage verifier found older reports too, but its merged result equaled the fresh run's displayed aggregate and cleared both enforced floors.

## 3. v2.2 finding disposition

| v2.2 requirement | v2.3 status | Evidence |
|---|---|---|
| Correct record-level NMR provenance | **Unresolved / contradicted** | Compound IDs corrected; spectrum type and IDs remain wrong, and temperature is unsupported |
| Independent electrochemistry artifact | **Partially resolved** | Separate 29-row JSON is consumed, but provenance is not page/table exact and derivation remains unauditable |
| Strict interoperability artifact selection | **Unresolved** | Strict parameter is dead from the CLI; explicit missing path falls back and passes |
| Broader, scale-aware UFF evidence | **Partially resolved** | N/P/Br added and relative errors shown; systems remain tiny and tests retain one universal 1.20 kcal/mol gate |
| Accurate validation labels | **Improved** | Ethylene is now “Tolerance Compliant”; other broad “Verified” labels remain |

## 4. Detailed findings

### 4.1 P0 — NMR provenance remains false

`experimental_nmr_reference.json` is structurally better than handwritten test literals: it records compound IDs, purported spectrum IDs, solvent, frequency, temperature, assignments, and retrieval date. The test consumes it and performs one-to-one matching.

However, the provenance is not correct:

- AIST lists ethanol as SDBS-1300, with proton record `HSP-01-876`; the artifact claims `CDS-01-387`.
- AIST lists acetone as SDBS-319, with proton record `HPM-00-026`; the artifact claims `CDS-00-098`.
- AIST documents `HSP`/`HPM` as proton-spectrum families and `CDS` as reconstructed carbon-13 spectra. Therefore all four `CDS-*` values are type-incompatible with the claimed 1H benchmark.
- AIST documents normal NMR measurement at 30 °C, whereas the artifact asserts 298.15 K without record-level evidence.
- The test reads but does not validate `frequency`, `temperature_k`, formula, source identity, retrieval date, or a source checksum. Incorrect metadata cannot fail the suite.

The ppm values may be chemically plausible, but the repository has not shown that they came from the records named. The table must not label them “Sourced” or “Verified” until each exact proton record and condition is independently checked.

### 4.2 P1 — strict interop selection is implemented but unreachable

The helper accepts a `strict` boolean and correctly limits candidates when it is true. `main()` always calls `verify_chemy_exports(args.chemy_dir)` without passing it, and the argument parser has no `--strict` switch. CI also invokes only `--verify-chemy`.

A negative-path reproduction supplied `/private/tmp/definitely-missing-chemy-audit` as `--chemy-dir`. The command selected `src/Chemy.Core.Tests/bin/Release/.../chemy_exported` and exited successfully. This is the exact stale-artifact behavior v2.2 required the developers to remove.

The simplest contract is stronger: an explicitly supplied directory must always be authoritative, while fallback discovery may remain only when the option is omitted.

### 4.3 Electrochemistry independence improves, but traceability is incomplete

The benchmark now loads 29 expected entries from `crc_iupac_reduction_potentials.json` and compares all exposed table values at 0.1 mV tolerance. This is materially stronger regression coverage than seven inline literals.

It is not yet a fully auditable external derivation. The artifact and implementation update were committed together; the metadata identifies CRC 97th edition and Section 5 but no page, table, row, archival extract, or derivation script. The test does not enforce the documented SHA-256. The Gold Book citation defines terminology rather than independently supplying all 29 numerical rows.

Classify this as a versioned reference-table comparison with stated bibliography, not proof that every transcription was independently verified.

### 4.4 UFF breadth grows, while atom typing becomes more molecule-specific

The RDKit generator and artifact reproducibly add ammonia, phosphine, and bromomethane. The documentation now exposes absolute and relative errors and appropriately isolates ethylene's 474.6% relative discrepancy.

The production change maps every three-coordinate nitrogen to 106.7° and every three-coordinate phosphorus to 93.3° based only on element and coordination count. That fits NH3 and PH3, but cannot distinguish pyramidal amines from planar amide, aromatic, imine, or other typed environments. It is a benchmark-shaped approximation, not faithful UFF atom typing. No regression case exercises a three-coordinate planar nitrogen.

The executable assertions also continue to apply the same 1.20 kcal/mol absolute ceiling to every molecule, despite the documentation showing molecule-specific “tolerance floors.” The presentation is scale-aware; the gate is not.

### 4.5 NMR accuracy evidence remains extremely narrow

The benchmark still contains only five selected non-exchangeable peak groups across four simple molecules. It removes matched predictions but never asserts that no unexplained predictions remain. It also matches by nearest shift rather than chemical environment identity. Its 0.094 ppm MAE is descriptive only for these selected points and cannot support general predictive-accuracy claims.

## 5. Updated scorecard

| Area | v2.2 | v2.3 | Principal reason |
|---|---:|---:|---|
| Software implementation quality | 9.1 | **9.0 / 10** | Healthy build/tests; dead strict path and stale-artifact acceptance |
| Chemistry education/demonstrations | 8.9 | **8.7 / 10** | False exact spectrum identifiers are especially harmful pedagogically |
| Developer prototyping | 9.0 | **8.9 / 10** | Stable suite and broader artifacts, with reproducibility caveat |
| Quantitative scientific analysis | 6.7 | **6.8 / 10** | Broader UFF/electrochemistry execution; provenance still blocks inference |
| Research/publication use | 4.9 | **4.7 / 10** | Record-level source claims remain unreliable |
| Overall claim-adjusted credibility | 7.7 | **7.6 / 10** | Engineering gains do not offset repeated evidence-integrity failures |

## 6. Priority remediation

### P0 — repair or withdraw NMR source claims

1. Replace each `CDS-*` identifier with the exact AIST 1H record (`HSP-*`, `HPM-*`, or applicable modern record) actually used.
2. Transcribe solvent, frequency, temperature, shifts, multiplicities and assignments from that same record; use `null` rather than an assumed value when metadata is absent.
3. Add record-specific source URLs/citation text and a captured derivation note permitted by AIST's terms.
4. Make tests validate metadata completeness and identity, not only peak values.

### P1 — activate strict artifact selection

1. Treat any explicitly supplied `--chemy-dir` as strict automatically, or expose and use `--strict`.
2. Update CI to pass its exact Release output directory.
3. Add a negative test proving a nonexistent explicit directory exits nonzero even when fallback artifacts exist.

### P1 — separate scientific references from implementation

1. Add page/table/row coordinates or a reproducible derivation for all electrochemical entries.
2. Enforce reference-artifact checksums in CI from a manifest maintained separately from generated output.
3. Add an independent review record for transcription and reaction convention.

### P2 — improve UFF applicability

1. Type atoms by bonding/hybridization environment rather than element plus coordination count.
2. Add planar three-coordinate nitrogen, aromatic, charged, iodine and larger optimized systems.
3. Make executable thresholds molecule-specific and justify both absolute and relative gates.

## 7. Acceptance criteria for v2.4

A credibility increase should require:

- exact, verified 1H spectrum identifiers and record-level conditions for every NMR row;
- a failing negative-path test for an explicitly missing interoperability directory;
- CI verification against one exact, freshly built artifact directory;
- electrochemistry provenance with page/table-level traceability or reproducible extraction;
- environment-sensitive UFF atom typing plus at least one planar three-coordinate nitrogen regression;
- executable scale-aware UFF thresholds matching the published table.

## 8. Final verdict

The developers improved data organization, broadened executable comparisons, and preserved excellent routine engineering health. Those gains are real.

The revision nevertheless fails the two most important v2.3 acceptance tests. The NMR artifact replaces wrong compound numbers with wrong spectrum identifiers and unsupported blanket conditions, while strict artifact selection remains unreachable and demonstrably falls back to stale output. Scientific credibility requires the metadata and negative paths to be as testable as the numerical happy path.

Chemy v2.3 is therefore assessed at **7.6 / 10 overall claim-adjusted credibility**: strong educational/prototyping software with selected reproducible comparisons, but not yet reliable enough in provenance or applicability evidence for research-grade quantitative claims.

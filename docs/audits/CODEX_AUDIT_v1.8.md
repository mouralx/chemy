# Chemy Scientific Credibility Audit — v1.8

**Audit date:** 2026-08-21  
**Audited revision:** `0a8eb4e` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.7.md`](CODEX_AUDIT_v1.7.md), revision `060f555`  
**Earlier audits:** [`CODEX_AUDIT_v1.6.md`](CODEX_AUDIT_v1.6.md), [`CODEX_AUDIT_v1.5.md`](CODEX_AUDIT_v1.5.md), [`CODEX_AUDIT_v1.4.md`](CODEX_AUDIT_v1.4.md), [`CODEX_AUDIT_v1.3.md`](CODEX_AUDIT_v1.3.md), [`CODEX_AUDIT_v1.2.md`](CODEX_AUDIT_v1.2.md), [`CODEX_AUDIT_v1.1.md`](CODEX_AUDIT_v1.1.md), [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md)  
**Auditor:** OpenAI Codex  
**Scope:** Delta audit against v1.7, including code/documentation comparison, strict build, full tests, fresh coverage, RDKit regeneration, dependency advisories, descriptor partitions, all-atom butane energetics, and Molfile charge handling.

## 1. Executive conclusion

Revision `0a8eb4e` closes several concrete credibility gaps identified in v1.7:

- the misleading `held_out` label is replaced with `expanded_regression`;
- tuning and expanded-regression metrics are calculated and gated separately;
- Python reference dependencies now have exact versions and artifact hashes;
- CI uses a repository-controlled Cobertura parser and enforces line and branch floors;
- the butane test now has 14 non-collapsed atom coordinates and verifies actual dihedrals;
- Molfile atom-block charge codes and `M  CHG` records are implemented and tested;
- the stale numeric test badge and machine-local documentation links were removed.

These are substantive engineering and claim-fidelity improvements. The new revision deserves a credibility increase.

The most important remaining scientific limitation is independence. The 32-molecule descriptor corpus is explicitly and correctly a regression corpus, not a generalization experiment. The butane test now provides a legitimate internal conformational-ordering regression, but it still has no pinned external energies or geometry comparison. Its raw energies—7.34, 16.15, 33.14, and 65.71 kcal/mol—are absolute force-field totals, and the test does not compare relative barriers with experimental data or RDKit UFF. It therefore cannot establish quantitative physical accuracy.

Molfile charge fidelity is materially improved, but the exporter still calls itself “Industrial-Grade,” “ISO/IUPAC-compliant,” and compatible with four external applications without any external conformance test. It also supports only a subset of V2000/SDF fields. That documentation remains stronger than the evidence.

**Overall claim-adjusted credibility: 7.1 / 10**, up from 6.9. Chemy is a credible educational and developer-prototyping scientific toolkit with strong deterministic regression controls. It is not yet validated for publication-grade physical prediction, broad chemical-space inference, environmental decisions, or full chemical-file interoperability.

### Credibility ratings

| Context | v1.7 | v1.8 | Interpretation at v1.8 |
|---|---:|---:|---|
| Software implementation quality | 8.4 | **8.7 / 10** | Strict clean build, hash-locked reference gate, dual coverage floors, safer parser |
| Chemistry education/demonstrations | 8.5 | **8.6 / 10** | Broad functionality with increasingly honest validation labels |
| Developer prototyping | 8.2 | **8.4 / 10** | Strong regression and reproducibility scaffolding |
| Quantitative scientific analysis | 5.5 | **5.7 / 10** | Better partition reporting and physical smoke test; no independent generalization evidence |
| Research/publication use | 4.2 | **4.4 / 10** | Reproducibility is strong, predictive validation remains narrow |
| Safety-of-scope | 4.4/5 | **4.4 / 5** | Unsupported medical predictions remain absent |
| Environmental decisions | 2.9 | **2.9 / 10** | Proposed degradation pathways remain empirically unvalidated |
| **Overall claim-adjusted credibility** | **6.9** | **7.1 / 10** | Stronger evidence controls and more accurate claims, with major independent-validation gaps |

The score is a structured engineering judgment, not a statistical confidence interval.

## 2. Reproduced evidence

| Check | v1.7 | v1.8 result |
|---|---:|---:|
| Release build with warnings as errors | Passed | **Passed: 0 warnings, 0 errors** |
| Automated tests | 146 passed | **146 passed, 0 failed, 0 skipped** |
| Fresh line coverage | 80.85% | **81.39%** (3,810 / 4,681) |
| Fresh branch coverage | 72.26% | **74.07%** (2,320 / 3,132) |
| CI line threshold | 80% | **80% enforced** |
| CI branch threshold | Absent | **70% enforced** |
| RDKit verification | 32 passed | **32 passed with RDKit 2025.09.2** |
| Dataset SHA-256 | `bbcbc89...` | **`fda1ca39cd853bd49bcb1827abe68e1668d55a60c6bfe83deb6217ea20a5a0a1` confirmed** |
| Python dependency artifacts | Version-pinned | **Version- and hash-locked** |
| Vulnerable NuGet packages | None reported | **None reported, including transitive dependencies** |
| Butane coordinate validity | Hydrogens collapsed | **All 14 atoms have constructed coordinates and verified dihedrals** |
| External force-field comparison | Absent | **Absent** |
| Formal-charge round trip | Absent | **Implemented for tested V2000 cases** |
| Independent file conformance | Absent | **Still absent** |

The coverage totals differ from v1.7 because this is a fresh collector output at the new revision; percentages and counts above come directly from its Cobertura root. The repository verifier independently accepted the same file.

## 3. v1.7 finding disposition

| v1.7 finding | v1.8 status | Evidence |
|---|---|---|
| `held_out` was misleading | **Resolved** | Renamed `expanded_regression` in generator, fixture, tests, and docs |
| Partitions not separately evaluated | **Resolved** | MAE/RMSE/max and thresholds run for each partition and combined data |
| Dependencies lacked hashes | **Resolved** | Requirements include hashes; CI passes `--require-hashes` |
| Butane hydrogens at origin | **Resolved** | Explicit all-atom coordinates are constructed |
| No actual dihedral verification | **Resolved** | Test computes and constrains 180°, 60°, 120°, and 0° |
| No external force-field target | **Unresolved** | Only internal ordering and downhill minimization are asserted |
| Formal-charge claim unsupported | **Substantially resolved** | Atom codes and `M  CHG` parsed/exported; tested for a -1 ion |
| Independent Molfile/SDF conformance absent | **Unresolved** | Chemy writer is still tested with Chemy reader only |
| No branch-coverage floor | **Resolved** | Repository script enforces 70% |
| Floating ReportGenerator | **Resolved** | Removed in favor of standard-library Python XML parsing |
| README test count stale | **Resolved** | Numeric test count removed |
| Machine-local documentation links | **Resolved in benchmark doc** | Relative repository links now used |

## 4. Detailed findings

### 4.1 Reference generation is now strongly reproducible

The requirements file records hashes for the supported RDKit, NumPy, and Pillow distributions, and CI installs with `--require-hashes`. The generator still asserts RDKit `2025.09.2` at runtime and compares exact generated bytes. The audit reproduced the committed file and its documented checksum.

This satisfies the v1.7 immutability request far better than version pins alone. Remaining limitations are operational rather than scientific:

- CI exercises only Ubuntu/Python 3.11, while the requirements comment claims macOS/Linux and x86-64/arm64;
- `python -m pip install --upgrade pip` remains floating, although pip does not define the scientific outputs;
- the large manually maintained wheel-hash list will require disciplined regeneration on upgrades.

**Assessment:** the external descriptor baseline is now reproducible and integrity checked for the CI environment.

### 4.2 Partition reporting is honest and useful, but not prospective validation

Changing `held_out` to `expanded_regression` directly resolves the principal experimental-design misstatement. The test now reports and gates both 16-record partitions separately:

| Partition | TPSA MAE | LogP MAE | QED MAE |
|---|---:|---:|---:|
| Tuning (N=16) | 0.0000 Å² | 0.2289 | 0.0280 |
| Expanded regression (N=16) | 0.0000 Å² | 0.1953 | 0.0111 |
| Combined (N=32) | 0.0000 Å² | 0.2121 | 0.0195 |

The expanded partition also has LogP maximum error 0.632 and the tuning partition has QED maximum error 0.203. CI gates mean error only. This permits isolated large errors even when a partition passes.

The records were used during implementation and remain regression cases. These metrics characterize agreement on selected known inputs; they do not estimate performance on unseen chemical space. A frozen prospective corpus, uncertainty analysis, unsupported-input rate, and applicability-domain policy are still required for generalization claims.

### 4.3 The butane test is now a valid internal regression, not an external benchmark

The previous catastrophic geometry problem is fixed. The test constructs all four carbons and ten hydrogens, verifies the C–C–C–C angles numerically, observes the expected ordering, and checks that minimization moves the highest-energy structure downhill.

Observed totals were:

| Conformer | Dihedral | Chemy total energy |
|---|---:|---:|
| Anti | 180° | 7.3377 kcal/mol |
| Gauche | 60° | 16.1516 kcal/mol |
| Eclipsed | 120° | 33.1359 kcal/mol |
| Syn-eclipsed | 0° | 65.7130 kcal/mol |

The hierarchy is chemically plausible, but the experiment still cannot establish numerical accuracy:

- no common zero is subtracted and no relative barrier tolerance is asserted;
- no RDKit UFF, Open Babel, published UFF, or experimental result supplies target values;
- bond lengths and angles are handcrafted rather than externally optimized and normalized;
- total-energy differences include every force-field term, not an isolated torsional scan;
- only the syn conformer is minimized, so minima and barriers are not compared consistently.

The method/test names still contain “Physical” and “TorsionBarrier.” A defensible description is **all-atom conformational-ordering regression**. Quantitative physical credibility should not increase substantially until external relative energies and optimized geometries are checked.

### 4.4 Formal-charge support is real but file-format scope remains narrow

The parser reads V2000 atom-block charge codes and later `M  CHG` records, applying property-block values after atom-block values. The exporter writes both representations, splitting `M  CHG` entries into groups of eight. The tested acetate anion retains `-1` through an internal round trip. Missing `M  END` is now rejected by `FromSdf`.

Remaining limitations include:

- only one negative-charge case is tested; positive charges, multiple records, override semantics, and ±2/±3 values are not covered;
- malformed or truncated `M  CHG` entries are silently partially accepted;
- `FromMolfileV2000` itself does not require an `M  END` terminator;
- isotope, radical, stereo, valence, atom-map, query, and SDF data fields remain unsupported or discarded;
- unsupported exporter bond types silently become single bonds;
- no external parser verifies Chemy output and no external fixture verifies Chemy input.

The class summary still says “Industrial-Grade” and “ISO/IUPAC-compliant” and names ChemDraw, PyMOL, RDKit, and BIOVIA compatibility. MDL V2000 is a CTfile format, and the repository contains no cross-application evidence supporting these universal claims. `API_REFERENCE.md` repeats the ISO/IUPAC statement. These should be narrowed to the implemented V2000 subset until conformance testing exists.

### 4.5 Coverage enforcement is materially better

The new verifier uses Python's standard XML library, fails when no report exists, reads both line and branch rates, and returns failure below 80%/70%. It removes the floating ReportGenerator tool and shell/locale parsing from v1.7.

One robustness edge remains: when a glob matches multiple reports, the script evaluates only `matches[0]`, whose ordering is not explicitly controlled. A clean CI job currently creates one report, so the gate works as intended there. For local or future multi-test-project runs, the script should require exactly one aggregate report or combine all matched reports deterministically.

The README's static 81.15% badge is already different from this audit's fresh 81.39%. A CI-backed dynamic badge or a threshold badge would avoid recurring staleness. Its static “CI Passing” badge is also not linked to actual workflow state.

### 4.6 Documentation claim fidelity improved, with old broad claims remaining

The latest benchmark edits accurately rename the expanded partition, publish separate metrics, update the checksum, and use portable links. These are meaningful improvements.

Remaining overstatements include:

- the benchmark document introduces itself as “comprehensive, end-to-end scientific verification,” despite major domains lacking independent accuracy evidence;
- the exporter and API reference claim industrial-grade ISO/IUPAC and named-tool compatibility without conformance evidence;
- butane test naming implies a quantitatively validated physical barrier;
- README says “zero dependencies” while the solution has .NET package dependencies and CI uses RDKit for validation; “no runtime scientific dependency” would be more precise;
- hard-coded passing/coverage badges represent repository text, not live CI evidence.

## 5. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.7 | v1.8 | Principal finding |
|---|---:|---:|---|
| Elements/molar mass | 4.4 | **4.5** | Hash-locked RDKit regression evidence |
| SMILES parsing | 3.1 | **3.1** | No new syntax breadth; covered inputs remain stable |
| Reaction balancing/stoichiometry | 4.0–4.2 | **4.0–4.2** | No material change |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Strong narrow textbook implementations |
| Reaction-network integration | 3.0 | **3.0** | No external validation |
| Hückel solver/interpretation | 2.6–4.2 | **2.6–4.2** | Analytical core remains a strength; interpretation remains heuristic |
| Shomate thermodynamics | 3.8 | **3.8** | No new database breadth |
| Empirical thermodynamic fallback | 1.5 | **1.5** | No new independent validation |
| Molecular mechanics | 3.0 | **3.3** | Valid all-atom ordering regression, still no external numeric target |
| 3D geometry/conformers | 2.8 | **2.9** | Better explicit geometry test, no conformer-quality corpus |
| TPSA subset | 3.8 | **3.9** | Reproducible partitioned regression; no unseen evaluation |
| LogP/MR subset | 3.2 | **3.3** | Separate error distributions expose limits |
| QED-inspired score | 3.7 | **3.8** | Separate error distributions; reduced alerts remain |
| Physicochemical profile | 3.9 | **4.0** | Stronger reproducibility and honest partition semantics |
| Spectroscopy | 2.3 | **2.3** | No external error distribution |
| Ring perception | 3.8 | **3.8** | No material change |
| Lead exploration | 2.2 | **2.2** | Scripted enumeration rather than validated discovery |
| EcoClean | 2.2 | **2.2** | Proposed pathways remain speculative |
| Molfile/SDF parser/exporter | 3.3 | **3.6** | Charge round trip added; broad conformance claims remain unproven |

## 6. Priority remediation

### P0 — align remaining claims with evidence

1. Rename the butane test to conformational-ordering regression unless external target values are added.
2. Replace “Industrial-Grade” and “ISO/IUPAC-compliant” with an explicit supported-field contract.
3. Remove named-application compatibility claims until exercised in automated cross-tool tests.
4. Replace static CI/coverage badges with live workflow badges or threshold statements.
5. Replace “zero dependencies” with the narrower runtime/algorithmic property actually meant.

### P1 — add independent scientific evaluation

1. Freeze a descriptor evaluation corpus before the next algorithm changes and keep it inaccessible to tuning.
2. Gate maximum/percentile errors and unsupported-input rate, not only MAE.
3. Compare butane relative energies and optimized geometries against pinned external UFF output.
4. Add independent spectroscopy and empirical-thermodynamics error distributions.
5. Validate environmental transformations against experimental or curated reaction evidence.

### P2 — complete interoperability and CI robustness

1. Test charged, isotopic, stereochemical, radical, and SDF-property fixtures from independent sources.
2. Reject malformed `M  CHG` records and unsupported exporter bond types explicitly.
3. Require `M  END` consistently in both Molfile and SDF entry points.
4. Round-trip fixtures through RDKit or another pinned external toolkit in CI.
5. Make coverage verification reject ambiguity or aggregate multiple reports deterministically.

## 7. Acceptance criteria for v1.9

A material increase in research credibility should require:

- at least one frozen, prospectively evaluated corpus not used during implementation;
- pinned external butane relative energies/geometries with numerical tolerances;
- independent Molfile/SDF interoperability evidence;
- maximum-error or percentile gates alongside mean descriptor errors;
- corrected broad documentation claims;
- one independent benchmark outside the descriptor family.

## 8. Final verdict

This revision responds well to the previous audit. It fixes the misleading holdout language instead of cosmetically defending it, makes the reference environment genuinely integrity checked, strengthens CI, repairs the malformed butane geometry, and implements real charge preservation. Those are credible improvements.

Chemy now earns **7.1 / 10 overall**: strong for learning and prototyping, increasingly disciplined as scientific software, but still below research-grade validation. The next substantial gain will not come from more internally passing tests. It will come from prospective data and independent implementations that can reveal errors the Chemy code and its own fixtures cannot detect.

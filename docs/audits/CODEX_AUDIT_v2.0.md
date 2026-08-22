# Chemy Scientific Credibility Audit — v2.0

**Audit date:** 2026-08-21  
**Audited revision:** `6e3a240` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.9.md`](CODEX_AUDIT_v1.9.md), revision `99bf4ab`  
**Earlier audits:** [`CODEX_AUDIT_v1.8.md`](CODEX_AUDIT_v1.8.md), [`CODEX_AUDIT_v1.7.md`](CODEX_AUDIT_v1.7.md), [`CODEX_AUDIT_v1.6.md`](CODEX_AUDIT_v1.6.md), [`CODEX_AUDIT_v1.5.md`](CODEX_AUDIT_v1.5.md), [`CODEX_AUDIT_v1.4.md`](CODEX_AUDIT_v1.4.md), [`CODEX_AUDIT_v1.3.md`](CODEX_AUDIT_v1.3.md), [`CODEX_AUDIT_v1.2.md`](CODEX_AUDIT_v1.2.md), [`CODEX_AUDIT_v1.1.md`](CODEX_AUDIT_v1.1.md), [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md)  
**Auditor:** OpenAI Codex  
**Scope:** Delta audit against v1.9, including source/documentation comparison, strict build, complete tests, fresh coverage, 48-record RDKit reproduction, four-conformer UFF reproduction, both Molfile interoperability directions, dependency advisories, thermodynamics expansion, and repository hygiene.

## 1. Executive conclusion

Revision `6e3a240` is the strongest remediation revision in this audit series. It fixes the selective force-field comparison identified in v1.9, builds a substantially real two-way RDKit/Chemy interoperability workflow, expands the descriptor corpus from 32 to 48 molecules, improves NIST/Shomate coverage, hardens coverage aggregation, requires `M  END`, and replaces the repository-wide claim of universal scientific verification with an honest evidence taxonomy.

The butane defect is materially resolved. A new pinned generator calculates all four RDKit UFF totals from the exact C# coordinates. Chemy's torsional term was corrected by distributing the central-bond barrier across neighbor-pair interactions. All four conformers now agree with RDKit within the 0.50 kcal/mol gate:

| Conformer | Chemy | RDKit UFF | Difference |
|---|---:|---:|---:|
| Anti 180° | 7.3282 | 7.3147 | 0.0135 kcal/mol |
| Gauche 60° | 16.1421 | 16.1286 | 0.0135 kcal/mol |
| Eclipsed 120° | 13.1277 | 12.7332 | 0.3945 kcal/mol |
| Syn-eclipsed 0° | 45.7048 | 45.3103 | 0.3945 kcal/mol |

This is genuine external numerical validation for one molecule and one coordinate scan. It does not establish full UFF equivalence across atom types, hybridizations, rings, or optimization trajectories, but it closes the specific contradiction from v1.9.

The interoperability workflow is also substantially genuine: RDKit creates files before the .NET run; Chemy parses those fixtures; Chemy emits files during tests; RDKit parses and checks them afterward. Neutral, anionic, cationic, and zwitterionic cases are present. However, several checks remain conditional, and the live run exposed a real format defect: Chemy's header says `2D` while files contain non-zero Z coordinates. RDKit warns and repairs the dimensional tag. The workflow therefore proves useful subset interoperability, not flawless coordinate-format conformance.

The newly labeled 16-record **prospective** corpus is useful post-development evidence because descriptor implementation code did not change in this revision. Nevertheless, its prospective status is not independently auditable: molecule selection, expected values, tolerances, and first evaluation all appear in the same commit. There is no earlier frozen manifest, preregistration, blinded evaluator, or prior checksum. It should be described as a new evaluation partition unless its selection chronology can be documented externally.

**Overall claim-adjusted credibility: 7.5 / 10**, up from 7.1. Chemy is now a well-engineered and scientifically disciplined educational/prototyping toolkit with credible external evidence in descriptors, a narrow UFF case, Shomate thermodynamics, and basic CTfile interoperability. It remains below publication-grade validation for broad force-field behavior, conformer generation, spectroscopy, empirical thermodynamic fallback, reaction networks, and environmental pathways.

### Credibility ratings

| Context | v1.9 | v2.0 | Interpretation at v2.0 |
|---|---:|---:|---|
| Software implementation quality | 8.8 | **9.0 / 10** | Clean strict build, reproducible external gates, improved parser and coverage merge |
| Chemistry education/demonstrations | 8.6 | **8.8 / 10** | Evidence categories now distinguish analytical, external, regression, and heuristic results |
| Developer prototyping | 8.5 | **8.8 / 10** | Strong cross-tool and benchmark scaffolding |
| Quantitative scientific analysis | 5.6 | **6.3 / 10** | Four-conformer UFF agreement, wider descriptor evaluation, broader Shomate checks |
| Research/publication use | 4.3 | **5.0 / 10** | Several reproducible external comparisons now exist; domains remain narrow |
| Safety-of-scope | 4.4/5 | **4.5 / 5** | Heuristic domains are more explicitly classified |
| Environmental decisions | 2.9 | **3.0 / 10** | Documentation is more honest; empirical evidence remains absent |
| **Overall claim-adjusted credibility** | **7.1** | **7.5 / 10** | Major external-validation corrections with bounded remaining gaps |

The score is a structured engineering judgment, not a statistical confidence interval.

## 2. Reproduced evidence

| Check | v1.9 | v2.0 result |
|---|---:|---:|
| Release build with warnings as errors | Passed | **Passed: 0 warnings, 0 errors** |
| Automated tests | 146 passed | **148 passed, 0 failed, 0 skipped** |
| Fresh line coverage | 81.46% | **81.53%** (3,823 / 4,689) |
| Fresh branch coverage | 74.20% | **74.44%** (2,336 / 3,138) |
| Descriptor fixture | 32 records | **48 records reproduced with RDKit 2025.09.2** |
| Descriptor SHA-256 | `fda1ca...` | **`0ca6126ff1ed8b3842ef430143fd49e5c7e723544f9c26179e0d63ed9a80f39d` confirmed** |
| UFF conformers externally generated | 2 asserted / 4 available | **4 / 4 generated and asserted** |
| UFF artifact SHA-256 | Absent | **`4cd1aa362f4b95c7808ba215715c73de62263ea56ba52076de1e2d1368b68b71` verified** |
| RDKit → Chemy path | Absent | **Executed through generated fixtures and .NET tests** |
| Chemy → RDKit path | Hard-coded fixtures | **Executed on live Chemy test output** |
| Species classes | Neutral/anion/cation | **Neutral, anion, cation, zwitterion, multi-record SDF** |
| Vulnerable NuGet packages | None | **None reported, including transitive dependencies** |
| Worktree after verification | Clean | **Clean; generated committed fixtures are deterministic** |

The RDKit run emitted this warning while parsing a Chemy export:

```text
Warning: molecule is tagged as 2D, but at least one Z coordinate is not zero. Marking the mol as 3D.
```

That is an evidence-backed interoperability defect, not a test-environment issue.

## 3. v1.9 finding disposition

| v1.9 finding | v2.0 status | Evidence |
|---|---|---|
| Only two UFF conformers gated | **Resolved** | All four totals are generated and checked |
| ~20.40 kcal/mol barrier discrepancy | **Resolved for this scan** | Source torsion calculation corrected; maximum difference now 0.3945 |
| UFF provenance not scripted | **Resolved** | Pinned generator and hashed JSON artifact added |
| Molfile gate was one-way | **Substantially resolved** | CI executes RDKit → Chemy → RDKit sequence |
| Formula/topology/coordinate assertions absent | **Partially resolved** | Chemy → RDKit checks formula/count/charge/non-zero coordinates; reverse checks are shallower |
| Zwitterion absent | **Resolved** | Glycine zwitterion fixture and charge round trip added |
| `M  END` optional in Molfile parser | **Resolved** | Parser now rejects a missing terminator |
| Coverage reports double-counted | **Substantially resolved** | Lines deduplicated by filename/line; branch merging improved |
| Comprehensive-verification overclaim | **Resolved at document preamble** | Four evidence categories and limitations are explicit |
| Prospective corpus absent | **Improved, not fully auditable** | New partition exists; freeze chronology is not recorded before this commit |
| Other predictive domains unvalidated | **Mostly unresolved** | Shomate expanded; spectroscopy, fallback thermodynamics, EcoClean remain limited |

## 4. Detailed findings

### 4.1 Butane UFF validation is now authentic and complete for its stated fixture

`generate_uff_reference.py` asserts RDKit `2025.09.2`, constructs all 14 atoms at the exact C# positions, calculates all four total energies, and verifies a hashed artifact. The C# test compares every total within 0.50 kcal/mol and reports relative values.

The force-field correction is chemically and structurally motivated: the torsional barrier associated with a central bond is divided among the combinations of neighboring atoms instead of applying the full barrier to every torsion quadruple. This explains and repairs the previous overcounting.

Limits that must remain explicit:

- the Chemy torsion constant remains a generic `2.5` rather than a complete UFF atom-type parameterization;
- only butane's sp3 C–C environment is externally tested;
- initial handcrafted coordinates are checked, not an externally compared optimized geometry;
- the 0.3945 kcal/mol residual for the two eclipsed cases is substantially larger than the 0.0135 residual for anti/gauche;
- no energy-component artifact proves agreement of bond, angle, inversion, torsion, and nonbonded terms separately;
- the optimizer is not cross-validated against RDKit minima or convergence behavior.

**Assessment:** credible validation of one all-atom butane coordinate scan; not general UFF conformance.

### 4.2 The 48-record descriptor experiment broadens evidence, but “prospective” needs provenance

The 16 new molecules include larger and more varied bioactive structures. Reported partition metrics are:

| Partition | TPSA MAE / max | LogP MAE / max | QED MAE / max |
|---|---:|---:|---:|
| Tuning (N=16) | 0.0000 / 0.0000 | 0.2289 / 0.5630 | 0.0280 / 0.2030 |
| Expanded regression (N=16) | 0.0000 / 0.0000 | 0.1953 / 0.6320 | 0.0111 / 0.0430 |
| New evaluation (N=16) | 0.0331 / 0.5300 | 0.4548 / 1.1540 | 0.0374 / 0.2080 |
| Combined (N=48) | 0.0110 / 0.5300 | 0.2930 / 1.1540 | 0.0255 / 0.2080 |

The deterioration in LogP on the new partition is scientifically informative and argues against fixture shaping. The evaluation also exposes a TPSA outlier rather than forcing exact agreement.

However, “prospective” normally means the evaluation protocol and cases were fixed before outputs were observed or model choices were made. The repository history shows the list, generated RDKit values, tolerances, test, and published results arriving together. Descriptor implementation code did not change in this commit, which supports a **post-development evaluation** interpretation, but cannot prove blindness or preregistration.

To make the next set auditable, commit a signed/hashed manifest and acceptance protocol first, then add results in a later commit without descriptor modifications. An independent evaluator or hidden CI corpus would be stronger.

### 4.3 The interoperability pipeline is real but has fail-open edges

The CI ordering is meaningful:

1. RDKit creates four Molfiles and a multi-record SDF.
2. The .NET build/test parses RDKit fixtures.
3. A .NET test writes current Chemy exports.
4. RDKit parses the live Chemy exports and validates selected properties.

This is substantially bidirectional and closes the central v1.9 criticism.

Remaining reliability gaps:

- if the Chemy export directory is absent, the Python verifier returns success;
- missing individual Chemy files produce warnings and are skipped rather than failing;
- the SDF check uses the requested `input_dir`, not the resolved `actual_dir`, so it can silently skip an SDF found only in a fallback directory;
- the RDKit → Chemy test returns successfully when its fixture directory is absent;
- each RDKit file check is guarded by `if (File.Exists(...))`, so missing files can silently remove coverage;
- reverse-direction assertions check counts/total charges but not full formulas, bond orders/endpoints, or coordinate tolerances;
- SDF validation checks only that at least three records parse, not record identity or properties.

The current clean CI sequence supplies all files, so the audit did observe both directions passing. These fail-open paths nevertheless reduce the gate's ability to detect future workflow or fixture regressions.

### 4.4 Chemy writes a 2D header for 3D coordinates

`MolfileExporter` writes this fixed program/header line:

```text
Chemy10 08202600002D ...
```

The exported atom records can contain non-zero Z coordinates. RDKit detects the contradiction, warns, and changes the molecule to 3D. This means structural parsing succeeds, but the dimensional metadata is false.

The fix is straightforward: emit `3D` whenever any significant Z coordinate exists and `2D` otherwise, then assert the dimensional flag and coordinate tolerances in the cross-tool gate. Until then, “3D coordinate fidelity validated against RDKit” is too strong.

### 4.5 Shomate validation breadth improved

The benchmark now covers H2O, CO2, CH4, N2, O2, and H2 at multiple temperatures and checks enthalpy, entropy, and heat capacity. It reports aggregate MAE and individual tolerances. This is stronger external numerical evidence for the tabulated Shomate path.

It does not validate the separate empirical thermodynamic fallback for arbitrary organics, reaction-network thermodynamics, phase handling beyond the listed gases, or broad coefficient-database coverage. Documentation should keep those paths distinct.

### 4.6 Coverage merging is safer

The verifier sorts reports, deduplicates executable lines by filename/line number, and merges branch counts rather than simply summing root totals. This addresses overlapping-report inflation for common cases.

Potential edge cases remain: identical relative filenames from different assemblies can collide, and branch identity is represented only at line level with the maximum covered count. The current CI produces one report, so neither affects the reproduced result.

### 4.7 Evidence taxonomy materially improves documentation credibility

The scientific benchmark document now distinguishes analytical identities, external comparisons, internal regressions, and qualitative heuristics. It explicitly warns that not all subsystems have equal evidence. This is exactly the kind of claim discipline a scientific tool needs.

Individual older benchmark entries should still be tagged consistently with those categories, rather than relying only on the preamble. The environmental, spectroscopy, lead-exploration, and empirical-fallback sections are the highest priorities.

### 4.8 A 3.3 MB internal session transcript was committed

`codex-session-01a021dc-8782-7be3-b743-4b7aa1d49a36.md` contains 35,213 lines and 3,325,418 bytes of agent conversation and command output, including absolute local paths and extensive historical repository data. It adds no runtime or scientific function.

The limited secret-pattern scan performed by this audit did not reveal an obvious credential, but that is not a sufficient privacy review of 3.3 MB of transcript. Such artifacts can preserve prompts, local metadata, terminal output, and accidentally exposed secrets. The file should be reviewed, removed from the repository if not intentionally published, and added to ignore rules. If any secret is found, removal from the latest tree is insufficient; history cleanup and credential rotation would be required.

## 5. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.9 | v2.0 | Principal finding |
|---|---:|---:|---|
| Elements/molar mass | 4.5 | **4.6** | Wider 48-record reference set |
| SMILES parsing | 3.1 | **3.3** | New complex evaluation inputs parse; syntax remains a subset |
| Reaction balancing/stoichiometry | 4.0–4.2 | **4.0–4.2** | No material change |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Strong narrow textbook implementations |
| Reaction-network integration | 3.0 | **3.0** | No external dynamic benchmark |
| Hückel solver/interpretation | 2.6–4.2 | **2.6–4.2** | No material change |
| Shomate thermodynamics | 3.8 | **4.2** | Six species, multiple temperatures, H/S/Cp checks |
| Empirical thermodynamic fallback | 1.5 | **1.5** | Expanded Shomate evidence does not validate fallback |
| Molecular mechanics | 3.0 | **3.8** | Four-conformer external agreement after a real source fix |
| 3D geometry/conformers | 2.9 | **3.1** | Cross-tool coordinates exercised; header metadata defect remains |
| TPSA subset | 4.0 | **4.2** | New partition reveals bounded non-zero error |
| LogP/MR subset | 3.4 | **3.6** | Wider evaluation shows MAE 0.4548 and max 1.154 |
| QED-inspired score | 3.9 | **4.0** | Wider evaluation remains within published regression limits |
| Physicochemical profile | 4.0 | **4.2** | Broader post-development evidence |
| Spectroscopy | 2.3 | **2.3** | Still no external error distribution |
| Ring perception | 3.8 | **3.8** | No material independent expansion |
| Lead exploration | 2.2 | **2.2** | No validation of discovery utility |
| EcoClean | 2.2 | **2.2** | Explicitly heuristic; still empirically unvalidated |
| Molfile/SDF parser/exporter | 3.7 | **4.0** | Actual two-way subset pipeline; fail-open checks and 2D/3D defect remain |

## 6. Priority remediation

### P0 — make interoperability fail closed

1. Fail if any expected export directory, Molfile, or SDF is missing.
2. Use the resolved directory for every Molfile and SDF check.
3. Remove conditional returns/skips from the CI-specific .NET interoperability test.
4. Verify formulas, atom identities, bond orders/endpoints, coordinates, charges, and SDF record identity in both directions.
5. Emit correct `2D`/`3D` dimensional metadata and gate it.

### P1 — make evaluation independence auditable

1. Rename the current partition to `post_development_evaluation` unless freeze chronology can be evidenced.
2. Commit the next evaluation manifest and thresholds before generating or viewing outputs.
3. Add confidence intervals, bias, percentiles, and unsupported-input rates.
4. Expand UFF comparison across atom types, hybridizations, rings, and optimized geometries.
5. Add at least one external quantitative spectroscopy or empirical-thermodynamics benchmark.

### P2 — repository and CI hygiene

1. Review and remove the committed session transcript; add an ignore rule for session artifacts.
2. Audit repository history for credentials or private data in that transcript.
3. Avoid tests writing into the source tree; use a declared artifacts directory shared between CI steps.
4. Identify coverage lines by assembly/module plus canonical source path.
5. Apply the evidence category label to every benchmark section and API claim.

## 7. Acceptance criteria for v2.1

A further material score increase should require:

- fail-closed, structure-complete Chemy ↔ RDKit interoperability;
- correct 2D/3D CTfile headers with numerical coordinate comparison;
- a prospectively committed evaluation protocol demonstrably preceding results;
- external UFF coverage beyond one butane scan;
- independent quantitative evidence for at least one currently heuristic predictive domain;
- removal and privacy review of the committed agent-session transcript.

## 8. Final verdict

The developers responded directly to the strongest v1.9 findings. The UFF discrepancy was not hidden or tolerance-washed; its source was corrected and all four external references now pass. The interoperability workflow now executes both ecosystems, the descriptor set is broader, Shomate validation is stronger, and the documentation finally distinguishes levels of evidence.

Those improvements justify raising Chemy to **7.5 / 10 overall credibility**. The project is increasingly exemplary as an educational and prototyping tool. Its next step toward research-grade credibility is to make evaluation independence auditable, make cross-tool checks fail closed and structurally complete, and extend independent numerical validation beyond the currently well-tested islands.

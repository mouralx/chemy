# Chemy Scientific Credibility Audit — v1.9

**Audit date:** 2026-08-21  
**Audited revision:** `99bf4ab` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.8.md`](CODEX_AUDIT_v1.8.md), revision `0a8eb4e`  
**Earlier audits:** [`CODEX_AUDIT_v1.7.md`](CODEX_AUDIT_v1.7.md), [`CODEX_AUDIT_v1.6.md`](CODEX_AUDIT_v1.6.md), [`CODEX_AUDIT_v1.5.md`](CODEX_AUDIT_v1.5.md), [`CODEX_AUDIT_v1.4.md`](CODEX_AUDIT_v1.4.md), [`CODEX_AUDIT_v1.3.md`](CODEX_AUDIT_v1.3.md), [`CODEX_AUDIT_v1.2.md`](CODEX_AUDIT_v1.2.md), [`CODEX_AUDIT_v1.1.md`](CODEX_AUDIT_v1.1.md), [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md)  
**Auditor:** OpenAI Codex  
**Scope:** Delta audit against v1.8, with strict build, complete tests, fresh coverage, RDKit reference reproduction, independent RDKit UFF reconstruction, Molfile interoperability inspection/execution, dependency advisories, and documentation-to-code comparison.

## 1. Executive conclusion

Revision `99bf4ab` contains several worthwhile improvements:

- descriptor partitions now have maximum-error gates in addition to MAE gates;
- the README uses a live GitHub Actions badge and threshold-based coverage wording;
- “zero dependencies” is narrowed to zero runtime scientific-library dependencies;
- Molfile/API documentation now describes the implemented field subset rather than claiming ISO/IUPAC industrial compliance;
- positive and negative formal-charge round trips are tested;
- coverage verification handles more than one report instead of silently selecting the first;
- RDKit is invoked in CI to parse neutral, anionic, and cationic Molfile fixtures.

However, the revision's two headline external-validation claims are materially overstated.

First, `verify_molfile_interop.py` calls itself **bidirectional** cross-tool verification, but implements only one direction: three hard-coded strings that resemble Chemy output are passed to RDKit. It never executes Chemy's exporter, never feeds an RDKit-produced file into Chemy's parser, and never invokes .NET at all. It reads `expected_formula` but never compares it, does not check coordinates or topology, and contains no zwitterion despite its module description. This is a useful RDKit-acceptance fixture test, not bidirectional interoperability.

Second, the test named `MatchesRDKitUffReference` checks RDKit reference energies for only anti and gauche butane—the two conformers that agree within 0.023 kcal/mol. It labels the other two outputs as torsional barriers without checking them. Independent reproduction with pinned RDKit on exactly the test coordinates found:

| Conformer | Chemy | RDKit UFF | Absolute difference |
|---|---:|---:|---:|
| Anti 180° | 7.3377 | 7.3147 | 0.0230 kcal/mol |
| Gauche 60° | 16.1516 | 16.1286 | 0.0230 kcal/mol |
| Eclipsed 120° | 33.1359 | 12.7332 | **20.4027 kcal/mol** |
| Syn-eclipsed 0° | 65.7130 | 45.3103 | **20.4027 kcal/mol** |

Relative to anti, Chemy predicts the 120° barrier as 25.7982 kcal/mol while RDKit gives 5.4184 kcal/mol. The new external evidence therefore reveals a major torsional-energy disagreement, but the committed test excludes exactly those values from its reference assertions. Calling the whole torsion-barrier test a match is not supported.

**Overall claim-adjusted credibility remains 7.1 / 10.** Engineering quality improves, but the selective and mislabeled validation prevents a score increase. Chemy remains credible for education and prototyping, with a strong descriptor regression process. It is not quantitatively validated for force-field barriers, prospective chemical-space prediction, full file interoperability, environmental decisions, or research publication.

### Credibility ratings

| Context | v1.8 | v1.9 | Interpretation at v1.9 |
|---|---:|---:|---|
| Software implementation quality | 8.7 | **8.8 / 10** | Strong clean build, gates, integrity controls, and clearer API scope |
| Chemistry education/demonstrations | 8.6 | **8.6 / 10** | Broad and useful; force-field test name may mislead learners |
| Developer prototyping | 8.4 | **8.5 / 10** | Strong regression platform with early external-tool scaffolding |
| Quantitative scientific analysis | 5.7 | **5.6 / 10** | Maximum gates improve rigor, but barrier comparison exposes a large mismatch |
| Research/publication use | 4.4 | **4.3 / 10** | External checks are not yet complete or honestly characterized |
| Safety-of-scope | 4.4/5 | **4.4 / 5** | No new high-stakes predictive claim |
| Environmental decisions | 2.9 | **2.9 / 10** | Still no empirical pathway validation |
| **Overall claim-adjusted credibility** | **7.1** | **7.1 / 10** | Better engineering offset by selective force-field validation and one-way interop |

This rating is a structured engineering judgment, not a statistical confidence interval.

## 2. Reproduced evidence

| Check | v1.8 | v1.9 result |
|---|---:|---:|
| Release build with warnings as errors | Passed | **Passed: 0 warnings, 0 errors** |
| Automated tests | 146 passed | **146 passed, 0 failed, 0 skipped** |
| Fresh line coverage | 81.39% | **81.46%** (3,813 / 4,681) |
| Fresh branch coverage | 74.07% | **74.20%** (2,324 / 3,132) |
| Coverage floors | 80% / 70% | **Both enforced and passed** |
| RDKit descriptor fixture | 32 passed | **32 passed with RDKit 2025.09.2** |
| Dataset SHA-256 | `fda1ca...` | **`fda1ca39cd853bd49bcb1827abe68e1668d55a60c6bfe83deb6217ea20a5a0a1` confirmed** |
| Vulnerable NuGet packages | None | **None reported, including transitive dependencies** |
| Molfile fixtures accepted by RDKit | Absent | **3 / 3 parsed with expected total charge** |
| Actual Chemy → RDKit runtime pipeline | Absent | **Absent** |
| RDKit → Chemy pipeline | Absent | **Absent** |
| Anti/gauche UFF reference | Absent | **Independently confirmed** |
| Eclipsed/syn UFF reference | Absent | **Large disagreement discovered; not gated by repository test** |

The untracked `codex-session-01a021dc-8782-7be3-b743-4b7aa1d49a36.md` file existed before this report was created and was not modified or treated as repository evidence.

## 3. v1.8 finding disposition

| v1.8 finding | v1.9 status | Evidence |
|---|---|---|
| Mean-only descriptor gates | **Resolved** | TPSA, LogP, and QED maxima are gated per partition |
| No external butane targets | **Partially resolved** | Anti/gauche targets are authentic; barrier conformers omitted |
| Quantitative torsional accuracy unknown | **Now contradicted for tested coordinates** | 120° and 0° totals differ from RDKit by ~20.40 kcal/mol |
| No independent Molfile reader | **Improved** | RDKit parses three fixed fixtures |
| No bidirectional interop | **Unresolved** | RDKit output is never parsed by Chemy; Chemy exporter is not executed |
| Broad Molfile compliance claims | **Substantially resolved** | Exporter and API documentation describe supported fields |
| Missing positive-charge coverage | **Resolved internally** | Pyridinium-style +1 round trip added |
| Static CI/coverage badges | **Resolved** | Live workflow badge and threshold badge used |
| “Zero dependencies” wording | **Resolved** | Narrowed to zero runtime scientific-library dependencies |
| Multiple coverage reports ambiguous | **Partially resolved** | All are summed, but overlapping assemblies can be double-counted |
| Prospective descriptor corpus absent | **Unresolved** | Dataset remains tuning plus expanded regression |

## 4. Detailed findings

### 4.1 The RDKit UFF values are real but selectively applied

The audit reconstructed RDKit molecules with explicit hydrogens and assigned the exact 14 coordinate triples used by the C# test. `UFFGetMoleculeForceField(...).CalcEnergy()` reproduced the committed anti and gauche constants to the shown precision. Those two constants are authentic.

The same calculation also provides the omitted eclipsed and syn values. They disagree sharply with Chemy. The fact that both omitted conformers differ by the same 20.4027 kcal/mol suggests a discrete torsion-term or phase/convention defect rather than random numerical noise. Locating that component requires an energy-term decomposition, but the quantitative disagreement itself is direct evidence.

The current test is vulnerable to confirmation bias:

- its name says the **torsion barrier** matches RDKit;
- it asserts external agreement only for conformers that pass;
- the two structures actually described as barrier/maximum have no RDKit assertions;
- only absolute totals are compared, while conformational science normally needs relative energies on a consistent baseline;
- reference generation is not scripted or stored with coordinates/version/procedure.

**Required correction:** generate all four RDKit values in a hash-locked Python artifact, assert relative energies for every conformer, and treat the current failure as a force-field defect rather than widening tolerances. Add term-by-term comparisons to isolate the cause.

### 4.2 Maximum-error descriptor gates are a genuine improvement

Each tuning, expanded-regression, and combined evaluation now enforces:

- TPSA maximum error ≤ 0.05 Å²;
- LogP maximum error ≤ 0.70;
- QED maximum error ≤ 0.25.

This prevents a single extreme result from being hidden by a passing mean. Existing maxima—0.632 for LogP and 0.203 for QED—pass with modest headroom. These thresholds are regression acceptance limits, not demonstrated clinical or experimental tolerances.

Percentiles, bias, confidence intervals, unsupported-input rates, and prospective evaluation remain absent. No algorithm changed in this revision, so descriptor credibility is stable rather than materially expanded.

### 4.3 The interoperability gate is one-way and fixture-only

The script successfully proves that RDKit 2025.09.2 sanitizes three committed Molfile strings and observes total charges 0, -1, and +1. This is useful evidence that the textual subset resembles valid V2000.

It does not prove its advertised workflow:

- no Chemy executable or library is called;
- the strings are manually embedded duplicates, not current exporter output;
- RDKit never serializes a molecule for Chemy to parse;
- `expected_formula` is assigned but never tested;
- `rdMolDescriptors` is imported but unused;
- atom counts, bond endpoints/orders, formulas, and coordinates are not asserted;
- no SDF is tested;
- no zwitterion is present despite the docstring;
- the success message calls the strings “Chemy-exported” without runtime provenance.

Consequently, a future exporter regression could produce invalid output while this CI gate still passes unchanged. A future parser regression would also be invisible.

**Required correction:** add a small .NET fixture command that writes actual Chemy output, parse and verify it with RDKit, then have RDKit write independent Molfile/SDF records that a .NET test parses and verifies. Compare structure, charge, coordinates, and supported properties in both directions.

### 4.4 Molfile claim wording is much better

The exporter no longer claims industrial-grade ISO/IUPAC compliance or compatibility with a list of applications. The API reference now lists the supported CTfile fields. This is an important claim-fidelity correction.

The implemented limitations from v1.8 remain: isotope, radical, stereo, atom-map, query, valence, and SDF data-field fidelity are absent; malformed `M  CHG` records can be partially accepted; `FromMolfileV2000` does not require `M  END`; and unsupported exporter bond types fall back to single. The documentation is now closer to this actual scope.

The parser/exporter summaries say coordinate fidelity is “validated against RDKit,” but the new script never compares coordinates. That clause should be narrowed until coordinate assertions exist.

### 4.5 Coverage aggregation fixes one problem but can introduce another

The script now reads every matching Cobertura report and computes a count-weighted ratio. In a clean current CI run there is one report, and the result is correct.

If several reports contain overlapping coverage for the same assembly, summing their counters double-counts code and weights results by the number of runs rather than merging covered lines/branches. The script also does not sort inputs, though addition is order independent. It should either require one aggregate report or merge coverage by unique module/document/line/branch identity.

### 4.6 Documentation still has a repository-wide credibility overstatement

README and API changes are appropriately scoped. However, `SCIENTIFIC_VERIFICATION_BENCHMARKS.md` still opens by claiming “comprehensive, end-to-end scientific verification” of every algorithm and says every calculation is validated against established laws and peer-reviewed standards. That is contradicted by the absence of independent quantitative evidence for spectroscopy, empirical thermodynamic fallback, reaction networks, conformer quality, and environmental pathways—and now by the measured force-field barrier disagreement.

The document should distinguish analytical identity tests, external numerical comparisons, internal regressions, and unvalidated heuristics instead of grouping them all as comprehensive verification.

## 5. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.8 | v1.9 | Principal finding |
|---|---:|---:|---|
| Elements/molar mass | 4.5 | **4.5** | Stable hash-locked reference evidence |
| SMILES parsing | 3.1 | **3.1** | No material change |
| Reaction balancing/stoichiometry | 4.0–4.2 | **4.0–4.2** | No material change |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Strong narrow textbook implementations |
| Reaction-network integration | 3.0 | **3.0** | No external validation |
| Hückel solver/interpretation | 2.6–4.2 | **2.6–4.2** | No material change |
| Shomate thermodynamics | 3.8 | **3.8** | No new breadth |
| Empirical thermodynamic fallback | 1.5 | **1.5** | Still independently unvalidated |
| Molecular mechanics | 3.3 | **3.0** | External evidence now exposes a large barrier discrepancy |
| 3D geometry/conformers | 2.9 | **2.9** | No external optimized-geometry corpus |
| TPSA subset | 3.9 | **4.0** | Maximum-error gate improves regression control |
| LogP/MR subset | 3.3 | **3.4** | Maximum-error gate improves regression control |
| QED-inspired score | 3.8 | **3.9** | Maximum-error gate improves regression control |
| Physicochemical profile | 4.0 | **4.0** | Stable evidence; no prospective data |
| Spectroscopy | 2.3 | **2.3** | No external error distribution |
| Ring perception | 3.8 | **3.8** | No material change |
| Lead exploration | 2.2 | **2.2** | No validation of discovery utility |
| EcoClean | 2.2 | **2.2** | Pathways remain speculative |
| Molfile/SDF parser/exporter | 3.6 | **3.7** | RDKit accepts fixed samples; bidirectional runtime interop not established |

## 6. Priority remediation

### P0 — fix the misleading external gates

1. Add RDKit reference values for all four butane conformers and gate relative energies.
2. Investigate the approximately 20.4027 kcal/mol eclipsed/syn discrepancy by energy component.
3. Rename the current Molfile job from “bidirectional” to “RDKit fixture acceptance” until both directions execute.
4. Assert formulas, topology, coordinates, and every documented species category in the interop script.
5. Remove the unused formula data/import or, preferably, make them real assertions.

### P1 — create genuine cross-tool and prospective evidence

1. Generate Chemy Molfiles during CI and pass those exact files to RDKit.
2. Generate RDKit Molfiles/SDFs during CI and pass them to Chemy.
3. Freeze a prospective descriptor set before further model work.
4. Add independent quantitative benchmarks outside descriptors and Molfile parsing.
5. Publish provenance scripts and artifacts for every external force-field number.

### P2 — improve robustness and documentation

1. Replace the comprehensive-verification preamble with a per-domain evidence classification.
2. Merge coverage by unique source identity or require one report.
3. Reject malformed/unsupported CTfile fields explicitly.
4. Add SDF property, isotope, stereo, radical, and atom-map scope tests or document their rejection.
5. Keep regression thresholds distinct from claims of scientific accuracy.

## 7. Acceptance criteria for v2.0

A future score increase should require:

- all four butane conformers matching pinned RDKit relative energies within justified tolerances;
- an automated, actual Chemy ↔ RDKit two-way pipeline;
- structural and coordinate assertions, not parse success alone;
- a prospectively frozen evaluation corpus;
- at least one external quantitative benchmark for another predictive domain;
- removal of the repository-wide comprehensive-verification claim.

## 8. Final verdict

The developers improved error gating, public wording, charge coverage, and external-tool scaffolding. Those changes are useful and the project remains well engineered.

The independent audit also demonstrates why external validation must be complete rather than selective. The two butane values included in the gate agree; the two omitted barrier values disagree dramatically. Likewise, RDKit parsing hard-coded text is useful, but it is not a bidirectional Chemy/RDKit pipeline. Until those claims match the executed experiment, the overall credibility rating appropriately remains **7.1 / 10**.

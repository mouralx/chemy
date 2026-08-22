# Chemy Scientific Credibility Audit — v1.2

**Audit date:** 2026-08-21  
**Audited revision:** `315a495` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.1.md`](CODEX_AUDIT_v1.1.md), revision `a106d55`  
**Historical audit:** [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md), revision `236fac3`  
**Auditor:** OpenAI Codex  
**Scope:** Delta review against v1.1, public documentation, scientific implementation fidelity, public execution paths, tests, coverage, dependency health, applicability controls, and reproducibility.

## 1. Executive conclusion

Revision `315a495` is a meaningful improvement over the v1.1 revision. It fixes several concrete correctness and safety-of-interpretation defects rather than merely adding new method names:

- unsupported SMILES stereochemistry now fails explicitly instead of silently becoming a different molecule;
- the acetamide TPSA double-counting error is corrected and locked to the expected 43.09 Å² result;
- Shomate requests outside the stored coefficient interval now fail instead of mixing clamped and unclamped temperatures;
- force-field iteration and convergence reporting are more honest;
- the public reaction API now exposes independent nullspace pathways;
- EcoClean no longer manufactures a near-99% efficiency or a fixed environmental half-life;
- the high-severity `Microsoft.OpenApi 2.0.0` advisory is resolved;
- the README removes several of its strongest false claims.

The revision does **not** resolve all P0 and P1 findings, despite the commit message saying it does. The public ADMET result still returns unsupported hERG, CYP450, and BBB classifications with medical-sounding language. Secondary documentation still publishes the old drug-safety, non-toxic lead, 99.4% PFAS mineralization, “100% verified,” and exact-gradient claims. The Wildman–Crippen and Ertl classifiers remain partial implementations labeled as complete published methods. The force field remains UFF-inspired rather than full UFF, and the general thermodynamics engine remains a heuristic Benson-like fallback despite the README shifting attention to the separate seven-species Shomate class.

### Credibility ratings

| Context | v1.0 | v1.1 | v1.2 | Interpretation at v1.2 |
|---|---:|---:|---:|---|
| Software implementation quality | 6.0 | 6.6 | **7.0 / 10** | Better errors, termination semantics, dependency health, and public algebra API |
| Chemistry education/demonstrations | 7.0 | 7.2 | **7.5 / 10** | Increasingly useful if heuristic outputs and applicability are taught explicitly |
| Developer prototyping | 6.0 | 6.5 | **6.9 / 10** | Solid experimental toolkit; scientific boundaries remain inconsistent across APIs |
| Quantitative scientific analysis | 3.0 | 3.4 | **3.7 / 10** | Several narrow calculations improved; headline descriptor fidelity is still unproven |
| Research/publication use | 2.0 | 2.3 | **2.6 / 10** | Still lacks external datasets, accuracy statistics, uncertainty, and reference-tool comparisons |
| Drug-safety decisions | 1.0 | 1.0 | **1.0 / 10** | Unsupported hERG/CYP/BBB conclusions are unchanged |
| Environmental decisions | 1.0 | 1.0 | **2.5 / 10** | Fabricated quantitative outcomes removed; qualitative pathways remain speculative |
| **Overall claim-adjusted credibility** | **4.5** | **4.9** | **5.3 / 10** | Real remediation progress, still constrained by one critical unsafe API and broad documentation drift |

This rating is a structured engineering judgment, not a statistical confidence interval. It weights implementation fidelity, validation quality, applicability controls, failure behavior, reproducibility, and the consequence of overinterpreting outputs.

### Bottom-line recommendation

Chemy is increasingly credible as an **educational and experimental chemistry toolkit**. It is not yet credible as a validated drug-safety, lead-optimization, environmental-remediation, general thermodynamic, quantitative spectroscopy, or research-grade molecular-modelling platform.

## 2. Reproduced evidence

### Engineering checks

| Check | v1.1 | v1.2 result |
|---|---:|---:|
| Automated tests | 127 passed | **130 passed, 0 failed, 0 skipped** |
| Core line coverage | 79.96% | **80.14%** (4,698 / 5,862) |
| Core branch coverage | 69.84% | **70.06%** (2,018 / 2,880) |
| High/critical vulnerable packages | 1 high | **None reported by NuGet audit** |
| CI | Build/test | **Unchanged: build/test only** |
| External scientific validation corpus | None | **None found** |
| Revision-linked benchmark artifact | None | **None found** |

### Independent public-API probes

```text
F[C@](Cl)(Br)I:
  throws NotSupportedException (fixed: stereochemistry no longer erased)

C@C:
  throws NotSupportedException at position 1 (fixed)

Force field with maxIterations = 0:
  Iterations = 0, Converged = false (fixed)

PFOA-like C8HF15O2:
  legacy efficiency = 0 and obsolete warning (fabricated 99.2% removed)

Ibuprofen ADMET:
  LogP = 3.42; TPSA = 37.30; QED = 0.574
  hERG = "Moderate Risk (Monitor hERG patch clamp in vitro)"
  CYP = "CYP1A2 / CYP2C9: Aromatic para-hydroxylation"
  BBB = "High BBB Permeability (CNS Active)"

C + O2 -> CO + CO2:
  Balance() deliberately reports nullity 2 and directs callers to
  BalanceIndependentPathways()
```

Passing tests show that the selected regression examples behave as intended. They do not establish empirical accuracy or published-method equivalence.

## 3. v1.1 finding disposition

| v1.1 finding | v1.2 status | Evidence |
|---|---|---|
| Fabricated EcoClean efficiency and half-life | **Resolved for new API use** | Values removed from constructor; obsolete legacy accessors return zero |
| EcoClean harmless/complete-mineralization wording | **Partially resolved** | Core wording improved; old claims remain in several docs; pathways/products remain speculative |
| Unsupported hERG/CYP/BBB conclusions | **Unresolved — Critical** | Fields, rules, strings, docs, and API examples remain |
| Silent SMILES stereochemistry loss | **Resolved by rejection** | `@`, `/`, `\`, `%` and bracket stereochemistry throw |
| Formula-derived invented topology | **Unresolved — High** | No composition-only boundary was added |
| Acetamide TPSA double counting | **Resolved for tested amides** | Contributions changed to 26.02/12.03/3.24; exact acetamide/paracetamol tests added |
| Complete Ertl 43-type fidelity | **Unresolved** | Overall classifier remains a compact subset with no external corpus |
| Complete Wildman–Crippen 68-type fidelity | **Unresolved** | No implementation change |
| Exact QED fidelity | **Unresolved** | No descriptor/alert implementation change |
| False force-field convergence and iteration count | **Resolved** | `Converged` depends only on convergence reasons; zero means zero iterations |
| Full UFF fidelity | **Unresolved** | No atom-typing/functional-form expansion |
| Nullspace basis absent from public reaction API | **Partially resolved** | New pathway API exists; sign/side semantics and minimal-positive combination remain weak |
| Old ring finder used by ADMET | **Resolved** | Aromatic ring count now uses `CycleBasis.ComputeSssr()` |
| Shomate clamp/result inconsistency | **Resolved** | Out-of-range temperature throws |
| Shomate general integration and interval coverage | **Unresolved** | Still seven standalone gas records with one interval each |
| Vulnerable transitive OpenAPI dependency | **Resolved** | NuGet audit reports no vulnerable packages |
| Misleading README language | **Partially resolved** | Major README edits are good; residual claims remain and secondary docs were not corrected |
| External validation and reproducible benchmarks | **Unresolved** | No datasets, accuracy metrics, or generated artifacts added |

## 4. Detailed analysis of the remediation

### 4.1 SMILES now fails safely, but remains a limited parser

This is one of the strongest fixes. The parser no longer silently discards `@`, `/`, `\`, `%`, or arbitrary unknown characters. Bracket stereochemistry is also rejected. A user can now distinguish “unsupported representation” from successful parsing.

This does not add stereochemistry support, multi-digit ring closures, or full OpenSMILES compliance. The correct claim is therefore “supported topological SMILES subset with explicit rejection,” not standards compliance. End-of-input validation for every malformed branch, bracket, and ring state should continue to be tested comprehensively.

**Score:** improves SMILES parsing from **2.0 to 2.8 / 5**.

### 4.2 TPSA amide correction is valid but does not make the classifier exhaustive

The revised primary-amide contribution `26.02` plus carbonyl oxygen `17.07` produces acetamide TPSA `43.09`, matching the familiar reference result. Secondary and tertiary amide nitrogen contributions were adjusted consistently, and paracetamol now has an exact 49.3 assertion. This repairs the specific scientific error found in v1.1.

However, `ErtlTpsa` still claims an “exhaustive” 43-fragment implementation while selecting from a much smaller set of handwritten branches. There is still no complete published atom-type table, precedence audit, unsupported-environment detection, or cross-validation against a large independent molecule corpus. One corrected class does not validate all N/O/P/S environments, formal charges, aromatic states, and ring cases.

**Score:** improves TPSA from **1.5 to 2.3 / 5**, not to supported status.

### 4.3 Wildman–Crippen and QED were not remediated

The LogP/MR class still describes itself as a complete 68-parameter implementation but implements only a small coarse subset. Missing environments fall into approximate nearby classes or a zero/generic result. No new exact reference tests or independent corpus were added.

QED still uses these incomplete LogP/TPSA values, simplified HBA and rotatable-bond rules, aromatic-ring approximations, and four handwritten alerts. Its ADS equation is useful, but the complete score is not equivalent to reference QED. The broad aspirin interval test remains insufficient.

The README's “published descriptors” phrasing is less dangerous than the previous ADMET-safety headline, but it still implies method fidelity not established by the code.

**Scores:** LogP/MR **1.5 / 5**, QED **2.5 / 5**, unchanged.

### 4.4 ADMET remains the highest-risk defect

No substantive ADMET safety remediation occurred. `AdmetProfile` remains documented as “comprehensive, industrial-grade ADMET.” The engine still infers:

- hERG cardiac risk from a few property thresholds;
- specific CYP enzymes and metabolic transformations from functional-group rules;
- BBB permeability and CNS activity from LogP/TPSA thresholds.

The reproduced ibuprofen output is unchanged. There is no trained model, labeled dataset, endpoint definition, calibration, uncertainty, sensitivity/specificity, applicability domain, or external validation.

The README removes “ADMET safety scoring” and “safely act as an oral medicine,” which is good. But `GETTING_STARTED.md`, `API_REFERENCE.md`, `BREAKTHROUGHS_SHOWCASE.md`, XML documentation, and the response model continue to expose the unsafe interpretation. Removing a claim from the landing page does not make the public API safe.

**Severity: Critical. Score: 0.5 / 5.** Remove these outputs, or move them into an explicitly experimental API whose type names, field names, values, warnings, and documentation cannot be mistaken for predictions.

### 4.5 EcoClean quantitative fabrication is removed; qualitative authority remains too strong

The near-99% formula and fixed class half-lives are gone. The method now declares `EvidenceLevel.Heuristic` and explicitly warns that it does not calculate kinetics, residence time, or reactor mass balance. The test no longer rewards a manufactured percentage. These are important P0 improvements.

Compatibility accessors return zero rather than a fake estimate and are marked obsolete. This is acceptable for source compatibility, although serialization frameworks may still expose them unless explicitly ignored; an obsolete zero-valued `TotalMineralizationEfficiencyPercent` can itself be confusing to API consumers.

The engine still generates named enzymes/catalysts, mechanisms, intermediates, and theoretical end products from formula-derived or shallow graph classification. It does not balance complete degradation reactions or prove pathway feasibility. Phrases such as engineered organisms, specific enzymes, Fenton reagent, complete inorganic endpoints, and “non-toxic inorganic phosphate salts” remain recommendations or outcomes not derived by the model.

Secondary documents still show 99.4%, “100% Mineralized Non-Toxic,” and harmless products. Those documents are current user-facing repository content, not historical audit files.

**Severity reduced from Critical to High. Score improves from 0.0 to 1.8 / 5.** The feature is now a hypothesis generator, but output language should say “unvalidated candidate” on every step and old documentation must be corrected.

### 4.6 Reaction pathways: better API, incomplete mathematical semantics

The public `Balance()` method now explains underdetermination and directs callers to `BalanceIndependentPathways()`. This is much better than the previous generic failure. The new method exposes each rational-nullspace basis vector as a reaction.

There are important limitations:

- taking `Math.Abs` of every basis coefficient discards sign, even though sign determines which side of a reaction a species belongs on;
- species are kept on their originally supplied sides rather than moved according to coefficient sign;
- a generic nullspace basis is not necessarily a set of chemically meaningful nonnegative elementary pathways;
- independent basis vectors are not the same as a minimal strictly positive balance for the combined equation;
- conversion from arbitrary-precision rational numerators to `int` remains unchecked;
- the new test only checks `pathways.Count >= 2`; it does not assert atom/charge conservation for every returned pathway.

For the carbon combustion example the chosen basis happens to yield useful pathways. That does not establish correctness for arbitrary underdetermined systems.

**Score improves from 3.5 to 3.8 / 5.** Add invariant tests that reconstruct the conservation matrix for every returned reaction and preserve coefficient signs correctly.

### 4.7 Force-field status reporting is fixed, scientific identity is not

`Converged` now means that a convergence criterion was actually met, and iteration reporting no longer claims one iteration when none ran. The project test was also corrected: it now requires non-increasing energy rather than falsely requiring convergence in 30 iterations.

The underlying engine remains a UFF-inspired potential with a small atom table, mostly element-level typing, generic fallback parameters, fixed angle constants, one generic torsion form, no full UFF typing, and a custom soft core. Central differences are consistent with the implemented energy but are not analytical gradients. Method metadata still claims applicability from H to Lw, which the parameter table cannot support.

**Score improves slightly from 2.5 to 2.7 / 5** for truthful execution semantics, not physical fidelity.

### 4.8 Shomate range handling and dependency security are genuinely fixed

The Shomate class now rejects temperatures outside its coefficient interval and uses one consistent temperature for H, S, and G. Tests cover both lower and upper rejection. This fully resolves the specific v1.1 inconsistency.

It remains a seven-gas, single-interval database and is not wired into the older general reaction thermodynamics path. The README now points to Shomate instead of advertising the Benson fallback, but elsewhere `ThermodynamicsEngine` still calls itself “100% Universal” and uses heuristic estimates for arbitrary unknown molecules.

The explicit `Microsoft.OpenApi 3.10.2` dependency resolves the formerly selected vulnerable transitive version. The suppression of `NU1903` was removed, and the current NuGet advisory query reports no vulnerable packages. This is a complete remediation of the known dependency finding.

## 5. Documentation credibility

### README improvements

The README now correctly says central finite-difference gradients rather than exact analytical gradients; removes the 100%-verified badge; softens industrial-grade, ADMET safety, physically valid conformer, true Benson, and quantitative EcoClean language; and describes the new graph and Shomate functionality more accurately.

Residual README issues include:

- “pure, mathematically rigorous” overgeneralizes heuristic subsystems;
- “43-fragment Ertl” still overstates the partial classifier;
- “Multi-Objective Lead Candidate Evolver” still optimizes incomplete QED/LogP and does not establish useful leads;
- “accurate 3D atomic coordinates” remains unsupported;
- “ISO/IUPAC-compliant” Molfile/SDF export remains unproven;
- the societal-breakthrough section still promises bypassing toxicity liabilities;
- the quality section still says 114/114 tests despite the badge saying 130.

### Secondary documentation remains substantially stale

The repository still presents these claims outside the README:

- `SCIENTIFIC_VERIFICATION_BENCHMARKS.md` calls all algorithms comprehensively validated and still publishes ibuprofen 4.00/34.1 and PFAS 99.4% as verified;
- `BREAKTHROUGHS_SHOWCASE.md` claims mutations eliminate hERG/CYP risks and PFAS reaches 100% non-toxic mineralization;
- `SCIENTIFIC_CREDIBILITY_REPORT.md` awards its own 9.5–10/10 scores and still claims exact analytical gradients;
- `GETTING_STARTED.md` and `API_REFERENCE.md` teach users to consume hERG, CYP, and BBB outputs as predictions;
- `SCIENTIFIC_APPROACH.md` claims every algorithm is validated by unit tests;
- `ARCHITECTURE.md` retains the old incorrect interpretation of complete amide group values as individual fragment contributions;
- core XML comments still say industrial-grade ADMET, complete 68-parameter LogP, complete spectra for any molecule, and 100% universal thermodynamics.

Historical `CODEX_AUDIT_v1.0.md` and `v1.1.md` are intentionally excluded from this criticism; they must remain unchanged as audit history.

**Documentation status: partially resolved, still High severity.** A credibility claim is repository-wide. Updating only the README leaves users, API consumers, and generated documentation exposed to contradictory claims.

## 6. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.1 | v1.2 | Principal current finding |
|---|---:|---:|---|
| Elements/molar mass | 4.0 | **4.0** | Useful standard-weight lookup, not isotope modelling |
| Formula parsing/topology | 3.0 | **3.0** | Composition works; invented topology boundary remains |
| SMILES parsing | 2.0 | **2.8** | Unsupported stereo now rejected; still a limited subset |
| Reaction balancing | 3.5 | **3.8** | Pathway API added; general sign/nonnegative semantics need work |
| Stoichiometry | 4.0 | **4.0** | Sound after a valid balance |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Strong narrow textbook implementations |
| Reaction-network integration | 3.0 | **3.0** | No material change |
| Explicit Hückel solver | 4.0 | **4.0** | Useful educational numerical solver |
| Automatic Hückel interpretation | 2.5 | **2.5** | Heuristic atom typing/observables remain |
| Shomate reference thermodynamics | 3.0 | **3.4** | Range fixed; only seven gas records and no general integration |
| Benson fallback | 1.0 | **1.0** | Still present and internally labeled universal |
| Molecular mechanics | 2.5 | **2.7** | Status fixed; incomplete UFF identity remains |
| 3D conformer generation | 2.0 | **2.0** | No conformer search or external geometry validation |
| TPSA | 1.5 | **2.3** | Amides fixed; full 43-type fidelity unestablished |
| LogP/MR | 1.5 | **1.5** | No material remediation |
| QED | 2.5 | **2.5** | No material remediation |
| ADMET/safety | 0.5 | **0.5** | Critical unsupported hERG/CYP/BBB output remains |
| Spectroscopy | 2.0 | **2.0** | Fixed correlation-table sketch, not complete spectra |
| Ring perception | 3.0 | **3.2** | Integrated into ADMET; complex validation still absent |
| Lead evolution | 1.0 | **1.2** | README softened; causal toxicity code/docs remain |
| EcoClean | 0.0 | **1.8** | Fake quantitative results removed; qualitative pathways overreach |
| File export/PubChem/API | 2.5–3.0 | **2.5–3.0** | Conformance, resilience, and integration evidence remain limited |

## 7. Validation quality

The three new tests directly protect useful fixes: stereo rejection, Shomate range rejection, and public independent-pathway access. Existing tests were corrected so they no longer require fake EcoClean efficiency or false force-field convergence. These are positive changes in test philosophy.

Scientific validation is still the central missing layer:

- no large independent TPSA/LogP/QED reference corpus;
- no exact per-type coverage report for published descriptor types;
- no molecular-mechanics comparison of typing, component energies, geometries, or gradients to a pinned UFF implementation;
- no bridged/spiro/polycyclic cycle-basis corpus;
- no spectroscopy error distribution against experimental spectra;
- no reaction-pathway invariant tests beyond pathway count;
- no file-format conformance or round-trip suite;
- no empirical ADMET or environmental dataset;
- no uncertainty, failure-rate, MAE/RMSE, bias, percentile, or applicability-coverage reporting.

Coverage is now 80.14% line and 70.06% branch. This is healthy regression coverage, not evidence that outputs are scientifically accurate.

## 8. Required next actions

### P0 — remove the remaining unsafe interpretation

1. Remove hERG, CYP, BBB, CNS-active, cardiac-safety, and clinical-sounding fields from the normal ADMET API.
2. Remove the corresponding rules, UI display, examples, API schemas, and documentation.
3. Ensure obsolete EcoClean quantitative properties are excluded from JSON serialization, then remove them in the next breaking release.
4. Mark every EcoClean step as an unvalidated mechanistic hypothesis; remove prescriptive catalyst/enzyme and harmless/non-toxic outcomes.
5. Correct all non-audit documentation, not only README.

### P1 — finish technical correctness boundaries

1. Separate empirical formula/composition from bonded molecular structure.
2. Complete and independently validate all Ertl and Wildman–Crippen atom types, or rename both as inspired approximations.
3. Make QED descriptors and alert definitions reference-equivalent, or rename the score.
4. Preserve nullspace coefficient signs, move species across reaction sides correctly, and test conservation for every returned pathway.
5. Remove full-UFF/H–Lw metadata until full atom typing and functional terms exist.
6. Rename fixed-table spectroscopy and lead evolution outputs so they communicate heuristic status.

### P2 — add actual scientific validation

1. Freeze reference datasets with provenance, licenses, hashes, and pinned reference-tool versions.
2. Publish per-method accuracy metrics, applicability coverage, unsupported-input rate, and error distributions.
3. Generate benchmark documents directly from executable artifacts tied to a commit.
4. Add adversarial chemical cases and explicit failure expectations.
5. Attach method metadata to validation artifact IDs rather than relying on self-authored evidence labels.

### P3 — strengthen CI and release evidence

1. Fail CI on high/critical dependency advisories.
2. Enforce compiler warnings as errors rather than naming the step as such only.
3. Add coverage floors, API integration tests, documentation-output checks, and file-format round trips.
4. Record SDK, OS, dependency lock, commit, seeds, commands, and dataset hashes in benchmark artifacts.

## 9. Acceptance gate for v1.3

The next audit can reasonably cross **6.0 / 10 overall** if:

- unsupported hERG/CYP/BBB outputs are removed from production-facing paths;
- all current non-audit documentation is synchronized with actual code behavior;
- formula-derived structures cannot enter topology-dependent engines;
- named descriptor/force-field methods are either completed or honestly renamed;
- at least one independent, reproducible validation corpus with quantitative metrics is committed.

## 10. Final verdict

Revision `315a495` demonstrates a healthier response to audit feedback than the preceding revision. It fixes real bugs, improves failure behavior, removes a fabricated environmental percentage, corrects dependency security, and makes the README substantially more honest. The credibility increase from **4.9 to 5.3 / 10** is deserved.

The commit does not resolve all P0/P1 findings. One critical drug-safety interface remains fully active, environmental pathways remain more authoritative than their evidence, and much of the documentation still preserves the old unsupported narrative. The project also continues to confuse implementing a recognizable equation or citing a paper with validating the complete published method.

**Overall claim-adjusted credibility: 5.3 / 10.**

Chemy is now a stronger educational and experimental toolkit. It is still not sufficiently evidenced for research-grade quantitative modelling or for drug-safety and environmental decisions without independent reference tools and domain-expert review.

# Chemy Scientific Credibility Audit — v1.3

**Audit date:** 2026-08-21  
**Audited revision:** `8d70639` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.2.md`](CODEX_AUDIT_v1.2.md), revision `315a495`  
**Earlier audits:** [`CODEX_AUDIT_v1.1.md`](CODEX_AUDIT_v1.1.md), [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md)  
**Auditor:** OpenAI Codex  
**Scope:** Delta review against v1.2, public claims, scientific implementation fidelity, API behavior, tests, coverage, dependency health, applicability controls, and reproducibility.

## 1. Executive conclusion

Revision `8d70639` makes the largest credibility improvement observed across the versioned audits. The critical unsupported hERG, CYP450, BBB, CNS-activity, and cardiac-safety outputs have been removed from `AdmetProfile` and from the calculation path. Empirical formulas no longer receive fabricated star-shaped bonds. Several incomplete scientific implementations are now honestly labeled “inspired,” “subset,” “empirical,” or “heuristic.” Reaction pathway coefficients preserve direction and are checked for conservation. EcoClean compatibility fields are excluded from JSON. CI now builds with warnings as errors. A small executable scientific-reference test set has been added.

These are substantive corrections to both behavior and scientific framing.

The commit message claims that all remaining P0, P1, and P2 findings are resolved. That claim is not supported by this audit. Important gaps remain:

- documentation and API metadata still contain removed hERG/CYP/BBB fields, non-toxic lead claims, 100% mineralization text, exact analytical-gradient text, and a self-awarded 9.5/10 credibility score;
- topology separation is enforced in ADMET and spectroscopy but is not a distinct type-system boundary and is not consistently enforced by every topology-dependent engine;
- the new “frozen reference dataset” is an embedded list of nine common molecules without source records, hashes, licenses, pinned RDKit/PubChem versions, or independently generated expected-value artifacts;
- LogP tolerance is as wide as 1.0 per molecule, while the implementation remains a coarse fragment subset;
- no QED, force-field, conformer, spectroscopy, file-format, Hückel interpretation, kinetics-network, or environmental predictive validation dataset was added;
- the older self-authored “scientific credibility report” still claims a 9.5/10 aggregate rating without an independent audit methodology.

### Credibility ratings

| Context | v1.1 | v1.2 | v1.3 | Interpretation at v1.3 |
|---|---:|---:|---:|---|
| Software implementation quality | 6.6 | 7.0 | **7.5 / 10** | Stronger API boundaries, warning discipline, conservation checks, and truthful metadata |
| Chemistry education/demonstrations | 7.2 | 7.5 | **7.9 / 10** | Useful broad toolkit when inspired/heuristic labels are retained at the point of use |
| Developer prototyping | 6.5 | 6.9 | **7.3 / 10** | Good experimental foundation with increasingly explicit limitations |
| Quantitative scientific analysis | 3.4 | 3.7 | **4.2 / 10** | A small reference suite is progress; broad method accuracy remains unestablished |
| Research/publication use | 2.3 | 2.6 | **3.1 / 10** | Still lacks representative external corpora, uncertainty, and pinned reproducible reference artifacts |
| Drug-safety decisions | 1.0 | 1.0 | **Not offered / 4.0 safety-of-scope** | Unsafe predictions removed; drug-likeness descriptors remain non-safety outputs |
| Environmental decisions | 1.0 | 2.5 | **2.7 / 10** | Quantitative fabrication remains removed; qualitative mechanisms are still speculative |
| **Overall claim-adjusted credibility** | **4.9** | **5.3** | **5.9 / 10** | Major risk reduction and better scientific honesty, but validation and repository-wide consistency are incomplete |

The rating is a structured engineering judgment, not a confidence interval. It weights claim fidelity, validation independence, applicability control, failure behavior, reproducibility, and the consequence of misinterpretation.

### Usage recommendation

Chemy is credible as an educational and experimental chemistry toolkit, and several narrow textbook calculators are credible within their documented assumptions. Descriptor, conformer, spectroscopy, force-field, thermodynamic-fallback, lead-evolution, and environmental results should still be independently checked before scientific or engineering use.

The project no longer offers drug-safety predictions through `AdmetProfile`; that is the correct and safest resolution. Drug-likeness and physicochemical filters must not be reinterpreted as evidence of safety, efficacy, exposure, or pharmacokinetics.

## 2. Reproduced engineering evidence

| Check | v1.2 | v1.3 result |
|---|---:|---:|
| Release build with warnings as errors | Not enforced in CI | **Passed, 0 warnings, 0 errors** |
| Automated tests | 130 passed | **136 passed, 0 failed, 0 skipped** |
| Core line coverage | 80.14% | **80.11%** (4,683 / 5,845) |
| Core branch coverage | 70.06% | **68.36%** (1,936 / 2,832) |
| High/critical vulnerable packages | None | **None reported by NuGet audit** |
| Executable scientific benchmark tests | None | **Present: 9 molecules plus 3 Shomate species** |
| Independently reproducible reference artifact | None | **Still absent** |
| CI coverage/advisory/benchmark artifact gates | Absent | **Still absent** |

The apparent branch-coverage decrease is not necessarily a regression in tested behavior because the set of valid branches also changed, but it shows that the new commit did not improve branch coverage despite adding tests.

### Independent public-API probes

```text
Ibuprofen AdmetProfile:
  MW 206.29; LogP 3.42; TPSA 37.30; QED 0.574
  no hERG, CYP450, or BBB fields
  MethodInfo explicitly says the suite does not assess biological safety

Formula-only C9H8O4:
  no generated bonds; HasBondedTopology = false
  ADMET and spectroscopy reject the input

F[C@](Cl)(Br)I and C@C:
  still rejected with NotSupportedException

Force field, maxIterations = 0:
  Iterations = 0; Converged = false

PFOA-like formula:
  legacy efficiency remains zero and excluded from JSON
```

## 3. v1.2 finding disposition

| v1.2 finding | v1.3 status | Evidence |
|---|---|---|
| Unsupported hERG/CYP/BBB result fields | **Resolved in core API** | Fields and calculation rules removed from `AdmetProfile` |
| Safety language in method metadata | **Resolved** | Explicit warning that drug safety/pharmacokinetics are not assessed |
| Safety language in all docs/API metadata | **Partially resolved** | Several stale claims and examples remain |
| Formula parser invents topology | **Resolved** | Formula parser returns zero bonds |
| Topology-dependent engines reject composition | **Partially resolved** | ADMET and spectroscopy reject; no universal type boundary across all consumers |
| Sign loss in reaction pathways | **Substantially resolved** | Sign-aware side placement plus element/charge conservation verification |
| Ertl/Wildman/UFF overclaiming in core metadata | **Resolved by honest scope** | Renamed as inspired/subset models with warnings |
| Full Ertl/Wildman/UFF fidelity | **Not implemented, now disclosed** | Scientific limitation remains but claim contradiction is reduced |
| QED overclaiming | **Improved by scope** | “Exact” removed and alert subset disclosed; underlying limitations remain |
| EcoClean legacy JSON exposure | **Resolved** | Compatibility properties have `JsonIgnore` |
| EcoClean speculative authority | **Partially resolved** | Heuristic label remains, but specific pathways/products still exceed evidence |
| Warnings-as-errors CI | **Resolved** | `--warnaserror` added and reproduced locally |
| Independent validation corpus | **Partially addressed, not resolved** | Small embedded benchmark with incomplete provenance |
| Repository-wide documentation consistency | **Unresolved — High** | Current user-facing claims contradict code and each other |

## 4. Detailed assessment

### 4.1 Removal of medical ADMET heuristics is a complete core-code fix

`AdmetProfile` now contains physicochemical descriptors and drug-likeness filters only. The hERG, CYP450, and BBB fields and rule branches are gone. The replacement `ScientificMethodInfo` clearly says the engine does not assess in vitro/in vivo safety, pharmacokinetics, hERG cardiotoxicity, or clinical outcomes.

This is stronger than deprecating or relabeling the old predictions: it prevents normal callers from receiving unsupported safety conclusions. It resolves the highest-risk defect present since audit v1.0.

There are still descriptor correctness limitations. HBA logic excludes amide nitrogen but not the full range of non-accepting nitrogens/oxygens, and rotatable-bond logic still does not exclude amide C–N and every restricted-bond environment. Those affect drug-likeness fidelity but are materially less dangerous than fabricated medical classifications.

**Disposition:** P0 core-code issue resolved. ADMET/descriptor subsystem improves from **0.5 to 2.8 / 5** because the remaining output is appropriately scoped, not because it became a validated ADMET model.

### 4.2 Formula/topology separation is materially improved but structurally incomplete

`FormulaParser` no longer calls `AutoGenerateBonds`; composition-only inputs therefore cannot silently masquerade as the arbitrary star graph found in v1.0–v1.2. `Molecule.HasBondedTopology` makes the distinction observable, and ADMET/spectroscopy reject an unbonded multi-atom input.

This is an effective behavioral fix for those consumers. It is not a complete type separation:

- a composition and a bonded structure are still represented by the same `Molecule` type;
- `HasBondedTopology` treats every one-atom object as topological, which is practical but semantic rather than structural typing;
- individual public descriptor entry points such as `ErtlTpsa.Calculate`, `WildmanCrippenLogP.Calculate`, and `BickertonQed.Calculate` do not enforce the guard themselves;
- geometry generation still has formula/VSEPR paths and can emit center-to-all PDB `CONECT` records for unbonded formulas;
- force field, evolution, environmental, graph, and other topology consumers do not share one enforced precondition;
- code can manually construct a multi-atom, zero-bond `Molecule` without a distinct composition type.

The original invented-topology defect is resolved at the parser. The broader P1 recommendation—make invalid topology use unrepresentable or consistently rejected—is only partially complete.

**Disposition:** formula/topology score improves from **3.0 to 3.7 / 5**.

### 4.3 Reaction pathway direction and conservation are much safer

`BalanceIndependentPathways()` now interprets coefficient signs, moves species between sides, and verifies element and charge conservation before returning a reaction. This corrects the v1.2 use of `Math.Abs` while retaining original sides.

Remaining mathematical limitations are narrower:

- the orientation heuristic based on a summed `reactantNet` is arbitrary when totals cancel;
- basis vectors are algebraically independent but are not guaranteed to be chemically elementary pathways;
- a nullspace basis is not equivalent to finding all or a minimal nonnegative solution cone;
- silently omitting a non-conserved candidate can return fewer pathways without explaining why;
- rational-to-`int` conversion remains bounded and unchecked for very large coefficients;
- the new benchmark does not add complex redox/ionic or mixed-sign pathway cases.

**Disposition:** reaction balancing improves from **3.8 to 4.1 / 5**.

### 4.4 Scientific naming is substantially more honest

The following metadata changes are appropriate:

- “Ertl-Inspired … Fragment Subset” replaces “exhaustive 43-fragment”;
- “Crippen-Inspired … Core Fragment Subset” replaces “complete 68-parameter”;
- QED now discloses its heuristic structural-alert subset;
- force field is called “UFF-Inspired” and its actual terms are listed;
- spectroscopy is called empirical estimation rather than complete prediction;
- the older thermodynamics XML comment no longer says “100% Universal.”

This does not improve numerical accuracy by itself, but it materially improves credibility because users can interpret output within the implementation's actual scope. One remaining concern is that unsupported descriptor atom environments contribute zero or a neutral default rather than returning an applicability failure. A warning in shared method metadata does not identify whether a particular molecule encountered such an environment.

### 4.5 The new benchmark suite is useful regression evidence, not yet independent validation

The new test class embeds nine common molecules with expected formula, molecular weight, TPSA, LogP, HBD, HBA, rotatable-bond, and aromatic-ring values. It executes strict TPSA checks, a broad LogP check, simple cycle counts, three Shomate comparisons, and topology-boundary checks.

Positive aspects:

- it puts reference-like numbers in executable form;
- it calculates TPSA and LogP MAE rather than checking only that values are finite;
- Shomate H/S tolerances are explicit;
- the dataset exercises several common functional groups;
- failures will expose future drift.

It does not satisfy the v1.2 acceptance criterion for an independent reproducible validation corpus:

- the values are embedded in source rather than stored as a versioned data artifact;
- no per-value source citation or retrieval record exists;
- “RDKit / PubChem / NIST” is mentioned, but versions, commands, settings, dates, canonical structures, and source URLs/identifiers are absent;
- no dataset hash, license, generator, or immutable upstream artifact is supplied;
- several expected LogP values appear chosen to match current Chemy output, and the test permits absolute error up to 1.0;
- nine small common molecules are not representative of the claimed chemical space;
- the test declares fields for HBD/HBA/rotatable bonds but does not actually assert them;
- it does not validate QED despite the QED field remaining prominent;
- a self-authored list cannot demonstrate independence without provenance.

The suite should be called a **frozen smoke/reference regression set**, not external scientific validation.

### 4.6 Environmental output is safer but remains a hypothesis generator

Legacy efficiency, half-life, and alias properties are now excluded from JSON. Documentation was partly updated to say qualitative cascade and candidate system. These changes reduce the likelihood that an API user consumes fabricated quantitative outcomes.

The engine still selects named enzymes, organisms, catalysts, mechanisms, and theoretical mineralization products from element presence and simple graph rules. It does not establish feasibility, kinetics, selectivity, intermediates, mass balance, reactor conditions, or toxicity. Specific phrases such as “non-toxic inorganic phosphate salts” remain in core output, and old showcase text still shows 100% non-toxic mineralization.

**Disposition:** safer API framing, still **High** if presented as remediation guidance. Score **2.0 / 5**.

### 4.7 CI and dependency hygiene improved

The release solution builds successfully with `--warnaserror`, zero warnings, and zero errors. The NuGet advisory query reports no vulnerable packages. Both results are reproducible and materially improve software credibility.

CI still does not:

- run dependency audit and fail on severity;
- collect/enforce coverage thresholds;
- publish benchmark or coverage artifacts;
- validate documentation examples against runtime;
- test multiple platforms or supported SDK ranges;
- run file-format conformance or API integration suites.

## 5. Documentation consistency audit

Several documents were improved, especially the generated ADMET example and qualitative EcoClean framing. However, current non-audit content still contains contradictions:

- `Chemy.Api/Program.cs` describes the ADMET endpoint as calculating hERG cardiac safety and CYP450 metabolism even though those fields were removed;
- `API_REFERENCE.md` still shows `cyp450MetabolismSite` and `bloodBrainBarrierPermeability` in response JSON;
- `BREAKTHROUGHS_SHOWCASE.md` still claims fluorination blocks CYP metabolism, eliminates hERG risk, and ends in “100% Mineralized Non-Toxic” products;
- `SCIENTIFIC_APPROACH.md` still claims CYP3A4 blocking and says every algorithm is validated by comprehensive unit tests;
- `SCIENTIFIC_CREDIBILITY_REPORT.md` still claims a weighted **9.5/10**, exact analytical gradients, and other self-verified ratings far above the evidence;
- `SCIENTIFIC_VERIFICATION_BENCHMARKS.md` still opens by calling every calculation comprehensively validated and labels heuristic EcoClean output verified;
- `README.md` still says 43-fragment Ertl and published Wildman–Crippen despite the core now correctly calling both subsets;
- README still calls formula/SMILES coordinates accurate, export ISO/IUPAC-compliant, lead evolution toxicity-bypassing, and reports the stale 114-test count;
- `ARCHITECTURE.md` retains the old incorrect presentation of whole amide totals as individual atom contributions;
- `MolfileExporter` still claims standards compliance without conformance evidence.

Historical `CODEX_AUDIT_v1.0.md` through `v1.2.md` are intentionally excluded; preserving their original statements is correct audit versioning.

**Disposition:** documentation remediation is incomplete and remains a **High-severity credibility issue**, although the corresponding unsafe ADMET fields are no longer present in code.

## 6. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.2 | v1.3 | Principal current finding |
|---|---:|---:|---|
| Elements/molar mass | 4.0 | **4.0** | Useful standard-weight lookup, not isotope modelling |
| Formula parsing/topology | 3.0 | **3.7** | Invented bonds removed; one shared type still permits misuse |
| SMILES parsing | 2.8 | **2.8** | Safe rejection retained; supported syntax remains limited |
| Reaction balancing | 3.8 | **4.1** | Sign and conservation fixed; pathway semantics remain limited |
| Stoichiometry | 4.0 | **4.0** | Sound after a valid balance |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Strong narrow textbook implementations |
| Reaction-network integration | 3.0 | **3.0** | No new validation or boundary work |
| Explicit Hückel solver | 4.0 | **4.0** | Useful educational numerical solver |
| Automatic Hückel interpretation | 2.5 | **2.5** | Heuristic atom typing/observable interpretation remains |
| Shomate thermodynamics | 3.4 | **3.6** | Three species now benchmarked; database still narrow |
| Empirical thermodynamic fallback | 1.0 | **1.5** | Universal claim removed; scientific model remains weak |
| Molecular mechanics | 2.7 | **3.0** | Honest UFF-inspired scope; no new physical validation |
| 3D conformer generation | 2.0 | **2.1** | Better framing in code; README still says accurate |
| TPSA subset | 2.3 | **2.8** | Honest name plus nine examples; chemical coverage remains narrow |
| LogP/MR subset | 1.5 | **2.3** | Honest name and smoke dataset; wide tolerance and missing types remain |
| QED-inspired score | 2.5 | **2.7** | Honest alert warning; no direct reference validation |
| Physicochemical/drug-likeness profile | 0.5 | **2.8** | Medical predictions removed; descriptor accuracy still partial |
| Spectroscopy | 2.0 | **2.3** | Honest empirical label and topology guard; no accuracy corpus |
| Ring perception | 3.2 | **3.4** | Nine simple ring-count examples; complex graphs still absent |
| Lead evolution | 1.2 | **1.4** | Core causal/non-toxic claims remain despite broader cleanup |
| EcoClean | 1.8 | **2.0** | JSON safer; qualitative pathway authority remains too strong |
| File export/PubChem/API | 2.5–3.0 | **2.5–3.0** | No conformance/resilience/integration validation added |

## 7. Remaining priorities

### P0 — finish repository-wide removal of unsafe claims

1. Remove stale hERG/CYP/BBB response fields and endpoint descriptions from API metadata and docs.
2. Remove claims that lead mutations block metabolism, eliminate toxicity/hERG risk, or extend plasma half-life.
3. Remove “non-toxic,” “100% mineralized,” and prescriptive EcoClean outcome language.
4. Replace the self-awarded 9.5/10 credibility report with a scoped capability/limitations document or clearly label it historical and superseded.

### P1 — make applicability enforcement systematic

1. Introduce a composition-only type distinct from a bonded molecular graph, or a single validated topology precondition used by every topology-dependent engine.
2. Reject unsupported descriptor atom environments per molecule instead of silently contributing zero/default values.
3. Complete rotatable-bond and HBA definitions used by QED/drug-likeness filters.
4. Remove or scope remaining formula-to-geometry connectivity invention.
5. Add reaction-pathway tests that cover mixed signs, charges, empty/filtered bases, and large coefficients.

### P2 — turn the benchmark into independent evidence

1. Store reference records in a data file with molecule identifiers, canonical structures, units, sources, licenses, and hashes.
2. Pin RDKit/CDK/Open Babel/NIST versions and commit the exact generator/query commands.
3. Separate reference values from Chemy output and publish diffs, failures, MAE/RMSE, bias, percentiles, and applicability coverage.
4. Expand beyond nine easy molecules to every supported atom environment and chemically difficult cases.
5. Add independent suites for QED, force field, conformers, spectroscopy, cycle bases, file formats, and numerical solvers.

### P3 — automate credibility controls

1. Run vulnerability audit, coverage thresholds, benchmarks, documentation-output checks, and API integration tests in CI.
2. Publish revision-linked coverage and benchmark artifacts.
3. Make documentation examples executable so removed fields and stale counts fail CI.
4. Add conformance round trips using independent file-format readers.

## 8. Acceptance gate for v1.4

The next audit can clearly exceed **6.5 / 10 overall** if:

- all live documentation and API metadata match the new safe core API;
- no topology-dependent public entry point accepts a composition-only structure;
- the benchmark has traceable, pinned, independently generated source data;
- reference coverage expands to the claimed applicability domains;
- CI publishes and gates quantitative benchmark/coverage/advisory results;
- at least one major predictive subsystem reports a defensible external error distribution.

## 9. Final verdict

Revision `8d70639` deserves a substantial credibility increase from **5.3 to 5.9 / 10**. Removing unsupported medical predictions and invented formula topology directly addresses the two most important code-level credibility risks. Honest “inspired/subset” naming is also a major improvement because it aligns scientific interpretation with implementation scope.

The solution has not resolved every P0/P1/P2 item. The new benchmark is a promising regression seed, not independent scientific validation, and repository-wide documentation still contradicts the safer code. Most quantitative headline methods remain educational approximations without representative external accuracy evidence.

**Overall claim-adjusted credibility: 5.9 / 10.**

Chemy is now a credible broad educational and developer-prototyping toolkit with several sound narrow calculations. Research-grade quantitative use still requires independent reference tools, applicability checks, reproducible external validation, and domain-expert review.

# Chemy Scientific Credibility Audit — v1.4

**Audit date:** 2026-08-21  
**Audited revision:** `7fe674c` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.3.md`](CODEX_AUDIT_v1.3.md), revision `8d70639`  
**Earlier audits:** [`CODEX_AUDIT_v1.2.md`](CODEX_AUDIT_v1.2.md), [`CODEX_AUDIT_v1.1.md`](CODEX_AUDIT_v1.1.md), [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md)  
**Auditor:** OpenAI Codex  
**Scope:** Delta review against v1.3, implementation/claim fidelity, topology boundaries, reference-data provenance, tests, coverage, CI controls, dependency health, and public documentation.

## 1. Executive conclusion

Revision `7fe674c` continues the positive remediation trajectory. The developers corrected most stale medical and environmental claims, replaced the self-awarded credibility scorecard with a capability/scope document, extended topology checks into descriptor and graph entry points, improved HBA and rotatable-bond rules, strengthened reaction parsing and pathway tests, expanded the executable reference suite, and added coverage collection plus dependency inspection to CI.

Chemy is now substantially more credible in how it describes itself. The repository consistently presents many predictive components as empirical, inspired, subset, heuristic, or exploratory rather than complete validated implementations.

The largest remaining credibility issue is the new file described as a “pinned external benchmark dataset.” It is pinned as repository content, but it is not reproducibly external:

- provenance is a free-text string such as `RDKit 2024.03.1 / PubChem CID 176`;
- there is no generator, source response, command, environment lock, dataset hash manifest, license record, or per-property source mapping;
- several values closely encode Chemy's current simplified behavior and conflict with standard chemical definitions—for example carboxylic-acid hydroxyl oxygen is counted as an H-bond acceptor, giving acetic acid and ibuprofen two acceptors rather than one;
- the field named `exactMolecularWeight` contains standard/average molecular weights such as aspirin 180.158, not monoisotopic exact mass;
- the tests compare those values back to Chemy and therefore can certify self-consistency while being mislabeled as RDKit/PubChem validation.

This does not invalidate the suite as regression testing. It prevents the suite from being accepted as independent scientific validation.

### Credibility ratings

| Context | v1.2 | v1.3 | v1.4 | Interpretation at v1.4 |
|---|---:|---:|---:|---|
| Software implementation quality | 7.0 | 7.5 | **7.8 / 10** | Better guards, CI, parsing, invariants, and documentation discipline |
| Chemistry education/demonstrations | 7.5 | 7.9 | **8.1 / 10** | Broad and useful when empirical/heuristic limitations stay visible |
| Developer prototyping | 6.9 | 7.3 | **7.6 / 10** | Strong experimental .NET foundation with clearer contracts |
| Quantitative scientific analysis | 3.7 | 4.2 | **4.5 / 10** | More tests and scope honesty; claimed reference provenance is not reproducible |
| Research/publication use | 2.6 | 3.1 | **3.4 / 10** | Still lacks trustworthy external datasets and broad error distributions |
| Safety-of-scope | 2.5 | 4.0 | **4.3 / 5** | Medical predictions remain removed; most associated docs are now cleaned |
| Environmental decisions | 2.5 | 2.7 | **2.9 / 10** | Qualitative framing improved; pathways remain unvalidated hypotheses |
| **Overall claim-adjusted credibility** | **5.3** | **5.9** | **6.2 / 10** | Credible educational/prototyping software; scientific validation remains limited |

The rating is a structured engineering judgment, not a statistical confidence interval. It considers claim fidelity, validation independence, applicability controls, failure behavior, reproducibility, and consequences of misuse.

### Usage recommendation

Chemy is credible for education, algorithm demonstrations, developer prototyping, and narrow textbook calculations within explicit assumptions. Descriptor, QED, force-field, conformer, spectroscopy, thermodynamic-fallback, lead-exploration, and environmental outputs still require independent verification for scientific work.

It does not expose hERG/CYP/BBB predictions and should continue not to. Physicochemical and drug-likeness descriptors are not evidence of safety, efficacy, pharmacokinetics, or clinical suitability.

## 2. Reproduced engineering evidence

| Check | v1.3 | v1.4 result |
|---|---:|---:|
| Release build with `--warnaserror` | Passed | **Passed, 0 warnings, 0 errors** |
| Automated tests | 136 passed | **143 passed, 0 failed, 0 skipped** |
| Core line coverage | 80.11% | **80.32%** (4,732 / 5,891) |
| Core branch coverage | 68.36% | **69.37%** (2,012 / 2,900) |
| High/critical vulnerable packages | None | **None reported by current NuGet audit** |
| External-looking descriptor records | 9 embedded | **16 JSON records** |
| Reproducible independent generator/artifact | Absent | **Still absent** |
| CI coverage collection | Absent | **Present, without threshold or published artifact** |
| CI advisory query | Absent | **Present, but not configured as a severity gate** |

`dotnet list package --vulnerable` generally reports advisories but its presence alone is not evidence that CI fails when one exists. A deliberate severity assertion or audit tool with failure semantics is required. Likewise, collecting coverage without enforcing or publishing it is observation, not a quality gate.

## 3. v1.3 finding disposition

| v1.3 finding | v1.4 status | Evidence |
|---|---|---|
| Stale hERG/CYP/BBB API metadata | **Resolved** | Endpoint description and API example updated |
| Unsafe lead-evolution causal claims | **Substantially resolved** | Core rationales and showcase changed to property exploration |
| Self-awarded 9.5/10 credibility report | **Resolved** | Replaced with method classification/scope matrix and audit link |
| Old exact-gradient documentation | **Resolved in reviewed docs** | Central finite-difference equation now shown |
| Old 100% non-toxic mineralization text | **Substantially resolved** | Core and showcase cleaned |
| Formula topology accepted by descriptor APIs | **Resolved** | TPSA, LogP, QED, ADMET, spectroscopy, and graph construction guard it |
| Formula topology accepted by every topology path | **Partially resolved** | Geometry still treats some unbonded formulas as VSEPR coordinate models |
| HBA/rotatable-bond definitions | **Improved, not complete** | Amide C–N excluded; acid oxygen and several charged/aromatic cases remain wrong |
| Reaction sign/conservation robustness | **Improved** | Added conserved pathway and charged redox tests |
| Reference dataset size | **Improved** | Expanded from 9 to 16 compounds |
| Independent benchmark provenance | **Unresolved** | Labels added, but no reproducible external evidence and suspect values remain |
| CI coverage/vulnerability controls | **Partially resolved** | Commands added without thresholds, artifacts, or reliable failure policy |
| File-format conformance | **Unresolved** | Claims softened, but no independent round-trip/conformance suite |
| Broad predictive validation | **Unresolved** | No external force-field, conformer, spectroscopy, EcoClean, or network corpus |

## 4. Detailed analysis

### 4.1 Repository-wide scientific framing is much better

The README now uses “Crippen-inspired,” “Ertl-inspired,” “exploration,” and “estimator.” It no longer calls the export layer ISO/IUPAC compliant or the generated coordinates accurate. The API no longer advertises removed medical endpoints. The showcase removes toxicity, hERG, CYP, bioavailability, plasma-half-life, and 100% mineralization claims. The former internal credibility report no longer assigns itself 8–10/10 scores.

These changes materially reduce user-facing overinterpretation. They are not cosmetic: honest scope is part of scientific correctness because it determines what a result is allowed to mean.

Residual overstatement includes:

- the README “Scientific Rigor — Peer-Reviewed Algorithms” badge can still imply implementations were validated merely because inspirations were published;
- the general “computational chemistry platform” framing is broader than the validated scope;
- `SCIENTIFIC_VERIFICATION_BENCHMARKS.md` continues to call all calculations comprehensively verified;
- several exact/analytical adjectives elsewhere should be restricted to arithmetic or equations, not numerical solutions or physical accuracy;
- EcoClean still turns shallow classification into specific biochemical mechanisms and endpoints.

**Disposition:** documentation severity falls from High to Medium.

### 4.2 Topology enforcement is broader, but geometry remains an exception

TPSA, LogP/MR, QED, ADMET, spectroscopy, and `ChemicalGraph.FromMolecule` now explicitly reject a multi-atom `Molecule` without bonds. The force field rejects an unbonded `SourceMolecule`. This is a strong improvement and makes direct public entry points safer.

`Geometry3DEngine.Generate3D` does not universally reject composition-only input:

- monatomic and diatomic formulas receive coordinates;
- single-heavy-center formulas such as `H2O` still receive VSEPR-style coordinates;
- unbonded multi-heavy-atom formulas fail indirectly only when `GenerateMultiCenter3D` reaches `ChemicalGraph.FromMolecule`;
- the benchmark's claim that all topology-dependent engines reject formula input tests `C9H8O4`, not the allowed `H2O` path;
- the geometry test explicitly confirms that formula-only water still gets a coordinate model, merely without PDB `CONECT` records.

There is a reasonable product choice to allow formula-driven educational VSEPR coordinates, but it must be a separately named and typed composition heuristic. The README now says the builder converts bonded molecular structures, which conflicts with the public formula-water behavior.

**Disposition:** topology boundary improves to **4.0 / 5**, but is not universal.

### 4.3 HBA and rotatable bonds improved, but the benchmark freezes remaining errors

Amide C–N bonds are now excluded from rotatable-bond counts. Amide nitrogen and positively charged nitrogen are excluded from HBA. These are real descriptor improvements.

Oxygen acceptor logic is still `NetCharge <= 0`, so neutral carboxylic-acid hydroxyl oxygen is counted as an acceptor. The new reference file records:

- ibuprofen: HBA = 2;
- acetic acid: HBA = 2;
- benzoic acid: HBA = 2;
- aspirin: HBA = 4.

Those counts reflect the implementation's “every neutral O accepts” rule, not the standard Lipinski-style exclusion of the acidic hydroxyl oxygen. The test then asserts exact equality and calls the values RDKit references. This is an example of a regression suite entrenching an implementation error under external provenance language.

Other missing HBA rules include full pyrrolic/aromatic nitrogen handling, protonated environments, zwitterions, sulfonamides, and additional resonance cases. The reference set is too small to establish the stated applicability domain.

**Disposition:** descriptor definitions improve, but the reference claim creates a new Medium-to-High credibility concern.

### 4.4 The “pinned external dataset” is not independently reproducible

Moving values into JSON, adding molecule identifiers, naming RDKit `2024.03.1`, and expanding to 16 compounds are all useful engineering steps. The test now asserts HBD/HBA/rotatable bonds, QED, more Shomate temperatures, Hückel analytical examples, and topology guards.

However, “pinned” currently means only committed to Git. To establish external provenance, the repository needs the complete derivation chain. It does not provide:

- a script that runs the pinned RDKit version;
- a lock file/container digest for RDKit and its dependencies;
- exact descriptor calls and parameter settings;
- archived PubChem responses or property names;
- per-property source attribution rather than one mixed provenance string;
- source date, URL, license, and checksum;
- a generated-file header and immutable input hash;
- a diff report separating external expected values from Chemy values.

There are internal reasons to doubt the asserted provenance:

- `exactMolecularWeight` values such as aspirin `180.158` and ethanol `46.069` are standard/average molecular weights, not exact monoisotopic masses;
- acid HBA values reproduce Chemy's simplified logic;
- the data sometimes says `Ertl 2000 / ChEMBL` while carrying LogP, QED, and other fields that the Ertl TPSA paper does not source;
- the test tolerates individual LogP errors up to 1.0, which is broad for claiming method agreement;
- no generated observed-versus-reference table is published, so the actual error distribution is hidden behind pass/fail thresholds.

The file is valuable as a **versioned regression fixture**. It is not yet credible as a pinned external benchmark dataset.

### 4.5 QED and LogP validation remains weak

QED now has an MAE threshold of 0.08 and per-compound difference limit of 0.15. LogP has MAE below 0.35 and individual difference up to 1.0. These are better than no metrics.

The reference QED values cannot establish independent accuracy without a reproducible generator. QED also consumes Chemy's subset TPSA/LogP, simplified HBA/rotatable rules, approximate aromatic rings, and reduced structural alerts. A dataset of 16 common molecules cannot validate the full alert system or drug-like chemical space.

The correct claim remains: QED-inspired and Crippen-inspired approximations with observed behavior over a small regression set.

### 4.6 Shomate and Hückel additions are more credible than the descriptor fixture

The Shomate test now checks H and S at multiple temperatures for H2O, CO2, and CH4. The values and tolerances are explicit. This is useful equation/reference verification, though still only three of seven stored gases and no interval-transition cases.

Hückel ethylene, butadiene, and benzene tests compare against analytical textbook values. These are legitimate tests of the numerical eigensolver and simple automatic topology mapping. They do not validate UV-Vis wavelengths, “Fukui” interpretations, heteroatom parameters, or broad molecular electronic-structure claims.

### 4.7 Reaction parsing and pathways improve, but exactness still has limits

Reaction term splitting now avoids interpreting charge `+` inside bracketed/braced formula syntax as a component separator, and the charged zinc/copper example tests mass and charge conservation. Independent pathway tests verify every returned reaction is balanced.

Limitations remain:

- plus signs are separators only when adjacent whitespace exists, so compact equations such as `H2+O2->H2O` may not parse as users expect;
- coefficient conversion still returns `int`, limiting the otherwise arbitrary-precision rational solver;
- a linear nullspace basis is not a chemical elementary-pathway decomposition or nonnegative solution cone;
- orientation remains heuristic for ambiguous bases.

**Disposition:** reaction balancing reaches **4.2 / 5** for scoped equation balancing.

### 4.8 CI additions observe but do not yet enforce scientific quality

CI now runs coverage collection and a dependency advisory query. This is better visibility. It does not upload coverage, set line/branch floors, compare against a baseline, publish benchmark metrics, or fail on statistical regression.

The dependency command is not configured with a policy that explicitly fails on high/critical vulnerabilities. The v1.0 advisory could therefore be printed in logs without necessarily failing the workflow. A CI control must have deterministic failure semantics, not only execute an informational command.

## 5. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.3 | v1.4 | Principal current finding |
|---|---:|---:|---|
| Elements/molar mass | 4.0 | **4.0** | Standard weights useful; new fixture misnames them exact mass |
| Formula parsing/topology | 3.7 | **4.0** | Broad guards added; formula-driven VSEPR remains special case |
| SMILES parsing | 2.8 | **2.8** | Safe limited subset, no new standards coverage |
| Reaction balancing | 4.1 | **4.2** | Charge parsing/tests improved; basis semantics and `int` limit remain |
| Stoichiometry | 4.0 | **4.0** | Sound after a valid balance |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Strong narrow textbook implementations |
| Reaction-network integration | 3.0 | **3.0** | No new validation corpus |
| Explicit Hückel solver | 4.0 | **4.2** | Three analytical systems now in consolidated benchmark |
| Automatic Hückel interpretation | 2.5 | **2.6** | Simple mappings tested; broad interpretations remain heuristic |
| Shomate thermodynamics | 3.6 | **3.8** | Multi-temperature checks for three gases; database/scope narrow |
| Empirical thermodynamic fallback | 1.5 | **1.5** | No scientific-model improvement |
| Molecular mechanics | 3.0 | **3.0** | Honest UFF-inspired scope; no independent physical benchmark |
| 3D conformer generation | 2.1 | **2.3** | PDB no longer invents bonds; formula-VSEPR boundary remains ambiguous |
| TPSA subset | 2.8 | **2.9** | Larger regression set; external provenance unverified |
| LogP/MR subset | 2.3 | **2.4** | MAE threshold added; reference independence and type coverage weak |
| QED-inspired score | 2.7 | **2.8** | Metrics added; inputs/alerts and reference provenance remain partial |
| Physicochemical profile | 2.8 | **3.0** | Better HBA/ROTB rules; acid HBA error frozen into fixture |
| Spectroscopy | 2.3 | **2.3** | Honest scope, no external accuracy evidence |
| Ring perception | 3.4 | **3.5** | Expanded simple examples; hard polycycles still absent |
| Lead exploration | 1.4 | **2.2** | Unsafe causal language removed; mutation/scoring remains scripted |
| EcoClean | 2.0 | **2.2** | Documentation safer; pathways remain speculative |
| File export/PubChem/API | 2.5–3.0 | **2.7–3.1** | Claims softened/API aligned; conformance and integration tests absent |

## 6. Priority remediation

### P0 — correct the benchmark's scientific identity

1. Rename the current JSON as a Chemy regression fixture until independently regenerated.
2. Correct acid HBA values and all other descriptor definitions against a pinned reference implementation.
3. Rename `exactMolecularWeight` to `standardMolecularWeight`, or supply true monoisotopic exact masses separately.
4. Remove unsupported external provenance labels unless each property can be reproduced from the named source.
5. Do not present passing self-consistency tests as scientific validation.

### P1 — make reference generation reproducible

1. Commit a generator using an exact RDKit container/package digest and explicit descriptor calls.
2. Archive or hash upstream PubChem/NIST inputs, including identifiers, property names, dates, units, and licenses.
3. Generate JSON mechanically; include schema version, generator version, input hashes, and output hash.
4. Publish observed-versus-reference rows and MAE/RMSE/bias/percentiles, including failures and unsupported molecules.
5. Expand cases by atom environment and applicability domain, not only by compound count.

### P2 — finish systematic boundaries and validation

1. Separate formula-driven VSEPR sketches from bonded conformer generation in API names and types.
2. Reject per-molecule unsupported descriptor atom types rather than silently using zero/default contributions.
3. Add independent force-field energy/geometry, conformer, spectroscopy, file-format, and cycle-basis corpora.
4. Add charged, tautomeric, zwitterionic, stereochemical, fused/bridged, and uncommon heteroatom cases.
5. Generate all benchmark documentation from tests so prose and runtime cannot diverge.

### P3 — turn CI observation into enforcement

1. Add explicit failure on high/critical dependency advisories.
2. Enforce line and branch coverage floors and publish Cobertura artifacts.
3. Publish benchmark metrics and fail on statistically meaningful regression.
4. Add API integration, JSON schema, and independent file-format round-trip checks.
5. Pin actions by immutable commit SHA for stronger supply-chain reproducibility.

## 7. Acceptance gate for v1.5

The next audit can clearly exceed **6.5 / 10 overall** when:

- the reference data is mechanically generated and independently reproducible;
- known acid HBA and exact-mass labeling errors are corrected;
- benchmark metrics expose the complete observed error distribution;
- CI has real advisory, coverage, and benchmark failure thresholds;
- at least one major predictive subsystem has representative external validation;
- formula-driven geometry is separated explicitly from bonded molecular modelling.

## 8. Final verdict

Revision `7fe674c` improves credibility from **5.9 to 6.2 / 10**. The strongest gains come from repository-wide claim cleanup, broader topology guards, improved reaction/descriptor rules, and more disciplined CI/testing. Chemy now communicates its educational and empirical nature much more responsibly.

The “pinned external benchmark dataset” is the principal blocker to a higher rating. It currently has the form of external evidence without the reproducible derivation needed to establish it, and it appears to freeze several implementation-specific errors as reference truth. Correcting that is more important than merely adding additional compounds.

**Overall claim-adjusted credibility: 6.2 / 10.**

Chemy is credible as educational and developer-prototyping software with several sound narrow calculations. Quantitative research use still requires independent tools, transparent applicability checks, reproducible external reference generation, and domain-expert review.

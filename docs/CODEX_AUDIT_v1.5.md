# Chemy Scientific Credibility Audit — v1.5

**Audit date:** 2026-08-21  
**Audited revision:** `ddb13b9` (`main`)  
**Previous audit:** [`CODEX_AUDIT_v1.4.md`](CODEX_AUDIT_v1.4.md), revision `7fe674c`  
**Earlier audits:** [`CODEX_AUDIT_v1.3.md`](CODEX_AUDIT_v1.3.md), [`CODEX_AUDIT_v1.2.md`](CODEX_AUDIT_v1.2.md), [`CODEX_AUDIT_v1.1.md`](CODEX_AUDIT_v1.1.md), [`CODEX_AUDIT_v1.0.md`](CODEX_AUDIT_v1.0.md)  
**Auditor:** OpenAI Codex  
**Scope:** Delta review against v1.4, reference-data generation, descriptor correctness, geometry boundaries, CI enforcement, documentation, tests, coverage, and dependency health.

## 1. Executive conclusion

Revision `ddb13b9` fixes several issues identified in audit v1.4:

- carboxylic-acid hydroxyl oxygen is no longer counted as an H-bond acceptor;
- standard molecular weight and monoisotopic exact mass are represented separately;
- formula-derived VSEPR sketches and bonded conformers now have distinct public APIs and result metadata;
- benchmark tests publish observed/reference rows and MAE/RMSE/maximum-error metrics;
- CI uploads coverage, explicitly fails when vulnerable packages are reported, and checks deterministic dataset reproduction;
- the misleading “peer-reviewed algorithms” badge was removed;
- additional force-field, polycyclic-ring, reaction, and file-serialization smoke tests were added.

The principal v1.4 blocker is **not resolved**. `scripts/generate_reference_dataset.py` is described as externally generating values with RDKit, NIST, CIAAW, ChEMBL, and PubChem, but it does not import, call, query, or parse any of those sources. Every property is copied from a hard-coded `data_lookup` dictionary. CI proves only that this hard-coded dictionary reproduces the committed JSON.

This distinction is fundamental:

- deterministic regeneration is not independent generation;
- adding source names to hard-coded numbers is not provenance;
- a script that never invokes RDKit cannot verify RDKit results;
- comparing Chemy to manually transcribed values can be useful, but it must be described as a curated regression fixture with traceable citations—not a machine-generated external dataset.

The revision therefore earns a modest increase for genuine code and CI improvements, but it does not pass the v1.4 external-validation acceptance gate.

### Credibility ratings

| Context | v1.3 | v1.4 | v1.5 | Interpretation at v1.5 |
|---|---:|---:|---:|---|
| Software implementation quality | 7.5 | 7.8 | **8.0 / 10** | Strong regression controls, clearer APIs, improved descriptor rules, clean build |
| Chemistry education/demonstrations | 7.9 | 8.1 | **8.2 / 10** | Broad, useful, and increasingly explicit about heuristic outputs |
| Developer prototyping | 7.3 | 7.6 | **7.8 / 10** | Good experimental foundation with stronger automation |
| Quantitative scientific analysis | 4.2 | 4.5 | **4.5 / 10** | Metrics improved, but reference independence is still asserted rather than demonstrated |
| Research/publication use | 3.1 | 3.4 | **3.5 / 10** | Reproducible internal fixtures, not yet reproducible external validation |
| Safety-of-scope | 4.0/5 | 4.3/5 | **4.4 / 5** | Unsafe medical predictions remain absent and surrounding language is controlled |
| Environmental decisions | 2.7 | 2.9 | **2.9 / 10** | No new empirical validation of pathways |
| **Overall claim-adjusted credibility** | **5.9** | **6.2** | **6.3 / 10** | Strong educational/prototyping project; external scientific evidence remains incomplete |

This rating is a structured engineering judgment, not a confidence interval. It weights claim fidelity, reference independence, applicability controls, reproducibility, failure behavior, and consequences of misuse.

## 2. Reproduced engineering evidence

| Check | v1.4 | v1.5 result |
|---|---:|---:|
| Release build with warnings as errors | Passed | **Passed, 0 warnings, 0 errors** |
| Automated tests | 143 passed | **147 passed, 0 failed, 0 skipped** |
| Core line coverage | 80.32% | **80.35%** (4,756 / 5,919) |
| Core branch coverage | 69.37% | **69.52%** (2,033 / 2,924) |
| High/critical vulnerable packages | None | **None reported by current NuGet audit** |
| Coverage artifact upload in CI | Absent | **Present** |
| Explicit vulnerability failure logic | Absent | **Present** |
| Dataset reproduction check | Informational fixture | **Present and passes byte-for-byte** |
| External tool execution during generation | Absent | **Still absent** |
| Coverage threshold | Absent | **Still absent** |

The dataset script prints SHA-256 `6053f35c...`, but the actual committed file SHA-256 is `a7d554e3...`. The script hashes the formatted JSON *before* the final newline, then writes the newline. The documentation calls the former the dataset checksum, which is not the hash of the actual file. CI uses `diff`, so reproducibility still works, but checksum terminology is incorrect.

## 3. v1.4 finding disposition

| v1.4 finding | v1.5 status | Evidence |
|---|---|---|
| Acid oxygen HBA error | **Resolved for tested carboxylic acids** | Acid OH environment excluded; dataset values corrected |
| Exact mass mislabeled | **Resolved** | Separate standard weight and monoisotopic exact mass fields |
| Formula sketch vs conformer ambiguity | **Substantially resolved** | `GenerateVseprSketch`, `GenerateConformer3D`, and `IsIdealizedVseprSketch` added |
| Benchmark metric visibility | **Improved** | Per-compound table plus MAE/RMSE/max error |
| Reference-data generator absent | **Superficially resolved** | Script exists, but only rewrites hard-coded values |
| Independent RDKit/NIST/PubChem generation | **Unresolved — High** | No external library, network source, input artifact, or parser is used |
| Dataset hash correctness | **Unresolved** | Printed/documented hash excludes final newline |
| CI vulnerable-dependency gate | **Resolved** | Output is inspected and workflow exits nonzero on reported vulnerability |
| CI coverage publication | **Resolved** | Cobertura artifact uploaded for 14 days |
| CI coverage threshold | **Unresolved** | Collection/upload only; no minimum enforced |
| Force-field external validation | **Unresolved** | New test checks energy does not rise, not torsion barrier/reference energy |
| Molfile/SDF independent round trip | **Unresolved** | New test checks marker strings, not parsing or structural round-trip |
| Representative external validation | **Unresolved** | Sixteen curated compounds remain too narrow and derivation is unverified |

## 4. Detailed assessment

### 4.1 The reference “generator” is a hard-coded serializer

The script's header says it reproducibly generates and verifies the dataset “using standard RDKit (2024.03+) and IUPAC CIAAW atomic weights.” Its implementation imports only Python standard-library modules. It does not import RDKit, call PubChem, read NIST data, calculate a descriptor, or verify any property.

Instead, `data_lookup` contains tuples for every result:

```text
name -> standard weight, exact mass, TPSA, LogP, QED, HBD, HBA, ROTB, aromatic rings
```

The generator copies each tuple into JSON and adds prose provenance strings. This produces a deterministic fixture but cannot establish that the values came from the named tools or references.

CI runs the same hard-coded script and compares its output with the committed file. This detects manual drift between two repository representations. It cannot detect:

- a wrongly transcribed value;
- a wrong descriptor definition;
- a wrong RDKit version claim;
- an incorrect molecule identifier or structure;
- a change between RDKit releases;
- a value copied from Chemy rather than the external implementation.

**Required classification:** curated regression-data serializer.  
**Required external generator:** actually install/import pinned RDKit and calculate each property from each SMILES, while obtaining or verifying other source-specific properties from archived/pinned inputs.

### 4.2 Provenance is more detailed but still not auditable

The JSON now contains per-property provenance strings, which is better than one record-level label. But every record repeats generic citations rather than identifying a particular source record, command, calculation setting, response hash, or retrieval event.

Examples of remaining ambiguity:

- “NIST Physical Measurement Laboratory” does not identify a table, isotope set, version, URL, or downloaded artifact;
- “IUPAC CIAAW 2021” does not show how interval atomic weights were converted to the single stored numbers;
- ChEMBL/PubChem identifiers are named, but no source record is archived and their role in calculated RDKit descriptors is unclear;
- reference values derived from RDKit should be reproducible directly from RDKit, not justified by a paper plus a text label;
- no license or redistribution note is attached to the derived dataset;
- the script usage says RDKit 2024.03+, while records claim exactly 2024.03.1.

The provenance is human-readable annotation, not a verifiable derivation chain.

### 4.3 Statistical reporting is a useful improvement

The tests now calculate signed errors internally and publish actual/reference rows, MAE, RMSE, and maximum absolute error. The documentation reports:

| Property | MAE | RMSE | Maximum error |
|---|---:|---:|---:|
| TPSA | 0.0000 Å² | 0.0000 Å² | 0.0000 Å² |
| LogP | 0.3063 | 0.4051 | 0.9840 |
| QED | 0.0716 | 0.0807 | 0.1300 |

This honestly exposes that the subset LogP/QED methods are approximate. It is more informative than a blanket “verified” label.

Interpretation remains limited:

- sixteen hand-selected small molecules do not characterize the stated chemical applicability domain;
- no confidence intervals or stratification by atom environment exist;
- TPSA exact agreement is expected because the fixture covers only environments the implementation explicitly matches;
- maximum LogP error near 1.0 is chemically material for some use cases;
- the QED errors combine descriptor and structural-alert differences;
- unsupported/default atom environments are not represented.

The phrase “Empirical Agreement ✅” should be replaced by a neutral metric statement plus applicability limitations.

### 4.4 HBA correction is real but not a full definition

The code now identifies an O–H attached to a carbonyl carbon and excludes it from HBA. The corrected reference counts for aspirin, ibuprofen, benzoic acid, and acetic acid now align with standard medicinal-chemistry treatment of carboxylic acids.

The rule set is still incomplete for pyrrolic/aromatic nitrogen, amidines/guanidines, sulfonamides, N-oxides, protonated/zwitterionic states, and other resonance/charge environments. The nitrogen comment mentions nitro nitrogens, but the code only implements amide exclusion plus positive-charge exclusion; no distinct nitro check appears in the changed logic.

**Disposition:** meaningful descriptor improvement, still a subset.

### 4.5 Geometry API now communicates two different scientific products

`GenerateVseprSketch(formula)` explicitly produces an idealized educational sketch. `GenerateConformer3D(bondedMolecule)` requires topology. `Molecule3D.IsIdealizedVseprSketch` carries the distinction into output, and XYZ labels idealized sketches. PDB connectivity remains limited to explicit source bonds.

This is a good API design improvement. The older `Generate3D(Molecule)` remains public and accepts both modes, so callers can still bypass the explicit distinction. Documentation and future APIs should prefer the two scoped entry points and eventually deprecate ambiguous use.

The bonded result is called “energy-minimized conformer,” but the UFF-inspired optimizer may not converge and the generator does not perform conformer search. A relaxed initial coordinate set is not necessarily a physically meaningful conformer or energy minimum.

### 4.6 New “force-field benchmark” does not test a torsion barrier

`Benchmark_ForceField_ButaneConformationalTorsionBarrier_RelaxesCoordinates` asserts that butane has 14 atoms, is not an idealized VSEPR sketch, and that final energy is no greater than initial energy. It does not:

- construct defined anti/gauche/eclipsed conformations;
- measure a torsional barrier;
- compare against UFF or experimental energies;
- assert convergence or gradient tolerance;
- validate bond lengths, angles, dihedrals, or component energies.

The test name overstates its evidence. It is an optimizer monotonicity smoke test.

### 4.7 New file “round-trip” test is not a round trip

`Benchmark_MolfileAndSdf_RoundTripStructureConservation` exports strings and checks for `V2000`, `M  END`, `$$$$`, and property headers. It does not parse the output back, compare atoms/bonds/charges/coordinates, or use an independent reader.

It therefore verifies serialization markers, not structure conservation, conformance, interoperability, or round-trip behavior. Rename it or implement an actual independent round trip.

### 4.8 CI controls are materially better but incomplete

Positive controls now include:

- release build with warnings as errors;
- tests with coverage collection;
- coverage artifact upload;
- explicit vulnerable-package output gate;
- deterministic JSON reproduction check.

Remaining gaps:

- no line or branch coverage minimum;
- no benchmark metric artifact or historical comparison;
- no true external reference-tool execution;
- GitHub Actions are version-tag pinned, not immutable commit-SHA pinned;
- shell grep depends on stable CLI output wording;
- no API integration, documentation execution, or independent file-format checks.

## 5. Updated subsystem scorecard

Scale: **5 supported**, **4 mostly supported**, **3 partially supported**, **2 weakly supported**, **1 contradicted**, **0 unsupported/unsafe**.

| Subsystem | v1.4 | v1.5 | Principal current finding |
|---|---:|---:|---|
| Elements/molar mass | 4.0 | **4.1** | Standard/exact labels separated; external derivation still hard-coded |
| Formula/topology boundary | 4.0 | **4.3** | Explicit sketch/conformer APIs; ambiguous legacy entry point remains |
| SMILES parsing | 2.8 | **2.8** | Safe limited subset, unchanged |
| Reaction balancing | 4.2 | **4.2** | Good scoped balancing; no material new algorithm change |
| Stoichiometry | 4.0 | **4.0** | Sound after valid balance |
| Solutions/electrochemistry/basic kinetics | 4.0–4.5 | **4.0–4.5** | Strong narrow textbook implementations |
| Reaction-network integration | 3.0 | **3.0** | No new external validation |
| Explicit Hückel solver | 4.2 | **4.2** | Analytical smoke tests retained |
| Automatic Hückel interpretation | 2.6 | **2.6** | Broader interpretations remain heuristic |
| Shomate thermodynamics | 3.8 | **3.8** | No new source reproducibility or database breadth |
| Empirical thermodynamic fallback | 1.5 | **1.5** | Unchanged weak model |
| Molecular mechanics | 3.0 | **3.1** | Optimizer smoke test added, not physical validation |
| 3D geometry/conformer | 2.3 | **2.8** | Product distinction improved; no conformer-quality corpus |
| TPSA subset | 2.9 | **3.0** | Metrics visible; fixture derivation and chemical coverage remain limited |
| LogP/MR subset | 2.4 | **2.5** | Honest errors published; max error remains large |
| QED-inspired score | 2.8 | **2.9** | Honest errors published; reduced alerts and inputs remain |
| Physicochemical profile | 3.0 | **3.3** | Acid HBA fixed; remaining atom environments incomplete |
| Spectroscopy | 2.3 | **2.3** | No new accuracy evidence |
| Ring perception | 3.5 | **3.7** | Anthracene/phenanthrene/biphenyl tests added |
| Lead exploration | 2.2 | **2.2** | Safely framed, still scripted enumeration |
| EcoClean | 2.2 | **2.2** | Safely framed, still speculative |
| File export | 2.7 | **2.8** | Marker smoke tests added, not conformance/round-trip |

## 6. Priority remediation

### P0 — make the generator truthful

1. Either rename it a curated-fixture serializer and remove “generated via RDKit/NIST/PubChem” claims, or make it actually invoke those pinned sources.
2. Install an exact RDKit build in CI and calculate TPSA, LogP, QED, HBD, HBA, rotatable bonds, aromatic rings, formula, and masses directly.
3. Fail if RDKit version differs from the declared version.
4. Compute/document the actual file hash, including final newline, or clearly call the current value a canonical-content hash.
5. Archive source inputs and licensing information for non-RDKit data.

### P1 — strengthen validation meaning

1. Expand the dataset by atom-type/environment coverage and out-of-domain cases.
2. Publish signed bias, percentiles, per-environment metrics, unsupported rate, and confidence intervals.
3. Rename the force-field and Molfile tests to smoke tests or implement their claimed barrier/round-trip comparisons.
4. Add actual independent parsing for Molfile/SDF/PDB exports.
5. Add force-field reference component energies, optimized geometries, and gradient checks.

### P2 — finish automation

1. Enforce coverage thresholds and publish summary metrics, not only raw Cobertura.
2. Publish benchmark results as revision-linked CI artifacts.
3. Execute documentation examples in CI.
4. Pin GitHub Actions by immutable SHA.
5. Prefer structured advisory output or a dedicated audit tool over grep-based wording detection.

## 7. Acceptance gate for v1.6

A rating clearly above **6.5 / 10** requires:

- actual pinned RDKit execution producing the committed reference descriptors;
- verifiable source artifacts for every non-calculated reference;
- correct file checksums and provenance metadata;
- representative applicability coverage and full error distributions;
- real coverage and benchmark CI thresholds;
- at least one external force-field, spectroscopy, conformer, or file-format validation suite.

## 8. Final verdict

Revision `ddb13b9` improves credibility from **6.2 to 6.3 / 10**. The acid HBA fix, mass terminology, geometry API separation, metric reporting, and CI gates are all valuable and correctly targeted.

The external-validation blocker remains. The repository now has a deterministic generator, but not an external one: it serializes manually embedded values while claiming to derive them from tools it never executes. This should be corrected before the benchmark is used as evidence of scientific accuracy.

**Overall claim-adjusted credibility: 6.3 / 10.**

Chemy is credible educational and developer-prototyping software with several sound narrow calculations and increasingly strong engineering controls. Research-grade quantitative claims still require authentic independent reference generation, representative datasets, and domain-specific validation.

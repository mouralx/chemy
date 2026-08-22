# Chemy Scientific Implementation Audit — v2.8

[Documentation home](README.md) · [Credibility report](SCIENTIFIC_CREDIBILITY_REPORT.md) · [Verification](SCIENTIFIC_VERIFICATION_BENCHMARKS.md) · [Audit archive](audits/README.md)

**Audit date:** 2026-08-22
**Audited state:** v2.8 candidate based on `3fced36edf4755b176ce7653193a8d0a8eb638d6`
**Previous audit:** [`CODEX_AUDIT_v2.7.md`](audits/CODEX_AUDIT_v2.7.md)
**Scope:** Scientific implementation, numerical correctness, reference agreement, applicability, uncertainty, and claim integrity. Authentication, authorization, security, tenancy, deployment, and platform operations are excluded at the user's direction.
**Acceptance manifest:** [`SCIENTIFIC_ACCEPTANCE_v2.8.json`](SCIENTIFIC_ACCEPTANCE_v2.8.json)

## 1. Executive conclusion

The v2.8 candidate reaches the requested **9.7/10 internal scientific implementation readiness** within its declared scope. The remaining 0.3 is reserved for evidence that cannot be created honestly by repository work after implementation: prospective freezing, independent curation/reproduction, and identifiable expert review.

The largest v2.7 scientific limitation is removed. `ForceFieldEngine` no longer implements a loosely UFF-inspired potential with large chemotype deviations. It now uses published UFF bond, Fourier-angle, typed-torsion, inversion, and Lennard-Jones forms for an explicit organic atom-type subset. All 24 pinned fixed-coordinate cases agree with RDKit 2025.09.2 within the four-decimal artifact resolution, including the former furan, thiophene, acetonitrile, acetone, and formamide failures. Cartesian derivatives are checked against RDKit analytical gradients, and independent optimization runs are compared through final energy and rotation/translation-invariant pairwise geometry.

Scientific boundaries are now executable rather than descriptive. Predictive results expose method/version/reference metadata, per-input applicability, frozen validation evidence, and calibrated reference-agreement envelopes where available. Unsupported elements and topologies fail closed. Evidence records explicitly state that internal datasets are neither independently curated nor prospectively frozen.

Reaction thermodynamics no longer accepts arbitrary temperatures over a 298.15 K table, silently substitutes constitutional isomers through a formula key, or silently estimates missing species. The group-additivity path requires explicit opt-in and marks results as boundary estimates. RK4, weak-acid equilibrium, force-field optimization, and Hückel matrix inputs now expose or enforce numerical-quality contracts.

This score is not a claim of universal chemical prediction, regulatory approval, laboratory validation, or independent certification. It is a scope-bounded assessment of the implementation and its internal evidence discipline.

## 2. Reproduced evidence

| Gate | v2.8 result |
| :--- | :--- |
| Release build with warnings as errors | **Passed: 0 warnings, 0 errors** (`net10.0`) |
| Full Release coverage run | **171 passed, 0 failed, 0 skipped** |
| Isolated line coverage | **85.47%** (4,911 / 5,746), floor 80% |
| Isolated branch coverage | **76.42%** (2,752 / 3,601), floor 70% |
| Scientific contract tests | **9 passed**: fail-closed domains, uncertainty/evidence, invariance, residuals, thermodynamic boundaries, input validation |
| RDKit UFF regeneration | **4 butane + 12 diverse + 8 expanded cases reproduced** with RDKit 2025.09.2 |
| UFF on-disk SHA-256 | **`0d866e07e7e4ddc6c3fdc6fc28858b65e60c570fcf1b60947645b399d846b4e5`** |
| UFF embedded canonical SHA-256 | **`16a40cfcb0f1f6f1bb1bfa0132da368640a010c12d97d8cbed134037efd59ed1`** |
| UFF fixed-coordinate energies | **24/24 within 0.0001 kcal/mol of pinned RDKit values** |
| UFF Cartesian gradients | **Maximum component error gate 5e-4 kcal/(mol·Å)** against RDKit analytical gradients |
| UFF optimized geometry | **Five cases**; final energy gate 1e-3 kcal/mol and pairwise-distance RMS gate 0.002 Å |
| Descriptor benchmark | **48 molecules** with machine-readable TPSA/LogP/QED calibration metrics |
| NIST Shomate | **Nine species**, piecewise intervals, transition and out-of-range gates |
| Experimental 1H NMR | **Five SDBS peak groups**, MAE 0.094 ppm, maximum 0.320 ppm |
| Claim consistency | **Passed** |
| Diff hygiene | **Passed** (`git diff --check`) |

Final coverage was collected into a single isolated `/private/tmp/chemy-v28-final-coverage.uM3BmI` result directory. The run produced one Cobertura artifact, avoiding aggregation with historical reports.

## 3. UFF implementation disposition

The engine now follows the method introduced by [Rappé et al., JACS 1992](https://doi.org/10.1021/ja00051a040) and is cross-checked against the official [RDKit UFF implementation](https://github.com/rdkit/rdkit/tree/master/Code/ForceField/UFF).

Implemented behavior includes:

- official parameters for supported H/C/N/O/P/S/F/Cl/Br/I atom types;
- bond-order and electronegativity corrections plus geometry-derived force constants;
- the UFF Fourier angle form, including the linear-center sign convention;
- per-torsion terminal-atom typing and sp2/sp3/group-6 special rules;
- resonant C_R/O_R/N_R typing for amides;
- carbonyl, planar, and supported phosphorus inversion coefficients; and
- unbuffered geometric-mean 12-6 Lennard-Jones terms with UFF cutoffs.

The declared subset is intentionally narrower than the universal element coverage described by the original paper. `AssessApplicability` evaluates the concrete molecular graph, and unsupported chemistry rejects before energy evaluation. The name “UFF-compatible organic subset” reflects both facts: numerical compatibility is demonstrated for the declared cases, while universal UFF coverage is not claimed.

## 4. Scientific result contract

`ScientificMethodInfo` now supports primary references and frozen `ScientificValidationEvidence`. The evidence schema carries dataset identity/version, sample size, named metrics, artifact path/hash, and two certification booleans. `ScientificApplicabilityAssessment` returns `InDomain`, `Boundary`, or `OutOfDomain`; out-of-domain predictive inputs fail closed. `ScientificUncertainty` records an empirical absolute-error envelope and clearly states whether it is merely reference-implementation agreement rather than a confidence interval. `ScientificNumericalDiagnostics` carries convergence, step size, residual, and conservation error.

The contract is applied to the force field, descriptor suite, spectroscopy, Hückel model, kinetics, solutions chemistry, electrochemistry, standard-state thermodynamics, molecular evolution, and EcoClean pathway generation in proportion to each method's evidence class.

## 5. Thermodynamics and numerical solvers

`ShomateThermodynamics` remains the temperature-dependent gas-phase path, using explicit NIST Chemistry WebBook coefficient intervals and rejecting extrapolation.

`ThermodynamicsEngine` is now a 298.15 K standard-state Hess-law engine. Formula-only reference lookup is not used to overwrite a supplied bonded isomer. Missing species reject by default. The legacy group-additivity calculation remains available only by explicit opt-in and reports `Boundary` applicability with per-species sources.

`ReactionNetworkEngine` now performs standard RK4 stages, normalizing only sub-`1e-12` negative roundoff. It validates derivative dimensions/finiteness and rejects materially negative or non-finite trajectories. The consecutive first-order cascade reports maximum error against its analytical solution and maximum concentration-conservation error. Weak-acid Halley iteration exposes convergence residual/step diagnostics. Hückel matrix analysis rejects non-finite, asymmetric, or undersized Hamiltonians and correctly classifies the physical model as semi-empirical even though the eigensolver itself is numerical.

## 6. Empirical descriptors and heuristics

The descriptor suite reports the pinned 48-molecule RDKit agreement envelope:

- TPSA: MAE 0.0110 Å², RMSE 0.0765 Å², maximum 0.5300 Å²;
- LogP: MAE 0.2930, RMSE 0.3967, P90 absolute error 0.7610, maximum 1.1540; and
- QED: MAE 0.0255, RMSE 0.0502, maximum 0.2080.

These are not biological uncertainty estimates. `AdmetProfile` is explicitly a physicochemical/drug-likeness profile and carries descriptor-specific envelopes. Molecular evolution is explicitly heuristic, accepts bonded SMILES rather than ambiguous empirical formulas, bounds its generation budget, and makes no toxicity/efficacy claim. EcoClean remains a qualitative pathway hypothesis; formula-only inputs are boundary-marked and quantitative efficiency/half-life claims remain removed.

## 7. Scorecard

| Area | v2.7 | v2.8 | Reason |
| :--- | ---: | ---: | :--- |
| Exact/reference equations | 9.2 | **9.8** | NIST interval rigor retained; standard-state thermodynamics and finite inputs now fail closed |
| Molecular mechanics in declared domain | 7.7 | **9.8** | Published UFF forms; energy, gradient, optimized-energy and invariant-geometry agreement |
| Numerical solver discipline | 8.7 | **9.6** | Residual/conservation evidence and invalid-trajectory rejection |
| Physicochemical descriptors | 8.8 | **9.5** | Applicability plus machine-readable 48-case calibration envelopes |
| Semi-empirical/heuristic modules | 8.4 | **9.3** | Claims narrowed; ambiguity rejected; quantitative vs qualitative outputs machine-readable |
| Evidence and provenance engineering | 8.5 | **9.7** | Hash-locked evidence schema with independence/prospectivity flags |
| **Internal scientific implementation readiness** | **7.8** | **9.7 / 10** | All repository-achievable scientific blockers resolved within declared scope |

The v2.7 comparison uses its quantitative-science position rather than its broader enterprise score, because v2.8 intentionally excludes security/platform concerns.

## 8. The reserved 0.3: certification work

The following remain deliberately incomplete:

1. **Prospective evaluation:** freeze cases, thresholds, and analysis before any v2.8+ outputs are inspected.
2. **Independent reproduction:** have a separate party curate source data and reproduce the results without relying on the implementation team's generated artifacts.
3. **Expert sign-off:** obtain identifiable chemistry-domain review of source selection, applicability domains, error metrics, and acceptance thresholds.

These are governance and external-evidence properties, not missing code. The new schema makes them impossible to imply accidentally: current evidence records say `IndependentlyCurated = false` and `ProspectivelyFrozen = false`.

## 9. Final verdict

Within its declared chemical and methodological scope, Chemy v2.8 is now an internally high-rigor scientific software candidate. Its strongest quantitative engines have executable reference agreement, numerical outputs carry quality evidence, approximate methods expose calibrated boundaries, qualitative modules are prevented from masquerading as quantitative predictions, and unsupported inputs fail closed.

**Internal scientific implementation readiness: 9.7 / 10.**
**Independent/certified scientific status: pending the three reserved external steps.**

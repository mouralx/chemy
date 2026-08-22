# Chemy Scientific Credibility Report

> Current implementation report for v2.8. For the versioned assessment and score, see [`CODEX_AUDIT_v2.8.md`](CODEX_AUDIT_v2.8.md).

## Scientific contract

Chemy separates four kinds of output:

1. exact equations and algebraic solvers;
2. converged numerical approximations with residual diagnostics;
3. peer-reviewed empirical models with explicit applicability and calibration evidence; and
4. qualitative heuristics that are not quantitative predictions.

`ScientificMethodInfo` carries method name, version, evidence class, applicability statement, warnings, primary references, and optional frozen validation evidence. Predictive results expose a per-input `ScientificApplicabilityAssessment`. Empirical descriptors expose a `ScientificUncertainty` reference-agreement envelope; iterative solvers expose `ScientificNumericalDiagnostics` where applicable.

The validation model deliberately records whether evidence was independently curated and prospectively frozen. Current internal v2.8 artifacts set both certification flags to `false`; independent certification remains future work.

## Capability matrix

| Domain | Implemented method | Classification | Executable evidence and boundary |
| :--- | :--- | :---: | :--- |
| Stoichiometry | Exact rational nullspace with charge conservation | Exact equation | Integer mass/charge conservation tests |
| Shomate thermodynamics | Piecewise NIST gas-phase polynomial equations | Reference equation | Nine species; interval and out-of-range gates |
| Standard-state reaction thermodynamics | Hess's law at 298.15 K | Exact equation over reference data | Missing species and other temperatures fail closed; estimated fallback is explicit and boundary-marked |
| Electrochemistry | Nernst equation with CODATA constants and pinned standard potentials | Exact equation | 29 couples plus analytical Daniell-cell comparison |
| Acid/base solutions | Strong-acid quadratic and weak-acid cubic/Halley solution | Exact equation/numerical root | Residual diagnostics; ideal dilute aqueous 25 °C domain |
| Reaction networks | Classical fixed-step RK4 | Numerical approximation | Analytical consecutive-cascade residual and conservation diagnostics; invalid derivatives/trajectories reject |
| Hückel orbitals | Semi-empirical HMO Hamiltonian plus Jacobi eigensolver | Empirical model | Analytical eigenvalue tests; finite symmetric-matrix and chemical-domain checks |
| Molecular mechanics | Published five-term UFF forms for the declared organic subset | Empirical model | 24 RDKit energies, gradients, optimized energies and invariant geometry checks |
| TPSA | Ertl-inspired polar fragment implementation | Empirical subset | 48-molecule RDKit benchmark: MAE 0.011 Å², maximum 0.530 Å² |
| LogP/MR | Crippen-inspired core atom-type implementation | Empirical subset | 48-molecule RDKit benchmark: MAE 0.293, P90 absolute error 0.761 |
| QED | Bickerton desirability equation over Chemy descriptors | Empirical composite | 48-molecule RDKit benchmark: MAE 0.0255, maximum 0.208 |
| 1H NMR | Topological groups and empirical shifts/splitting | Empirical/heuristic | Five SDBS peak groups: MAE 0.094 ppm, maximum 0.320 ppm |
| 13C NMR and IR | Topological/functional-group correlation rules | Heuristic | No calibrated numerical uncertainty yet; result warning is explicit |
| Molecular evolution | Rule-based graph mutation ranked by descriptors | Heuristic | Bonded SMILES only; no potency, safety, metabolism, or clinical claim |
| EcoClean | BDE-informed qualitative pathway construction | Heuristic | Formula input is boundary-marked; no kinetic/mineralization-efficiency claim |

## UFF-compatible organic subset

`ForceFieldEngine` implements:

- UFF bond-order and electronegativity corrections;
- geometry-derived bond and angle force constants;
- Fourier angle functions for linear, trigonal, and general centers;
- typed sp2/sp3/group-6 torsion rules;
- carbonyl, planar, and supported phosphorus inversion terms;
- geometric-mean 12-6 Lennard-Jones parameters and UFF cutoffs; and
- resonant C_R/O_R/N_R typing for amides.

The declared element domain is H, C, N, O, P, S, F, Cl, Br, and I for the explicitly implemented atom types. `AssessApplicability` preflights a molecular graph; unsupported chemistry fails before a numerical result is returned.

The pinned RDKit 2025.09.2 artifact covers four butane conformers, twelve diverse molecules, eight expanded regression molecules, analytical-reference Cartesian gradients, and optimized pairwise-distance invariants. Fixed-coordinate energies agree within the artifact's four-decimal resolution. Gradient component differences are gated at 5e-4 kcal/(mol·Å), and optimized pairwise-distance RMS differences at 0.002 Å.

These results establish reference-implementation agreement for the declared cases. They do not prove universal UFF coverage, prospective generalization, or independent certification.

## Thermodynamic boundaries

`ShomateThermodynamics` is the temperature-dependent gas-phase route and rejects temperatures outside published coefficient intervals.

`ThermodynamicsEngine` is restricted to its 298.15 K standard-state property table. It no longer accepts arbitrary temperatures, silently substitutes a constitutional isomer from a formula key, or silently estimates missing compounds. Callers may explicitly request the legacy group-additivity estimate; such results identify every property source and receive `Boundary` applicability status.

## Descriptor calibration

The 48-molecule artifact contains tuning, expanded-regression, and post-development evaluation partitions. The combined reference-agreement metrics are:

- TPSA: MAE 0.0110 Å²; RMSE 0.0765 Å²; maximum absolute error 0.5300 Å².
- LogP: MAE 0.2930; RMSE 0.3967; P90 absolute error 0.7610; maximum 1.1540.
- QED: MAE 0.0255; RMSE 0.0502; maximum absolute error 0.2080.

These quantify agreement with RDKit 2025.09.2 for this implementation subset. They are not experimental pharmacokinetic or clinical uncertainty intervals. `AdmetProfile` is therefore a physicochemical/drug-likeness profile, not an ADME, toxicity, efficacy, or safety prediction.

## Evidence still reserved for certification

The internal scientific implementation target can be assessed at 9.7/10 only with the following 0.3 explicitly reserved:

1. an evaluation manifest frozen before any result or threshold is observed;
2. independently curated reference data and reproduction by a separate party; and
3. identifiable domain-expert review/sign-off of datasets, applicability domains, and acceptance thresholds.

No repository-only change can honestly manufacture those three forms of evidence after implementation.

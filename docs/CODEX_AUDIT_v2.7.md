# Chemy Enterprise & Scientific Credibility Audit — v2.7

**Audit date:** 2026-08-22
**Audited state:** uncommitted working tree based on `c932190` (`main`)
**Previous audit:** [`CODEX_AUDIT_v2.6.md`](CODEX_AUDIT_v2.6.md), uncommitted candidate based on `c932190`
**Auditor and implementer:** OpenAI Codex
**Scope:** Direct remediation of every v2.6 acceptance criterion; enterprise API-boundary hardening; source, test and public-claim review; warning-as-error Release build; complete tests with and without fresh coverage; live RDKit dataset regeneration; strict positive/negative interoperability; dependency advisory scan; and Development/Production API smoke tests.

## 1. Executive conclusion

The v2.7 candidate resolves the two v2.6 P0 thermodynamics defects. Ethylene now uses the official NIST Shomate coefficients, all nine supported gases have species-specific piecewise intervals through their published upper range, interval transitions are deterministic, and out-of-range calls reject rather than extrapolate. Tests cover values, transitions, source URLs and failure paths.

The force-field contract is also materially stronger. The implementation and every active public surface now describe five terms, carbonyl carbon receives the documented 50 kcal/mol inversion constant, sulfur is removed from the unsupported inversion-center path, iodomethane has an executable gate, and per-component energy is inspectable. The expanded RDKit set is honestly labeled post-development regression evidence—not held-out or equivalence evidence. Large furan, thiophene and acetonitrile deviations remain visible as applicability limitations.

Optimization is repaired rather than cosmetically relabeled. Bounded-memory L-BFGS with monotonic Armijo line search replaces the slow primary steepest-descent path. Convergence is accepted only near a stationary point; invalid controls fail closed; the detailed geometry API retains termination, gradient and energy evidence; and ethanol, benzene and aspirin converge under the declared 500-iteration production budget. The HTTP endpoint no longer minimizes an already minimized conformer.

The service boundary now provides an enterprise engineering baseline: Production refuses to start without an externally supplied API credential, `/api/v1` authentication uses a constant-time comparison, unconfigured CORS and hosts fail closed, requests are rate/body bounded, correlation IDs and generic problem responses are emitted, registered health checks actually run, and production API documentation is hidden by default. A live Production smoke test returned 401 without the credential, 200 with it, and 204 for the hidden root documentation route.

This does **not** make Chemy a certified enterprise scientific platform. The repository still lacks corporate OAuth2/OIDC authorization, tenant isolation, durable audit/event storage, deployment SLOs, backup/restore evidence and automated API middleware integration tests. Several scientific engines remain educational or approximate, and the UFF-inspired subset is not numerically interchangeable with RDKit UFF.

**Overall claim-adjusted credibility: 8.6 / 10.** The candidate is an enterprise-capable engineering baseline with enforceable scientific boundaries, not yet a regulated, multi-tenant or publication-general platform.

## 2. Reproduced evidence

| Check | v2.7 candidate result |
|---|---|
| Release build with warnings as errors | **Passed: 0 warnings, 0 errors** (`net10.0`) |
| Tests without coverage | **162 passed, 0 failed, 0 skipped** in about 2s |
| Tests with isolated fresh coverage | **162 passed, 0 failed, 0 skipped** in about 62s |
| Fresh line coverage | **82.74%** (4,156 / 5,023), floor 80% |
| Fresh branch coverage | **75.99%** (2,592 / 3,411), floor 70% |
| RDKit descriptor artifact | **48 compounds reproduced with RDKit 2025.09.2** |
| UFF artifact regeneration | **4 butane + 12 standard + 8 expanded-regression cases reproduced** |
| UFF on-disk SHA-256 | **`ea6bfc116f2f19f000e45c1e676734acccfb7434d8da001ab14fa8d3fbbe073c`** |
| UFF embedded canonical SHA-256 | **`b031cbbc60b989d79b5bd638c9d64a693549ea433e06ba60a78192b8d93e62fa`** |
| RDKit → Chemy fixtures | **Generated and parsed by the full .NET suite** |
| Chemy → RDKit strict interop | **Passed against explicit Release output** |
| Missing explicit interop path | **Correctly rejected with exit code 1** |
| Dependency advisory audit | **No vulnerable direct or transitive NuGet packages reported** |
| Claim/evidence consistency | **Passed; now enforced in CI** |
| Production API smoke test | **401 unauthenticated; 200 authenticated; documentation hidden** |
| Diff hygiene | **Passed** (`git diff --check`) |

Coverage was collected into an isolated `/private/tmp/chemy-coverage-v27` result directory and verified from the single fresh Cobertura file, avoiding historical-report aggregation.

## 3. v2.6 acceptance-criteria disposition

| v2.6 criterion | Status | Evidence |
|---|---|---|
| NIST-correct ethylene coefficients and values | **Resolved** | Correct 298–1200 K and 1200–6000 K C2H4 segments; tight value tests |
| Real species-specific Shomate intervals | **Resolved** | Piecewise model, transition selection, supported-range API and rejection outside published intervals |
| Remove unsupported holdout language | **Resolved** | Contract renamed `expanded_regression_molecules`; claims call it post-development regression |
| UFF tables match live output and executable gates | **Resolved** | Current formamide/halomethane values documented; iodine assertion added |
| Carbonyl inversion behavior | **Resolved for implemented model** | 50 vs 6 constant selected and component-level 50/6 regression test added |
| README/method agreement | **Resolved** | Five-term and bounded-allocation wording synchronized across code, UI and active docs |
| Externally reviewable future holdout chronology | **Not claimed / future work** | Current partition is explicitly not prospective; no false status remains |
| Geometry convergence evidence | **Resolved for declared common set** | Detailed API plus 100-vs-500 evidence for ethanol, benzene and aspirin |

## 4. Major implementation outcomes

### 4.1 Piecewise NIST thermodynamics now fails scientifically closed

`ShomateThermodynamics` stores explicit `TMinKelvin` and `TMaxKelvin` values on every coefficient segment. It uses half-open internal intervals with an inclusive final endpoint, reports the selected interval and source URL, rejects non-finite/non-positive temperatures, and refuses extrapolation for known species.

The corrected ethylene rows match the official [NIST Chemistry WebBook C2H4 gas-phase table](https://webbook.nist.gov/cgi/cbook.cgi?ID=C74851&Table=on&Type=JANAFG). Piecewise ranges and values were also checked against the official NIST records for [H2O](https://webbook.nist.gov/cgi/cbook.cgi?ID=C7732185&Table=on&Type=JANAFG), [CO2](https://webbook.nist.gov/cgi/cbook.cgi?ID=C124389&Table=on&Type=JANAFG), [CO](https://webbook.nist.gov/cgi/cbook.cgi?ID=C630080&Table=on&Type=JANAFG), [CH4](https://webbook.nist.gov/cgi/cbook.cgi?ID=C74828&Table=on&Type=JANAFG), [O2](https://webbook.nist.gov/cgi/cbook.cgi?ID=C7782447&Table=on&Type=JANAFG), [N2](https://webbook.nist.gov/cgi/cbook.cgi?ID=C7727379&Table=on&Type=JANAFG), [H2](https://webbook.nist.gov/cgi/cbook.cgi?ID=C1333740&Table=on&Type=JANAFG) and [NH3](https://webbook.nist.gov/cgi/cbook.cgi?ID=C7664417&Table=on&Type=JANAFG).

The public API no longer rounds scientific outputs before returning them. Display formatting remains a consumer concern.

### 4.2 The UFF-inspired model is auditable and honestly bounded

The implemented total is now inspectable as bond stretch, angle bend, torsion, inversion and van der Waals components. Carbonyl-specific inversion behavior follows the relevant constant path visible in RDKit's official [UFF parameter utilities](https://raw.githubusercontent.com/rdkit/rdkit/master/Code/ForceField/UFF/Utils.cpp), [inversion energy](https://raw.githubusercontent.com/rdkit/rdkit/master/Code/ForceField/UFF/Inversion.cpp) and [force-field builder](https://raw.githubusercontent.com/rdkit/rdkit/master/Code/GraphMol/ForceFieldHelpers/UFF/Builder.cpp), derived from the [Rappé et al. UFF method](https://pubs.acs.org/doi/10.1021/ja00051a040).

The model remains intentionally classified as a UFF-inspired subset. It uses simplified harmonic angle behavior, soft-core nonbonded handling and no electrostatic point-charge term. The expanded regression retains large differences for furan, thiophene and acetonitrile. Those observations are diagnostic applicability evidence, not equivalence passes.

### 4.3 Optimization now has a production contract

The optimizer precomputes topology, uses central finite-difference gradients, bounded seven-pair L-BFGS history and an Armijo sufficient-decrease line search. It restarts with a safe descent direction when curvature/noise invalidates the quasi-Newton direction. An energy plateau is accepted only when the gradient is also close to tolerance, preventing tiny steps with large forces from being called converged.

`EnergyMinimizationResult` retains exact termination reason, iteration count, maximum Cartesian gradient component, initial/final energies and coordinates. Defaults are 500 iterations and `1e-3` kcal/(mol·Å). The HTTP request is capped at 2,000 iterations to prevent an unbounded computational request.

### 4.4 Enterprise controls are now real defaults

The API boundary adds:

- Production startup failure when authentication is enabled but the secret is absent;
- constant-time `X-Api-Key` comparison for `/api/v1` routes;
- exact-origin CORS with no permissive fallback;
- loopback-only default host filtering, requiring explicit deployment host configuration;
- per-client fixed-window rate limiting with no queued overflow;
- a 64 KiB default Kestrel request-body limit and bounded headers/keep-alive timing;
- sanitized/propagated correlation IDs and structured logging scope;
- generic RFC-style problem responses with trace IDs;
- `nosniff`, frame-denial, referrer and HSTS controls;
- actual registered health-check execution; and
- production-hidden OpenAPI, Scalar and Swagger surfaces unless explicitly enabled.

The repository intentionally keeps the credential empty. Deployments must inject `ApiSecurity__ApiKey` through their approved secret provider and set exact origins and hosts.

## 5. Remaining limitations and enterprise backlog

### P1 — replace the baseline key with organizational identity

An API key is deployable and fail-closed, but it provides service authentication rather than user/workload identity, roles or fine-grained authorization. Production organizations should integrate OAuth2/OIDC or workload identity at an approved gateway and define scopes for expensive/scientific operations. Rotation and revocation procedures also need operational ownership.

### P1 — add automated API integration/security tests

The Development and Production middleware paths were live-smoke-tested in this audit, while CI statically verifies the required controls. A dedicated API integration test project should automatically exercise 401/200 behavior, missing-secret startup, 413 bodies, 429 limits, CORS denial/allowance, correlation sanitization, problem responses and hidden production documentation.

### P1 — establish production operations

The repository does not yet supply a deployment-specific threat model, SLO/error budget, centralized audit/event sink, dashboards/alerts, capacity tests, disaster recovery exercise, SBOM/signing/provenance policy, tenant isolation or data-retention policy. These are required before calling a deployment enterprise-certified or regulated.

### P1 — broaden scientific validation without widening claims

The UFF-inspired subset is suitable for bounded prototyping and conformer relaxation, not cross-tool energy equivalence. Future work needs a preregistered/frozen evaluation set published before results, gradient/geometry comparisons, and explicit per-chemotype acceptance criteria. Other predictive modules need similar external applicability-domain validation before production decision use.

### P2 — complete independent data review

NMR and electrochemistry artifacts are hash-locked and better classified, but still benefit from immutable source captures, identifiable reviewer/date, discrepancy worksheets and repeatable extraction where licensing permits.

## 6. Updated scorecard

| Area | v2.6 | v2.7 | Principal reason |
|---|---:|---:|---|
| Software implementation quality | 9.3 | **9.5 / 10** | Warning-clean build, 162 tests, stronger optimizer, executable consistency and security gates |
| Enterprise service baseline | Not scored | **8.4 / 10** | Fail-closed auth/CORS/hosts, rate/body limits, health/errors/correlation; IAM and API integration suite remain |
| Chemistry education/demonstrations | 8.6 | **9.1 / 10** | Correct NIST data and sharply clearer model boundaries |
| Developer prototyping | 9.2 | **9.4 / 10** | Detailed diagnostics, piecewise thermodynamics and stable reproducible artifacts |
| Quantitative scientific analysis | 6.7 | **7.7 / 10** | NIST truth restored and UFF component/convergence evidence improved; UFF deviations remain |
| Research/publication use | 4.8 | **6.4 / 10** | Thermodynamic blockers fixed, but broad model validation and independent provenance remain incomplete |
| Overall claim-adjusted credibility | 7.8 | **8.6 / 10** | All v2.6 acceptance blockers resolved without inflating remaining claims |

## 7. Acceptance criteria for v2.8

A further enterprise/credibility increase should require:

1. an automated API integration/security test project covering every middleware control and negative path;
2. an approved OAuth2/OIDC or workload-identity design with scope/role enforcement, or an explicit gateway integration contract;
3. a threat model, deployment topology, SLOs, observability/alert rules and secret-rotation runbook;
4. SBOM generation plus artifact signing/provenance verification in CI;
5. a frozen scientific evaluation manifest committed before results or threshold changes;
6. gradient, optimized-geometry and per-component external comparisons across declared chemotypes; and
7. identifiable independent review artifacts for scientific reference datasets.

## 8. Final verdict

This candidate is no longer merely an educational demo with optimistic scientific labels. It has enforceable source ranges, reproducible external references, honest applicability wording, inspectable optimization quality and a fail-closed HTTP boundary suitable as the foundation of an enterprise deployment.

The distinction matters: **enterprise-capable baseline** is justified; **enterprise-certified scientific platform** is not yet justified. Corporate identity/authorization, automated boundary testing, operating controls and broader external validation remain necessary before regulated or high-consequence use.

The v2.7 candidate is therefore assessed at **8.6 / 10 overall claim-adjusted credibility**.

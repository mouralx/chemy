# Chemy Documentation

[Project home](../README.md) · [Getting started](GETTING_STARTED.md) · [Cookbook](COOKBOOK.md) · [API reference](API_REFERENCE.md) · [Scientific status](SCIENTIFIC_CREDIBILITY_REPORT.md)

This is the canonical entry point for Chemy documentation. Choose a path below instead of reading the documents in filename order.

## Start and use Chemy

1. [Getting Started](GETTING_STARTED.md) — prerequisites, restore/build/test commands, project references, and how to launch the API or web workstation.
2. [Cookbook](COOKBOOK.md) — focused C# recipes for common scientific operations.
3. [API Reference](API_REFERENCE.md) — public C# types and HTTP endpoints.

## Understand the implementation

- [Architecture](ARCHITECTURE.md) explains project boundaries, component relationships, numerical infrastructure, and the API boundary.
- [Scientific Approach](SCIENTIFIC_APPROACH.md) explains implemented equations, model assumptions, applicability domains, and numerical methods.
- [Use Cases and Demonstrations](BREAKTHROUGHS_SHOWCASE.md) provides illustrative workflows. These demonstrations do not add validation beyond the evidence documents below.

## Evaluate scientific evidence

Read these in order when assessing scientific readiness:

1. [Scientific Credibility Report](SCIENTIFIC_CREDIBILITY_REPORT.md) — concise, current capability matrix and limitations.
2. [Scientific Verification and Benchmarks](SCIENTIFIC_VERIFICATION_BENCHMARKS.md) — benchmark definitions, reference sources, metrics, and interpretation.
3. [Scientific Audit v2.8](CODEX_AUDIT_v2.8.md) — the current versioned assessment and 9.7/10 internal-readiness score.
4. [Scientific Acceptance Manifest v2.8](SCIENTIFIC_ACCEPTANCE_v2.8.json) — machine-readable gates and certification flags.

The remaining external certification work is prospective freezing, independent reproduction, and identifiable domain-expert review. Internal evidence must not be described as completing those steps.

## Audit history

The latest audit remains at [Scientific Audit v2.8](CODEX_AUDIT_v2.8.md). Earlier audits are preserved, with their original `CODEX_AUDIT_vX.Y.md` filename pattern, in the [audit archive](audits/README.md).

Historical audits describe the repository state at the time they were written. Use them to follow remediation decisions, not as documentation of current behavior.

## Document ownership

| Document | Primary question | Update when |
| :--- | :--- | :--- |
| [Getting Started](GETTING_STARTED.md) | How do I install and run it? | Commands, prerequisites, ports, or startup behavior change |
| [Cookbook](COOKBOOK.md) | How do I perform a task? | Public C# usage patterns change |
| [API Reference](API_REFERENCE.md) | What can I call? | Public types, fields, routes, or payloads change |
| [Architecture](ARCHITECTURE.md) | How is it structured? | Components, dependencies, or control flow change |
| [Scientific Approach](SCIENTIFIC_APPROACH.md) | What methods are implemented? | Equations, algorithms, domains, or assumptions change |
| [Verification and Benchmarks](SCIENTIFIC_VERIFICATION_BENCHMARKS.md) | What evidence executes? | Fixtures, references, thresholds, or metrics change |
| [Credibility Report](SCIENTIFIC_CREDIBILITY_REPORT.md) | What is scientifically supportable now? | Capability boundaries or evidence interpretation change |
| [Current Audit](CODEX_AUDIT_v2.8.md) | What was the versioned verdict? | Never rewrite for a later version; add the next patterned audit |

## Repository documentation layout

```text
docs/
├── README.md                                  Documentation home
├── GETTING_STARTED.md                         Installation and first run
├── COOKBOOK.md                                Task-oriented C# examples
├── API_REFERENCE.md                           C# and REST surface
├── ARCHITECTURE.md                            Software and numerical architecture
├── SCIENTIFIC_APPROACH.md                     Methods and equations
├── SCIENTIFIC_CREDIBILITY_REPORT.md           Current scientific status
├── SCIENTIFIC_VERIFICATION_BENCHMARKS.md      Evidence and benchmark details
├── BREAKTHROUGHS_SHOWCASE.md                  Illustrative use cases
├── CODEX_AUDIT_v2.8.md                        Current versioned audit
├── SCIENTIFIC_ACCEPTANCE_v2.8.json            Machine-readable acceptance record
├── audits/                                    Historical patterned audits
└── images/                                    Documentation images
```

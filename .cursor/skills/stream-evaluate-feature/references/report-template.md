# Evaluation report template

Write a repo-specific decision memo, not a filled-in checklist. Lead with the verdict. Cite repo paths and symbols inline, and give links and versions for external sources. Never fabricate results.

Scale to the risk tier:

- **Small:** sections 1, 2, 3, 5, 6, 11, and 12. Compact matrices are acceptable. Collapse the rest into one line each ("N/A: managed-only, no lifecycle change").
- **Medium and High:** all sections. High tier also needs explicit spikes and release gates.

## Structure

1. **Verdict and confidence.** One verdict, the risk tier, the main reason, the biggest blockers or conditions, and the next concrete action. Say whether you accept the requested *outcome*, the requested *implementation*, or both.
2. **Customer problem and success criteria.** Who needs it, the current workaround, the desired behavior, assumptions, non-goals, and the cost of doing nothing.
3. **Current SDK behavior and evidence.** Branch, commit, and release tag; relevant APIs and paths; the actual call flow; current limitations; conflicts between sources. Label evidence: verified in code, verified by test/build, documented, external, inferred, unknown.
4. **Options and trade-offs.** Status quo, minimal coherent change, and an alternative, with concrete costs and risks and why the preferred one wins.
5. **Proposed API and customer experience.** Current usage, proposed common and advanced usage (mark new symbols **(proposed)**), semantics, defaults, errors, lifecycle, capabilities, compatibility delta, and migration if any.
6. **Platform and Unity compatibility.** Both matrices below, feature-specific limits, and the future-backend assessment.
7. **Recommended internal design.** Files and symbols to change, data flow and ownership, threading, teardown, dependencies, native or build integration, fallback, and backend coordination.
8. **Feature interactions and regressions.** Only meaningful interactions, and explicitly what happens for customers who never enable the feature.
9. **Risk register.** Ranked by severity and likelihood: trigger, affected configurations, impact, mitigation, residual risk, and evidence or test.
10. **Verification and delivery.** Acceptance criteria, targeted tests, what CI covers vs what needs manual or device work, phases, effort range with assumptions, docs and CHANGELOG, rollback.
11. **Decision gates and open questions.** The gate table, blockers, unknowns with the cheapest way to resolve each, and revisit criteria.
12. **Customer-facing response.** A short reply covering what is possible now, limitations, alternatives, and direction. Promise nothing that is not approved.

## Platform matrix

| Platform / variant | Existing SDK support | Feature feasibility (import / build / run) | Required changes or limitation | Evidence and validation |
|---|---|---|---|---|
| Android ARM64 | | | | |
| iOS (device) | | | | |
| Windows x64 | | | | |
| macOS Intel / Apple Silicon | | | | |
| Linux (documented Ubuntu) | | | | |
| Unity Editor (Windows / macOS / Linux hosts) | | | | |
| WebGL / Web | Unsupported (documented) | | | |
| UWP | Unsupported (documented) | | | |
| visionOS, standalone XR, PC VR | Not supported | | | |
| PlayStation / Xbox / Nintendo | Not supported | | | |

Group unsupported targets into one row unless the request targets them. Split supported rows when constraints differ.

## Unity version matrix

| Editor | Commitment | Feature, API, and dependency compatibility | Build and runtime risks | Validation status |
|---|---|---|---|---|
| 2021.3 (oldest; note the 2021.3.36 vs 2021.3.41+ Android toolchain split) | Minimum (`package.json`) | | | |
| 2022.3 | Documented | | | |
| 2023.1 / 2023.2 | Docs list 2023.1; CI tests 2023.2 | | | |
| Unity 6 (6000.0, plus 6000.1 and 6000.2 in CI) | Documented as "Unity 6" | | | |
| Latest stable (only if relevant) | Target, not a commitment | | | |

Refresh the rows from `repo-baseline.md` and record drift. Validation status uses: verified (with command and environment), planned, unavailable, failed, or excluded.

## Gate table

| Gate | Status | Evidence or condition |
|---|---|---|
| Product fit | | |
| Public API | | |
| Current platforms | | |
| Unity compatibility | | |
| Future extensibility | | |
| Package and host | | |
| Runtime and media | | |
| Performance | | |
| Security and privacy | | |
| Verification and delivery | | |

Overall confidence: high, medium, or low, with the reason.

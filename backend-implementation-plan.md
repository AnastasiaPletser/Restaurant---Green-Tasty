# Backend Implementation Plan

## Source requirement
Create or extend the requested backend Lambda feature, then add unit tests and perform a code review.

## Status
Waiting for a concrete feature or change request.

## Objectives
- Translate the backend requirement into a bounded implementation scope.
- Implement the required Lambda behavior in `backend/restaurant-backend-app/`.
- Add focused unit tests in `backend/restaurant-backend-app/Tests/`.
- Run targeted build/test validation.
- Review the completed implementation and disposition all actionable findings.

## Out of scope
- Unrelated production refactors.
- Generated `bin/`, `obj/`, or build artifacts.
- Secrets or live AWS resources.
- Frontend or infrastructure changes unless required by the accepted requirement.

## Workflow

| Stage | Owner | Entry criteria | Deliverables | Exit criteria |
|---|---|---|---|---|
| 1. Scope | Backend Implementation Orchestrator | Concrete requirement provided | Confirmed target, contracts, affected scope | Plan updated with acceptance criteria |
| 2. Implement | AWS Lambda Implementer | Target and contracts identified | Focused production/configuration changes | Implementation complete; build/test evidence reviewed |
| 3. Test | Restaurant Unit Test Writer | Implementation available | Focused unit tests | Relevant tests pass and defects are reported |
| 4. Review | Restaurant Backend Code Reviewer | Implementation + tests available | Ordered findings or `No findings` | Findings are fixed or explicitly dispositioned |
| 5. Verify | Backend Implementation Orchestrator | Corrections and review complete | Updated plan and final report | Acceptance criteria mapped to evidence |

## Acceptance checklist
- [ ] The concrete requirement and affected Lambda/API/event contract are documented.
- [ ] Production changes are limited to the accepted scope.
- [ ] Unit tests cover relevant success, validation, failure, and boundary behaviors.
- [ ] Targeted build and test commands pass.
- [ ] No secrets or live-resource assumptions are introduced.
- [ ] The code reviewer reports no unresolved actionable defects.
- [ ] Changed files and validation evidence are recorded below.

## Progress

| Stage | Status | Evidence |
|---|---|---|
| Scope | Pending concrete feature | Source requirement is a workflow instruction, not a Lambda behavior requirement |
| Implement | Pending | — |
| Test | Pending | — |
| Review | Pending | — |
| Verify | Pending | — |

## Delegation brief template

```text
Requirement: <specific behavior>
Repository scope: backend/restaurant-backend-app
Plan: backend-implementation-plan.md
Target: <Lambda/project/routes/services/contracts>
Constraints: <input/output compatibility, AWS resources, security, out of scope>
Acceptance criteria: <observable behavior>
Validation: <exact focused commands>
Required report: changed files, result, commands, failures, and blockers
```

## Final validation and results
_To be completed after delegation._

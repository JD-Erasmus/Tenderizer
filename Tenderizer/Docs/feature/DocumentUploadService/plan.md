# Document Upload Service Plan

## Purpose
Evolve the existing tender upload flow into a generic document upload pipeline that routes handling rules by document type while keeping endpoints thin and reusable.

## Current State (Already Implemented)
- Tender uploads are currently supported through `TenderDocumentsController` + `ITenderDocumentService`.
- `TenderDocumentService` already performs authorization checks, storage via `IPrivateFileStore`, and persistence to `TenderDocument`.
- Existing UI/endpoints are working and should remain stable during migration.

## Current Schema Alignment Snapshot
| Area | Current implementation | Alignment to target model | Gap / action |
|---|---|---|---|
| Physical file layer | `StoredFile` used by `TenderDocument` and `LibraryDocumentVersion` | ? aligned | Keep as shared physical blob abstraction |
| Reusable document identity | `LibraryDocument` + `LibraryDocumentVersion` with version history | ? aligned | Add explicit library type classification for CV |
| Tender-owned submission artifacts | `TenderDocument` linked to `Tender` and optional `LibraryDocumentVersionId` | ? aligned | Keep ownership on tender side; preserve optional reuse link |
| CV handling | CV classification on `LibraryDocument.Type` | ? aligned | Keep CV classification in reusable library model |
| Checklist evidence ownership | `ChecklistDocument` links tender + checklist + file (+ optional library source) | ? aligned | Keep explicit tender-owned evidence aggregate |
| Checklist linkage integrity | Explicit checklist evidence relationships and foreign keys | ? aligned | Keep `ChecklistDocument` as active write/read path |
| Upload orchestration | Tender-specific upload service + DTOs | ?? partially aligned | Introduce generic orchestrator with routed typed metadata contracts |

### Baseline Constraints
- Current tender upload behavior is a reference baseline, but breaking changes are acceptable during model alignment.
- Prefer additive-first migration where practical, but direct breaking refactors are allowed in this phase.
- Endpoint and UX compatibility are preferred, not mandatory, while converging to the target model.

## Goals
- Provide one internal upload orchestration entry point for supported document types.
- Route validation, metadata mapping, and persistence behavior by document type.
- Reuse existing file storage abstraction (`IPrivateFileStore`) and existing domain services.
- Keep service logic testable and independent of MVC concerns.
- Preserve current tender upload behavior while introducing the new abstraction.

## Non-Goals
- No EF Core migration changes.
- No changes to the physical storage provider contract.
- No UI redesign in this phase.
- No controller route changes in this phase.

## Proposed Routing Model
Use a strategy/router pattern:

- `IDocumentUploadService` (generic orchestration)
- `IDocumentUploadRoute` (per-document-type handler)
- `DocumentUploadRouter` (maps `DocumentType` => route implementation)

Suggested enum:
- `TenderDocument`
- `LibraryDocument`
- `ChecklistEvidence`

Type hierarchy clarification:
- `TenderDocument` = the main tender pack document (RFP/RFQ or equivalent core tender file).
- `LibraryDocument` = reusable shared document.
- CV documents are modeled as a **library document type classification** (not a top-level document type).
- `ChecklistEvidence` = tender-owned evidence document attached to checklist items.
- `ChecklistEvidence` may be uploaded directly or linked from an existing library document.

Initial delivery scope:
- Implement `TenderDocument` route first using current `TenderDocumentService` logic as reference.
- Add `LibraryDocument` route only if required by active scenario parity.
- Keep other enum values defined but not necessarily fully implemented in phase 1.

## Target Entity Contracts (Canonical)
### `LibraryDocument`
- Represents reusable business identity.
- Add `Type` classification (e.g., `Cv`, `Certificate`, `Policy`, `Template`).
- Remains independent from tender ownership.

### `LibraryDocumentVersion`
- Represents immutable reusable revision.
- References `StoredFile`.
- Can be referenced by both `TenderDocument` and `ChecklistDocument` as optional reuse source.

### `TenderDocument`
- Represents tender-owned submission artifact.
- References `Tender` + `StoredFile`.
- Optional `LibraryDocumentVersionId` indicates source reuse only, not ownership transfer.
- Remove CV-specific behavior from tender aggregate over time.

### `ChecklistDocument` (new aggregate)
- Represents checklist evidence owned by a tender.
- References `Tender` + `ChecklistItem` + `StoredFile`.
- Optional `LibraryDocumentVersionId` supports evidence sourced from library.
- Replaces indirect checklist-to-document linkage pattern.

### `ChecklistItem`
- Represents requirement/question only.
- Should reference checklist evidence through explicit relationship.
- Does not own file metadata directly.

## Request/Response Contracts
### Upload request
- `DocumentType`
- `OwnerId` (tender id, library id, or equivalent)
- `UploadedByUserId`
- Optional `IFormFile File` (required for direct uploads; omitted for library-linked checklist evidence)
- Generic metadata payload (route-specific DTO serialized in outer request)

Typed metadata contract rule:
- Keep the outer upload request generic.
- Each route must define and own a strongly typed metadata contract.
- Generic service must deserialize/validate metadata into the route contract before route execution.
- No untyped dictionary access inside route implementations.

Metadata expectations by type:
- `TenderDocument` metadata contract includes tender-specific fields (e.g., `Category`, `DisplayName`, optional `ChecklistItemId`).
- `LibraryDocument` metadata contract includes reusable `Type` classification (e.g., `Type = Cv`) and library-specific fields.
- `ChecklistEvidence` metadata contract includes checklist linkage fields, including optional `LibraryDocumentVersionId` when evidence is sourced from library.

### Upload result
- Success flag
- Stored file id/path key
- Domain document id
- Validation errors (structured)
- Optional error code/message for controller mapping

## Processing Flow
1. Existing endpoint submits a generic request DTO to `IDocumentUploadService`.
2. Service performs baseline validation (file present, filename sanitization, size/content checks).
3. Service resolves route handler from router by `DocumentType`.
4. Route performs business-rule validation (category/metadata/owner assumptions).
5. Route stores file through `IPrivateFileStore`.
6. Route persists domain record via target domain service/repository.
7. Service returns normalized result for endpoint/view model mapping.

## Route Responsibilities
Each route should define:
- Allowed extensions and max size.
- A typed metadata DTO + metadata validation rules and defaults.
- Domain entity creation mapping.
- Authorization assumptions for owner id.
- Post-upload actions (if any).

Tender route specifics (phase 1):
- Map from generic metadata to tender fields (`Category`, `DisplayName`).
- Create `ChecklistDocument` evidence and mark checklist completion where `ChecklistItemId` is provided.
- Preserve current authorization semantics (owner/assignment/admin).

Library route specifics (when enabled):
- Handle reusable document persistence and versioning behavior.
- Handle CV as a library `Type` classification rather than a separate top-level route.

Checklist evidence specifics (when enabled):
- Treat checklist evidence as tender-owned child documents.
- Support either direct file upload or attach/link from existing library document version.

## Validation and Security
- Centralized baseline checks in generic service:
  - non-empty file
  - sanitized filename
  - content type allowlist fallback to extension checks
- Centralized metadata envelope checks in generic service:
  - metadata payload present when required by route
  - payload deserializes into route metadata DTO
  - schema/shape errors returned as structured validation failures
- Route-specific checks for business rules.
- Ensure owner-level authorization before storage write.
- Log upload attempts and failures with correlation data.

## Error Handling
- Return structured validation failures (no exceptions for expected errors).
- Throw only for unexpected infrastructure faults.
- Keep user-facing messages generic; keep detailed diagnostics in logs.
- Keep parity with current tender controller UX (`TempData` success/error messages).

## Migration Strategy (Early Development Mode)
- Controlled breaking changes are acceptable in this phase.
- Legacy data backfill is optional and can be skipped.
- Prefer model convergence and code simplification over temporary compatibility.
- Keep endpoint routes stable where practical while completing internal cleanup.

## Implementation Steps
1. Add `DocumentType` enum and generic request/result DTOs.
2. Define `IDocumentUploadService` and `IDocumentUploadRoute<TMetadata>` interfaces.
3. Implement `DocumentUploadRouter` for type-to-route resolution.
4. Add typed metadata DTOs for each route (`TenderDocumentUploadMetadata`, `LibraryDocumentUploadMetadata`, `ChecklistEvidenceUploadMetadata`).
5. Implement `TenderDocumentUploadRoute` first, mirroring current tender behavior.
6. Add generic `DocumentUploadService` orchestration with baseline validation and metadata deserialization/validation.
7. Register route metadata bindings and service/router/routes in DI (`Program.cs`).
8. Refactor tender upload endpoint to call the generic service behind the same route.
9. Verify parity against current tender upload behavior (success, validation, authorization).
10. Add/adjust unit tests for router resolution, baseline validation, metadata binding, and route success/failure.
11. Add integration test for tender route end-to-end.
12. Introduce `LibraryDocument` route with type classification support (including CV classification).
13. Introduce `ChecklistEvidence` route for tender-child checklist documents with optional library linkage.

## Testing Plan
- Unit tests:
  - unknown type => route not found error
  - baseline invalid file => validation failure
  - invalid metadata payload shape => validation failure
  - metadata payload deserializes into expected route DTO
  - route success => normalized success result
  - route failure => normalized failure result
  - tender route maps metadata to domain entity correctly
  - library route maps CV type classification correctly
  - checklist route accepts library-linked evidence metadata
- Integration tests:
  - tender upload persists stored file + `TenderDocument` record
  - authorization failure blocks upload before storage write
  - checklist completion still occurs when checklist id is supplied
  - checklist evidence can be attached from an existing library document

## Rollout Notes
- Start with currently active tender upload scenario.
- Prioritize convergence to the target ownership/reuse/evidence model over temporary compatibility.
- Legacy tender-specific paths can be removed once replacement paths compile and tests pass.
- Expect controlled breaking changes during refactor; stabilize after model alignment milestones.
- Expand to additional document types incrementally after tender route stabilizes.

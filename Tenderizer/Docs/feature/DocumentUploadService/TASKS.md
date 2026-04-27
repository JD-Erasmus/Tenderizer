# Document Upload Service Task List

This checklist is derived from `Docs/feature/DocumentUploadService/plan.md` and aligned to `Docs/feature/DocumentUploadService/DocumentModel.md`.

## Working assumptions
- Controlled breaking changes are allowed during this refactor phase.
- Convergence to the ownership/reuse/evidence model is prioritized over short-term compatibility.
- Endpoint route stability is preferred but not mandatory.

## 1) Domain model alignment
- [x] Add library document classification (`Type`) to support CV as a library classification.
- [x] Introduce `ChecklistDocument` aggregate (tender-owned evidence).
- [x] Add `ChecklistDocument` relationships: `Tender`, `ChecklistItem`, `StoredFile`, optional `LibraryDocumentVersion`.
- [x] Add explicit `ChecklistItem` navigation(s) to checklist evidence.
- [x] Mark tender-side CV specialization (`TenderDocumentCategory.Cv`, `TenderDocumentCvMetadata`) for deprecation.

## 2) Persistence and mappings
- [x] Update `ApplicationDbContext` mappings for new entities and FKs.
- [x] Add indexes for expected checklist evidence lookup paths.
- [x] Ensure delete behaviors reflect ownership boundaries (tender-owned evidence cascades with tender).
- [x] Preserve optional reuse links to `LibraryDocumentVersion` with restrict semantics.

## 3) Upload contracts and routing
- [x] Add `DocumentType` enum (`TenderDocument`, `LibraryDocument`, `ChecklistEvidence`).
- [x] Add generic upload request/result contracts.
- [x] Add typed metadata contracts:
  - [x] `TenderDocumentUploadMetadata`
  - [x] `LibraryDocumentUploadMetadata`
  - [x] `ChecklistEvidenceUploadMetadata`
- [x] Define `IDocumentUploadService` and `IDocumentUploadRoute<TMetadata>`.
- [x] Implement `DocumentUploadRouter` for document type -> route resolution.

## 4) Route implementations
- [x] Implement `TenderDocumentUploadRoute` with current tender behavior parity as baseline.
- [x] Implement `LibraryDocumentUploadRoute` with type handling (including CV classification).
- [x] Implement `ChecklistEvidenceUploadRoute` for direct upload and optional library-sourced evidence.
- [x] Centralize baseline upload validation (file presence, filename sanitization, content checks).
- [x] Centralize metadata envelope validation and route DTO deserialization.

## 5) Service and endpoint integration
- [x] Implement generic `DocumentUploadService` orchestration.
- [x] Register routes/router/service in DI (`Program.cs`).
- [x] Refactor tender upload flow to use generic service.
- [x] Refactor checklist evidence flow to use `ChecklistDocument`.
- [x] Remove legacy checklist indirect linkage path from active write flow.

## 6) Data transition
- [ ] Skipped in early development mode: Backfill legacy checklist linkage records into `ChecklistDocument`.
- [ ] Skipped in early development mode: Backfill/normalize CV records into library classification where required.
- [ ] Skipped in early development mode: Run validation checks for ownership and reuse-link correctness.

## 7) Cleanup
- [x] Remove legacy tender CV special handling once replacement behavior is verified.
- [x] Remove `ChecklistItem.UploadedTenderDocumentId` after full cutover.
- [x] Remove dead route logic and unused DTOs.

## 8) Testing
- [x] Unit: unknown type -> route resolution failure.
- [x] Unit: invalid metadata payload -> validation failure.
- [x] Unit: route metadata binding/deserialization success.
- [x] Unit: tender route mapping parity.
- [x] Unit: library route CV type mapping.
- [x] Unit: checklist route supports library-linked evidence.
- [x] Integration: checklist evidence persists with correct ownership.
- [x] Integration: tender upload + optional checklist completion behavior.
- [x] Integration: authorization prevents invalid owner-context uploads.

## 9) Documentation
- [x] Update `Docs/Domain-Model.md` with `ChecklistDocument` and library CV classification.
- [x] Update `Docs/Architecture.md` upload flow and ownership boundaries.
- [x] Keep `plan.md` and `DocumentModel.md` aligned with implementation decisions.

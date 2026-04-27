# Document Upload Service Task List

This checklist is derived from `Docs/feature/DocumentUploadService/plan.md` and aligned to `Docs/feature/DocumentUploadService/DocumentModel.md`.

## Working assumptions
- Controlled breaking changes are allowed during this refactor phase.
- Convergence to the ownership/reuse/evidence model is prioritized over short-term compatibility.
- Endpoint route stability is preferred but not mandatory.

## 1) Domain model alignment
- [ ] Add library document classification (`Type`/`Subtype`) to support CV as a library classification.
- [ ] Introduce `ChecklistDocument` aggregate (tender-owned evidence).
- [ ] Add `ChecklistDocument` relationships: `Tender`, `ChecklistItem`, `StoredFile`, optional `LibraryDocumentVersion`.
- [ ] Add explicit `ChecklistItem` navigation(s) to checklist evidence.
- [ ] Mark tender-side CV specialization (`TenderDocumentCategory.Cv`, `TenderDocumentCvMetadata`) for deprecation.

## 2) Persistence and mappings
- [ ] Update `ApplicationDbContext` mappings for new entities and FKs.
- [ ] Add indexes for expected checklist evidence lookup paths.
- [ ] Ensure delete behaviors reflect ownership boundaries (tender-owned evidence cascades with tender).
- [ ] Preserve optional reuse links to `LibraryDocumentVersion` with restrict semantics.

## 3) Upload contracts and routing
- [ ] Add `DocumentType` enum (`TenderDocument`, `LibraryDocument`, `ChecklistEvidence`).
- [ ] Add generic upload request/result contracts.
- [ ] Add typed metadata contracts:
  - [ ] `TenderDocumentUploadMetadata`
  - [ ] `LibraryDocumentUploadMetadata`
  - [ ] `ChecklistEvidenceUploadMetadata`
- [ ] Define `IDocumentUploadService` and `IDocumentUploadRoute<TMetadata>`.
- [ ] Implement `DocumentUploadRouter` for document type -> route resolution.

## 4) Route implementations
- [ ] Implement `TenderDocumentUploadRoute` with current tender behavior parity as baseline.
- [ ] Implement `LibraryDocumentUploadRoute` with subtype handling (including CV classification).
- [ ] Implement `ChecklistEvidenceUploadRoute` for direct upload and optional library-sourced evidence.
- [ ] Centralize baseline upload validation (file presence, filename sanitization, content checks).
- [ ] Centralize metadata envelope validation and route DTO deserialization.

## 5) Service and endpoint integration
- [ ] Implement generic `DocumentUploadService` orchestration.
- [ ] Register routes/router/service in DI (`Program.cs`).
- [ ] Refactor tender upload flow to use generic service.
- [ ] Refactor checklist evidence flow to use `ChecklistDocument`.
- [ ] Remove legacy checklist indirect linkage path from active write flow.

## 6) Data transition
- [ ] Backfill legacy checklist linkage records into `ChecklistDocument`.
- [ ] Backfill/normalize CV records into library classification where required.
- [ ] Run validation checks for ownership and reuse-link correctness.

## 7) Cleanup
- [ ] Remove legacy tender CV special handling once replacement behavior is verified.
- [ ] Remove `ChecklistItem.UploadedTenderDocumentId` after full cutover.
- [ ] Remove dead route logic and unused DTOs.

## 8) Testing
- [ ] Unit: unknown type -> route resolution failure.
- [ ] Unit: invalid metadata payload -> validation failure.
- [ ] Unit: route metadata binding/deserialization success.
- [ ] Unit: tender route mapping parity.
- [ ] Unit: library route CV subtype mapping.
- [ ] Unit: checklist route supports library-linked evidence.
- [ ] Integration: checklist evidence persists with correct ownership.
- [ ] Integration: tender upload + optional checklist completion behavior.
- [ ] Integration: authorization prevents invalid owner-context uploads.

## 9) Documentation
- [ ] Update `Docs/Domain-Model.md` with `ChecklistDocument` and library CV classification.
- [ ] Update `Docs/Architecture.md` upload flow and ownership boundaries.
- [ ] Keep `plan.md` and `DocumentModel.md` aligned with implementation decisions.

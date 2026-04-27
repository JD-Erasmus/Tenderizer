# Document Upload Domain Model

## Core Idea

You are not modeling “documents” as one generic concept.

You are modeling:

- **Ownership**
- **Reuse**
- **Evidence**

Everything else is implementation detail.

---

## 1) `StoredFile` = Physical Reality

`StoredFile` is the lowest layer.

### Meaning

> “A binary file exists somewhere.”

It has no business meaning and is reused by higher-level aggregates.

### Relationships

- `LibraryDocumentVersion -> StoredFile`
- `TenderDocument -> StoredFile`
- `ChecklistDocument -> StoredFile`

### Rule

`StoredFile` must never know **why** it exists.

---

## 2) Library = Reusable Knowledge Base

The library is the central reuse system.

### 2.1) `LibraryDocument`

### Meaning

> “A reusable business document concept.”

Examples:

- CV
- Certificate
- Policy
- Template

It answers: **what is this document in business terms?**

### 2.2) `LibraryDocumentVersion`

### Meaning

> “A specific revision of a reusable document.”

Each version:

- Points to `StoredFile`
- Is immutable once created (preferred)
- Supports history and auditability

### Rule

- `LibraryDocument` defines identity
- `LibraryDocumentVersion` defines revision reality

### 2.3) CV is NOT Special Infrastructure

CV is just classification:

- `LibraryDocument.Type = CV`

This means:

- Not a separate table
- Not a tender concern
- Not duplicated per tender

Consequence: CVs live once and are reused.

---

## 3) Tender = Transactional Context

`Tender` is not a document type.

It is a workspace/container for a submission and owns two document concerns.

### 3.1) `TenderDocument` = Submission Artifacts

### Meaning

> “Files that belong to this specific tender submission.”

Examples:

- Proposal PDF
- Pricing sheet
- Tender-specific supporting files

### Key Property

These artifacts are **not reusable outside the tender**.

### Optional Reuse Link

- `LibraryDocumentVersionId` (nullable)

Meaning:

> “This tender document originated from a reusable library document version.”

Reuse is tracked, but ownership remains tender-side.

### 3.2) `ChecklistDocument` = Evidence Layer

### Meaning

> “Proof that a checklist requirement is satisfied.”

Belongs to:

- `Tender`
- `ChecklistItem`

Optional source:

- `LibraryDocumentVersion`

Evidence can be:

- **Case A**: Uploaded directly (tender-specific)
- **Case B**: Linked from library (reused compliance artifact)

### Rule

`ChecklistDocument` is always tender-owned, even when sourced from library.

---

## 4) `ChecklistItem` = Requirement, Not Document

### Meaning

> “A requirement that must be satisfied.”

`ChecklistItem` does not own files. It references `ChecklistDocument` as evidence.

- Checklist item = question
- Checklist document = answer evidence

---

## 5) Canonical Relationship Graph

```text
StoredFile
   ↑
   ├── LibraryDocumentVersion ── LibraryDocument (CV classified here)
   │
   ├── TenderDocument ─────────── Tender
   │
   └── ChecklistDocument ──────── Tender + ChecklistItem
```

Optional reuse links:

- `TenderDocument -> LibraryDocumentVersion` (optional)
- `ChecklistDocument -> LibraryDocumentVersion` (optional)

---

## 6) Modeling Rules

### Rule 1: Ownership is NOT Reuse

A library source does not change ownership.

Example:

- `ChecklistDocument` sourced from CV still belongs to `Tender`.

Reuse is metadata, not ownership.

### Rule 2: Library is the Only Reusable Aggregate

Reusable entities are only:

- `LibraryDocument`
- `LibraryDocumentVersion`

Everything else is contextual.

### Rule 3: Tender is Always Transactional

`Tender` never owns reusable identity. It owns:

- Attachments
- Evidence
- Submission state

### Rule 4: CV is Not a Special Workflow

CV is classification:

- `LibraryDocument(Type = CV)`

No tender-specific CV table/workflow in the clean model.

### Rule 5: Checklist is Evidence-Driven

Checklist logic should not care where a file came from.
It should only care whether valid evidence is attached.

---

## 7) What This Fixes

### Before

- `TenderDocumentCategory` explosion
- CV-specific tender-side tables
- Library/tender semantic mixing
- Checklist indirect FK workarounds
- Enum-driven polymorphic behavior

### After

- Clear ownership boundaries
- Reusable library system
- Clean checklist evidence model
- Predictable upload routing
- Reduced category-driven conditional logic

---

## 8) Upload Service Mapping

### Library Route

Creates:

- `LibraryDocument`
- `LibraryDocumentVersion`

### Tender Route

Creates:

- `TenderDocument`

Optional:

- `LibraryDocumentVersionId`

### Checklist Route

Creates:

- `ChecklistDocument`

Optional:

- `LibraryDocumentVersionId`

Each route should focus only on its aggregate.

---

## 9) Key Insight

The architecture is not:

> “documents with types”

It is:

> “ownership domains with optional reuse links”

---

## Final Mental Model

- **Library** = reusable truth
- **Tender** = transactional context
- **Checklist** = proof mechanism
- **StoredFile** = physical blob
- **Reuse** = link, not ownership

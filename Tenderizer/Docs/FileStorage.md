# File Storage

## What This Adds

- A private file store that writes uploaded documents outside `wwwroot`.
- Immutable file records in the database via `StoredFile`.
- Reusable `LibraryDocument` records with immutable `LibraryDocumentVersion` rows.
- `TenderDocument` attachments that pin the exact `StoredFileId` used at attachment time.
- Optional upload of the Tender / RFP source document during tender creation.

## Configuration

Add or update `DocumentStorage` in configuration:

```json
"DocumentStorage": {
  "PrivateRootFolder": "App_Data/Documents",
  "MaxFileSizeMb": 25,
  "AllowedExtensions": [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"]
}
```

## Current Tradeoff

The code is implemented, but the database tables still need a migration created by your team because this repository explicitly avoids generating migrations in this workflow.

## Security Boundary

- Files are stored under a private root outside public directories.
- Downloads are served only through authenticated controller actions.
- Tender document access is limited to the tender owner or an admin.
- Reusable library document management is restricted to admins.

## Current UI Flow

- `Library Documents` list page for browsing reusable documents
- `Library Documents / Create` page for new reusable documents
- `Library Documents / Details` page for version history and version uploads
- `Tender Create` page with optional Tender / RFP document upload
- `Tender Documents` page for direct uploads and attaching reusable library document versions

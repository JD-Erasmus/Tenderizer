Release notes
=============

Unreleased
----------

Tender workflow
---------------

- Added collaborative tender workflow stages (`Draft`, `Identified`, `InProgress`) across tender service flows.
- Added tender assignments so owners and admins can assign participating users.
- Added generated and user-managed checklist items with document-linked completion (`UploadedTenderDocumentId`).
- Integrated checklist completion with tender document uploads, including optional checklist-item association during upload.
- Removed checklist upload locking and simplified the upload flow by eliminating lock/unlock interactions.
- Unified tender details UX with an at-a-glance tender summary and attached-documents snapshot.
- Kept `TenderDocuments/Index` as the primary checklist-linked upload and document management experience.
- Added assignment and status-change notification orchestration through `INotificationService`.
- Added service-level automated tests for checklist behavior and tender status-transition notifications.

Migration and rollout notes
---------------------------

- EF migration artifacts for checklist and assignment schema changes must be created and applied by repository maintainers.
- No new identity or domain seed data is required specifically for this feature beyond existing app configuration.

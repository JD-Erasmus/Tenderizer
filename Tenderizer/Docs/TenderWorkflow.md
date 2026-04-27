Tender workflow
===============

Overview
--------
This document describes the new "Tender workflow" feature: status transitions, team assignments, checklist generation, and document upload responsibilities.

New statuses
------------
- `Draft`: initial state when a tender is created. Basic metadata (title, description) and optional Tender/RFP document may be uploaded.
- `Identified`: team members are assigned to the tender. Identified triggers checklist generation of required documents.
- `InProgress`: assigned team members work through the checklist and upload documents.
- `Submitted`: checklist is finished and the tender has moved to submission.

Data model changes
------------------
- `Tender` (existing) additions:
  - `Status` (existing) will include `Identified` and `InProgress` values in `TenderStatus` enum.
  - `AssignedUserIds` (new): collection of user ids assigned to the tender (many-to-many). Implement with join table `TenderAssignment`.
  - `ChecklistGeneratedAt` (new, nullable DateTime): timestamp when checklist was generated.

- New entity: `ChecklistItem`
  - `Id` (int)
  - `TenderId` (fk)
  - `Title` (string)
  - `Description` (string)
  - `Required` (bool)
  - `IsCompleted` (bool)
  - `UploadedTenderDocumentId` (nullable fk to `TenderDocument` or `StoredFile`)
  - `CreatedAt` (DateTime)

- New entity: `TenderAssignment` (join table)
  - `TenderId` (fk)
  - `UserId` (fk)
  - `AssignedAt` (DateTime)

Checklist generation
--------------------
- When a tender is transitioned from `Draft` to `Identified`, the system will generate checklist items based on a configurable template or default set (e.g., `RFP Document`, `Budget`, `CVs`, `Technical Specs`).
- Checklist items are persisted as `ChecklistItem` rows tied to the tender.
 - Assigned users may add additional checklist items while the tender is in the `Identified` stage; these user-added items become part of the persisted checklist and are tracked the same as template items.

Default code template
---------------------
 - Most tenders follow a common pattern, so provide a default, read-only template in configuration or code that `ChecklistService` uses when no explicit template is selected.
 - Example `appsettings.json` fragment:

```
"ChecklistTemplates": [
  {
    "Name": "Default",
    "Items": [
      { "Title": "RFP Document", "Required": true },
      { "Title": "Budget", "Required": true },
      { "Title": "CVs", "Required": false },
      { "Title": "Technical Specifications", "Required": true }
    ]
  }
]
```

 - POCO used to bind the config:

```
public class ChecklistTemplateConfig {
  public string Name { get; set; }
  public List<ChecklistTemplateItemConfig> Items { get; set; }
}
public class ChecklistTemplateItemConfig { public string Title { get; set; } public bool Required { get; set; } }
```

 - `ChecklistService.GenerateChecklistAsync` reads the configured `Default` template, maps items to `ChecklistItem` entities, persists them, and sets `ChecklistGeneratedAt`.

 - If later we add editable templates, the default config provides a sensible fallback.

Checklist-linked uploads
------------------------
- Checklist items are completed by linking uploaded tender documents directly to `ChecklistItem.UploadedTenderDocumentId`.
- Users can upload documents from the tender documents page and optionally associate each upload to a checklist item in the same form.
- Server-side validation ensures checklist item and tender ownership/assignment rules are still enforced.

Service changes
---------------
- `ITenderService` / `TenderService`:
  - Add methods to assign users to a tender and to transition status.
  - Add methods to assign users to a tender and to transition status.

- `IChecklistService` / `ChecklistService` (new):
  - Add method `GenerateChecklistAsync(Guid tenderId)` that creates checklist items and sets `ChecklistGeneratedAt`.
  - Add methods to query checklist items for a tender and to mark checklist items completed when a document is uploaded or attached.

- `TenderDocumentService`:
  - Permit uploads by assigned users when tender status is `Identified` or `InProgress`.
  - On upload, optionally mark corresponding `ChecklistItem.IsCompleted` and link the uploaded document.

UI changes
----------
- `Tenders/Create` and `Tenders/Edit` pages: allow admins/owners to set assigned users and change status.
- `Tenders/Details` page: show a quick tender summary and attached-documents section for at-a-glance context.
- `TenderDocuments/Index` page: primary checklist-linked upload experience, where uploads can be associated to checklist items.

APIs and background tasks
-------------------------
- No new background tasks required initially. ReminderScheduler could later use checklist completion to trigger reminders.
Notifications
-------------
- Assigned users should receive an email when they are assigned to a tender.
- Assigned users and the tender owner should receive an email when the tender status changes (notably: `Draft` -> `Identified`, `Identified` -> `InProgress`, and `InProgress` -> `Submitted`).
- Notifications should be sent asynchronously and retried on transient failures. The existing `IEmailSender` / `SmtpEmailSender` pipeline may be reused; consider a small `INotificationService`/`NotificationService` to format templates and orchestrate sends.
- Email templates should include the tender name, status, assigned user list, and a link to the tender details page.

Migration notes
---------------
- Add EF Core migrations for new entities and columns.
- Seed mapping for existing `TenderStatus` enum to include new values.

Security and authorization
--------------------------
- Only admins and tender owners can assign users and change certain statuses (e.g., to `Submitted`).
- Assigned users may upload documents and mark checklist items as completed.
 - Assigned users may add checklist items while the tender is in the `Identified` status and may upload documents and mark checklist items as completed.
 - Any user assigned to the tender may edit or remove checklist items after generation (for example to correct titles, change required flags, or remove items no longer relevant). Owners and admins retain override rights and may edit or remove items regardless of assignment.

Open questions
--------------



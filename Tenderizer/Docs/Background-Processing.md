Background Processing
=====================

Tenderizer sends reminder emails with two components: `ReminderScheduler` and `TenderReminderWorker`.

ReminderScheduler
-----------------

`ReminderScheduler` runs during tender create and update flows.

Its behavior is:

1. Load the tender id, closing time, and status.
2. Delete all pending reminders for that tender.
3. Stop if the tender is in a terminal status.
4. Stop if the tender status is not reminder-active.
5. Compute reminder timestamps from configured offsets.
6. Keep only timestamps that are still in the future.
7. Insert missing reminder rows.

Active statuses:

- `Draft`
- `Identified`
- `InProgress`

Terminal statuses:

- `Submitted`
- `Won`
- `Lost`
- `Cancelled`

Offset configuration
--------------------

The scheduler reads `ReminderOffsetsMinutes` from configuration.

Example:

```json
{
  "ReminderOffsetsMinutes": [10080, 4320, 1440]
}
```

If the setting is missing, the defaults are:

- 7 days before closing
- 3 days before closing
- 24 hours before closing

Worker loop
-----------

`TenderReminderWorker` is a hosted `BackgroundService`.

Runtime behavior:

- Wake every 60 seconds
- Query for due reminders where:
  - `SentAtUtc == null`
  - `ReminderAtUtc <= now`
  - The related tender is not terminal
- Skip reminders that have already failed 5 times
- Load the owner email from ASP.NET Identity
- Send the email through `IEmailSender`

Failure handling
----------------

On success:

- `SentAtUtc` is set
- `LastError` is cleared

On failure:

- `AttemptCount` is incremented
- `LastError` is truncated to 500 chars
- `ReminderAtUtc` is moved to 10 minutes in the future

After each pass, the worker deletes any pending reminders that belong to terminal tenders.

Operational caveat
------------------

The worker does not acquire a distributed lock before sending. If multiple web instances run against the same database, they can process the same due reminder concurrently and send duplicates.

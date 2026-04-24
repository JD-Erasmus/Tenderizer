# UI Instructions (Bootstrap-only)

These UI guidelines define Tenderizer’s UI system. The goal is **calm, dense, text-first interfaces** inspired by **DecimalChain** and **Reflect**: minimal chrome, fast scanning, zero decoration.

Framework constraint: **Bootstrap only**. No Tailwind. No custom CSS unless explicitly approved.

---

## Design Principles (Non‑negotiable)

* Text > containers > color. In that order.
* Density beats whitespace. Reduce vertical waste.
* UI should feel like a serious internal tool, not a marketing site.
* Repetition is a feature. Same patterns everywhere.
* If Bootstrap can do it, do not reinvent it.

---

## Layout

### Page width

* Always use `container`.
* Never use `container-fluid` for content.

### Vertical rhythm (tight by default)

Inspired by Reflect’s dense lists.

* Section spacing: `mb-3`
* Cards: `mb-3`
* Forms: `mb-3` per field
* Tables: no extra margins inside cards

Avoid `mb-5`, `py-5`, large paddings.

### Page header (mandatory pattern)

Every page starts with this exact structure:

* Left: title + optional subtitle
* Right: primary action(s)

Bootstrap pattern:

```html
<div class="d-flex justify-content-between align-items-center mb-3">
  <div>
    <h1 class="mb-0">Page Title</h1>
    <div class="text-muted small">Optional context</div>
  </div>
  <div class="d-flex gap-2">
    <button class="btn btn-primary">Primary</button>
    <button class="btn btn-outline-secondary">Secondary</button>
  </div>
</div>
```

No hero sections. No banners.

---

## Typography

* Use semantic headings only.

  * Page title: `h1`
  * Section headers: `h2` or `h3` only when necessary
* Default Bootstrap body text everywhere else.
* Use `small` + `text-muted` for secondary metadata (timestamps, counts, hints).

No font size hacks. No inline styles.

---

## Color and Emphasis

Borrow from DecimalChain’s neutral, serious tone.

* Default UI is grayscale.
* Color is reserved for **state**, not decoration.

Allowed usage:

* `text-muted` → secondary info
* `badge bg-secondary` → neutral labels
* `badge bg-success | warning | danger` → status only
* Alerts only when something actually happened

No gradients. No custom palettes.

---

## Buttons

### Hierarchy

* One primary action per view. No exceptions.
* Secondary actions must be outline buttons.

### Sizes

* Page actions: normal size
* Table actions: **always small**

Standard table actions:

* View: `btn btn-sm btn-outline-primary`
* Edit: `btn btn-sm btn-outline-secondary`
* Delete: `btn btn-sm btn-outline-danger`

No icon-only buttons. Text always visible.

---

## Command / Action Bars (Reflect-inspired)

For list-heavy pages, actions live **above the table**, not scattered.

Pattern:

* Left: filters / search
* Right: primary action

Bootstrap:

```html
<div class="d-flex justify-content-between align-items-center mb-2">
  <input class="form-control form-control-sm w-50" placeholder="Search…" />
  <button class="btn btn-sm btn-primary">New</button>
</div>
```

Avoid filter sidebars in V1.

---

## Confirmations (SweetAlert standard)

All destructive or irreversible actions **must** use SweetAlert.

Rules:

* No `confirm()`
* No Bootstrap modal confirmations
* Same wording everywhere

### Destructive

* Icon: `warning`
* Title: `Delete this item?`
* Text: `This action cannot be undone.`
* Confirm: `Delete`
* Cancel: `Cancel`
* Confirm color: red

### Implementation

* Confirmation submits an existing POST form
* Never perform deletes via GET

---

## JavaScript

* Keep JavaScript to the minimum needed for the feature.
* Prefer no JavaScript when Bootstrap and server-rendered forms are sufficient.
* When JavaScript is required, place it in a feature-based or model-based file.
* Avoid inline scripts in Razor views unless there is no practical alternative.

---

## Forms

### Layout

* Single-column only
* Forms live in cards or plain pages

Each field:

* `label.form-label`
* `input.form-control` / `select.form-select`
* `asp-validation-for` with `text-danger`

### Validation

* Server-side is the source of truth
* Use:

  * `asp-validation-summary="ModelOnly"` at the top
  * Field-level validation messages

### Date / Time

* Always label as UTC
* Format hint: `YYYY-MM-DD HH:mm UTC`
* Input: `datetime-local`
* Helper text: `UTC (no local conversion in V1)`

---

## Tables (Primary Data Surface)

Tables are the core UI surface, Reflect-style.

### Style

* `table table-striped`
* Wrapped in `table-responsive`
* No table borders unless default

### Columns

Keep them lean:

* Name
* Client
* Status
* Closing (UTC)
* Actions

Details belong in the details view.

### Actions

* Right-aligned: `text-end`
* Small outline buttons only

---

## Cards

Cards group related content. Nothing more.

* `card mb-3`
* `card-header`: title + optional count
* `card-body`: content

Empty states:

* Show `text-muted small` → “No items.”
* No illustrations. No icons.

---

## Dashboard

Structure mirrors DecimalChain’s block layout.

### Sections (fixed)

* Urgent
* This Week
* Active
* History

Each section:

* Card
* Header: title + count
* Body: table (same columns everywhere)

Sorting:

* Urgent / This Week: closing ASC
* History: closing DESC

No charts in V1.

---

## Tender Details

* Use `dl.row` for key/value data
* Keep layout narrow and readable
* Actions visible at top:

  * Edit (always)
  * Documents (owner/admin)
  * Delete (Admin only, SweetAlert)

No tabs in V1 unless content explodes.

---

## Documents UI

Document management is now split into focused screens.

### Library Documents

* Index page is list-only.
* Create reusable documents on a dedicated page.
* Manage version history and upload new versions on the details page.

Do not mix create forms and version tables on the same list view.

### Tender Create

* The create tender page may include one optional file input for the tender advert / RFP document.
* Treat it as a helper upload during creation, not a full document-management page.

### Tender Documents

* Tender document management lives on its own page.
* Keep upload and attach-from-library forms in separate cards.
* The attached-document table remains the primary data surface.

---

## Navigation

Minimal, Reflect-style.

Top nav:

* Dashboard
* Tenders
* Library Docs (Admin only)
No feature links until the feature exists.

---

## Accessibility and Consistency

* Labels required for all inputs
* Buttons must contain text
* Status must be readable as text, not just color

---

## Do / Don’t

### Do

* Reuse the same table, card, and header patterns
* Prefer density over whitespace
* Ship boring, predictable UI
* Use SweetAlert consistently

### Don’t

* Add custom CSS for “just one tweak”
* Introduce new UI patterns casually
* Mix confirmation styles
* Design for aesthetics over clarity

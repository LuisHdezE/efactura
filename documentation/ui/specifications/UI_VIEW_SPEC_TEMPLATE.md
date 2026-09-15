# UI-<AREA>-NNN — <View Name>

Status: `DRAFT / NOT_VISUALLY_APPROVED`

Upstream interface scope ID: `<WEB/ANDROID-ID>`

## 1. Objective

Describe the function of this view and the user problem it solves.

## 2. Users and roles

List roles that can access the view and the authoritative source for those roles.

## 3. Business requirements

List real requirement identifiers only.

## 4. User stories

List existing user-story identifiers when available. If a story must be added, govern it separately rather than inventing an untracked identifier here.

## 5. Use cases

List real business/Application use cases and identifiers only.

## 6. Entry points and navigation

Describe how the user reaches the view and valid destinations from it.

## 7. Functional flow

Document step by step:

1. user input;
2. interaction;
3. expected processing;
4. result;
5. subsequent navigation or state.

## 8. Data displayed

For every relevant datum record:

| Data | Authority/source | Required | Notes |
| --- | --- | --- | --- |
|  |  |  |  |

## 9. Actions

| Action | Required permission/role | Backend support | API/Application binding | Notes |
| --- | --- | --- | --- | --- |
|  |  | `SUPPORTED / PENDING / SIMULATED / N/A` |  |  |

A visual control must not imply an executable capability when backend support is `PENDING`.

## 10. Visible business rules

Record rules that affect what the user sees, can enter or can execute.

## 11. Validation

Define client-side experience validation and the authoritative server-side validation/error mapping.

## 12. States

Cover applicable states explicitly:

- initial/default;
- loading;
- populated;
- empty;
- filtered empty;
- validation error;
- backend error;
- forbidden;
- conflict;
- rate limited;
- offline/unavailable when applicable;
- success/confirmation state.

## 13. Errors and confirmations

Define user-visible errors, destructive/irreversible confirmations and retry behavior without inventing server semantics.

## 14. Security and permissions

Define who may consult, create, modify, delete, confirm, cancel or execute fiscal/financial actions.

## 15. Responsive behavior

Document desktop, tablet and mobile expectations that are relevant to the workflow.

## 16. Accessibility

Document keyboard behavior, labels, focus order, status communication and non-color-only semantics.

## 17. Dependencies on other views

List stable UI identifiers when they exist; otherwise identify upstream scope candidates without inventing final IDs.

## 18. Backend/API integration

### Existing

List current endpoints, operation identifiers, contracts and Application use cases supported by repository evidence.

### Missing

List gaps explicitly. Missing capability must not be hidden behind mock UI without documentation.

### Temporary simulation

If approved for design/testing purposes, define exact simulation boundaries and ensure the UI makes no false product claim.

## 19. Visual references

| Version | Artifact | Status | Approval evidence |
| --- | --- | --- | --- |
| `v1` | `../references/drafts/<UI-ID>/v1.*` | `DRAFT` | none |

Only one exact version may be promoted to an approved baseline after explicit approval by Luis.

## 20. Approval record

- Approved version: `NONE`
- Approval date: `NONE`
- Approval statement/reference: `NONE`
- Approved artifact: `NONE`

## 21. Implementation evidence

- frontend route/component: `NONE`
- implementation commit/PR: `NONE`
- running capture: `NONE`
- visual review: `NONE`

## 22. Change history

Material changes after approval must create a new version and preserve the prior baseline.

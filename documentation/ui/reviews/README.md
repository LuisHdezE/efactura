# UI Visual and Functional Reviews

A governed view is not complete when code merely renders. After implementation, compare the running frontend against the exact approved visual baseline.

Recommended per-view structure:

```text
reviews/<UI-ID>/
├── approved-reference.*
├── implementation.*
└── visual-review.md
```

The review must cover at least:

- structure;
- visual hierarchy;
- component position;
- spacing;
- typography;
- colors;
- iconography;
- dimensions;
- responsive behavior;
- forms;
- tables;
- states;
- interactions;
- permission-aware behavior;
- absence of controls that imply unsupported backend capability.

Significant differences must be corrected or explicitly documented and accepted. A review does not silently redefine the approved reference.

## Review result

Use one of:

- `PASS`: implementation materially matches the approved baseline and functional contract;
- `PASS_WITH_ACCEPTED_DEVIATIONS`: documented differences are intentional and accepted;
- `FAIL`: significant visual or functional drift remains.

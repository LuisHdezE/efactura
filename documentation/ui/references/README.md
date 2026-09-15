# Visual References

Visual references are repository evidence, not chat-only artifacts.

## Drafts

Store unapproved alternatives under:

```text
references/drafts/<UI-ID>/
```

Use explicit versions such as `v1`, `v2`, `v3`. Desktop, tablet, mobile, modal and significant-state variants should be retained when they materially define the approved experience.

A draft remains `DRAFT` regardless of whether work continues from it.

## Approved baselines

Only an exact version explicitly approved by Luis may be copied/promoted under:

```text
references/approved/<UI-ID>/
```

The approval record must live in the corresponding functional specification and identify the exact artifact/version.

An approved reference must never be silently replaced. Material redesign creates a new version, preserves the previous one and requires a new explicit approval.

## Binary artifact rule

Generated images used to define a view must be stored in the repository. Showing an image in ChatGPT, an issue or a PR discussion is not sufficient evidence by itself.

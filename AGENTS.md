# Coding Agent Instructions

## 1. Core Principles

-   Write clean, readable, maintainable, and production-quality code.
-   Follow the project's existing architecture, conventions, and coding
    style. The repository is the source of truth.
-   Prefer simple, explicit, well-structured solutions over clever,
    over-engineered, or unnecessarily abstract ones.
-   Make the smallest change that correctly solves the task.
-   Do not introduce unrelated changes, speculative features, or
    unnecessary refactoring.
-   Do not invent requirements. If an important requirement is ambiguous
    and cannot be safely inferred from the codebase, ask for
    clarification.

## 2. Understand the Project Before Changing It

Before editing code:

1.  Inspect the relevant files and surrounding implementation.
2.  Read applicable project instructions, documentation, configuration,
    and existing tests.
3.  Identify the conventions already in use, including naming,
    formatting, folder structure, error handling, dependency injection,
    and architectural boundaries.
4.  Reuse existing utilities, abstractions, and patterns when they are
    appropriate.
5.  Check whether the requested behavior already exists before
    implementing it.

Do not assume a framework, library, language version, or project
structure without checking the repository.

## 3. Follow Existing Code Style

-   Match the style of the files being modified.
-   Preserve established naming conventions, formatting, brace
    placement, file organization, and language idioms.
-   Follow the project's existing architecture and separation of
    responsibilities.
-   Use the project's established patterns for logging, exceptions,
    validation, configuration, and dependency management.
-   Do not introduce a new coding style or architectural pattern for
    personal preference.
-   Do not reformat unrelated code.
-   Do not rename public APIs, files, classes, or members unless the
    task requires it.

If existing conventions conflict, prefer the convention used by the
nearest relevant, actively maintained code. Ask before making a broad
convention change.

## 4. Clean Code Requirements

-   Give variables, methods, classes, and files meaningful names.
-   Keep methods focused on one responsibility.
-   Avoid duplicated logic when a small, sensible abstraction would
    remove it.
-   Avoid premature abstraction and unnecessary design patterns.
-   Avoid deeply nested conditionals when clearer control flow is
    possible.
-   Keep dependencies explicit.
-   Handle errors deliberately. Never silently swallow exceptions or
    hide failures.
-   Validate inputs at appropriate boundaries.
-   Avoid magic numbers and unexplained constants.
-   Remove dead code only when it is directly related to the task and
    safe to remove.
-   Add comments only when they explain intent, constraints, or
    non-obvious reasoning. Do not narrate code that is already clear.
-   Do not add comments, documentation, or boilerplate merely to make
    the change look more substantial.

## 5. Do Not Do These Things

-   Do not over-engineer a straightforward task.
-   Do not create unnecessary classes, interfaces, managers, wrappers,
    factories, or layers.
-   Do not add dependencies when the existing project can solve the
    problem cleanly.
-   Do not duplicate functionality that already exists.
-   Do not change unrelated files or behavior.
-   Do not replace established project patterns with personal
    preferences.
-   Do not add placeholder implementations, fake data, or unrequested
    fallback behavior.
-   Do not suppress compiler warnings, linter rules, or test failures
    just to make checks pass.
-   Do not claim that code works without running the relevant checks or
    clearly stating that they were not run.
-   Do not make broad changes when a targeted fix is sufficient.

## 6. Dependencies and Compatibility

-   Prefer existing dependencies and built-in platform features.
-   Add or upgrade a dependency only when it is necessary for the
    requested task.
-   Check compatibility with the project's target framework, language
    version, and current dependency versions.
-   Do not update unrelated packages or configuration.
-   Do not introduce APIs that are unavailable in the project's
    configured environment.

## 7. Tests and Verification

-   Follow the project's existing testing framework and test
    conventions.
-   Add or update tests when behavior changes or a bug is fixed.
-   Prefer tests that verify observable behavior and important edge
    cases.
-   Run the most relevant tests, build, formatter, and/or static
    analysis available in the project.
-   Fix regressions caused by your changes.
-   Do not modify or delete tests merely to hide a failure.
-   If a check cannot be run, state exactly which check was skipped and
    why.

## 8. Editing Workflow

For each task:

1.  **Inspect:** Understand the relevant implementation and project
    conventions.
2.  **Plan:** Identify the smallest set of changes needed.
3.  **Implement:** Make focused changes that fit the existing codebase.
4.  **Verify:** Run relevant tests and checks.
5.  **Review:** Inspect the final diff for unrelated edits, duplication,
    accidental API changes, and style inconsistencies.
6.  **Report:** Summarize what changed and list verification results
    honestly.

Do not rewrite large portions of the codebase unless explicitly
requested.

## 9. Communication

-   Be concise and technically precise.
-   Explain important implementation decisions when they are not
    obvious.
-   Mention assumptions that materially affect the solution.
-   Report incomplete work, risks, and known limitations.
-   Distinguish clearly between verified results and expectations.
-   If the requested approach conflicts with the project's architecture
    or would introduce significant technical debt, explain the issue and
    propose a better alternative before proceeding with a broad change.

## 10. Definition of Done

A task is complete only when:

-   The requested behavior is implemented.
-   The implementation follows the existing project style and
    architecture.
-   No unrelated changes were introduced.
-   Relevant tests and checks pass, or skipped checks are explicitly
    reported.
-   The final diff has been reviewed.
-   The completion summary accurately describes the changes and
    verification status.
### Unity UI Architecture

- Prefer Unity Prefabs for reusable UI components and UXML/USS when the project uses UI Toolkit.
- Do NOT construct fixed UI hierarchies at runtime using `new GameObject()`, `AddComponent()`, or equivalent APIs when the same result can reasonably be achieved with a Prefab.
- Separate UI presentation from UI behavior. Visual structure and layout should be managed through serialized assets, while scripts handle data binding, interactions, and state.
- Use serialized references (`[SerializeField]`) instead of hardcoding UI styling and hierarchy construction in C#.
- Runtime UI generation is allowed when the structure is genuinely dynamic or procedurally determined. Prefer instantiating reusable Prefabs for repeated elements.
- Avoid repeatedly creating and destroying frequently used UI elements. Consider object pooling when profiling or usage patterns justify it.
- When a UI Prefab is required, create and configure the actual asset if the available tools support it. Do not silently replace it with runtime-generated UI.
- Follow the project's existing UI architecture and conventions. Do not introduce a new UI framework without explicit justification.
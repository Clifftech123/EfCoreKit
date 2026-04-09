# Contributing to EfCoreKit

Thank you for taking the time to contribute! This guide covers everything you need to go from idea to merged pull request.

---

## Table of Contents

- [Ways to Contribute](#ways-to-contribute)
- [Before You Start](#before-you-start)
- [Development Setup](#development-setup)
- [Branch Strategy](#branch-strategy)
- [Making Changes](#making-changes)
- [Running the Tests](#running-the-tests)
- [Code Style](#code-style)
- [Commit Messages](#commit-messages)
- [Pull Request Process](#pull-request-process)
- [Versioning and Releases](#versioning-and-releases)

---

## Ways to Contribute

- **Report a bug** — [Open an issue](https://github.com/Clifftech123/EfCoreKit/issues) with steps to reproduce, expected behaviour, and actual behaviour.
- **Request a feature** — [Open an issue](https://github.com/Clifftech123/EfCoreKit/issues) describing the use case and what you'd like the API to look like.
- **Fix a bug or implement a feature** — Fork the repo, make changes on a branch, and submit a pull request.
- **Improve documentation** — Typos, missing examples, unclear wording — all fixes are welcome.

---

## Before You Start

For anything beyond a small bug fix or documentation change, **open an issue first**. This lets us agree on the approach before you invest time writing code, and avoids situations where a well-written PR cannot be merged because the design doesn't fit the project's direction.

---

## Development Setup

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 10.0 or later |
| Git | Any recent version |

### Getting the code

```bash
# Fork the repo on GitHub, then clone your fork
git clone https://github.com/<your-username>/EfCoreKit.git
cd EfCoreKit

# Add the upstream remote so you can pull future changes
git remote add upstream https://github.com/Clifftech123/EfCoreKit.git
```

### Build

```bash
dotnet restore
dotnet build
```

---

## Branch Strategy

| Branch | Purpose |
|--------|---------|
| `master` | Latest stable release — never commit here directly |
| `develop` | Integration branch — all PRs target this branch |
| `feature/<name>` | New features |
| `fix/<name>` | Bug fixes |
| `docs/<name>` | Documentation-only changes |

**Always branch off `develop`, and open your PR against `develop`.**

```bash
git fetch upstream
git checkout -b fix/soft-delete-cascade upstream/develop
```

---

## Making Changes

1. Create your branch off `develop` (see above).
2. Make focused, minimal changes — one concern per PR.
3. Keep the public API backwards-compatible unless you've discussed a breaking change in an issue first.
4. Do not add features, refactor surrounding code, or clean up unrelated areas as part of a bug fix PR.
5. Update the relevant `docs/` page if your change affects documented behaviour.

---

## Running the Tests

The project uses integration tests (no mocks — tests run against a real in-memory or SQLite database):

```bash
dotnet test tests/EfCoreKit.Tests.Integration/EfCoreKit.Tests.Integration.csproj --configuration Release
```

All tests must pass before a PR can be merged. If you're adding a feature or fixing a bug, add a test that covers the new behaviour.

---

## Code Style

- Follow the conventions already in the codebase — consistency matters more than any individual preference.
- Use `var` where the type is obvious from the right-hand side.
- Prefer expression-bodied members for single-line methods/properties.
- Use `async`/`await` throughout — no `.Result` or `.Wait()`.
- No unused `using` directives.
- No commented-out code.
- XML doc comments (`///`) are not required unless you are adding a new public API surface.

The project does not currently enforce a formatter tool, so use your judgement to match the surrounding code.

---

## Commit Messages

Use the conventional commits style:

```
<type>: <short summary in present tense>

[Optional longer description explaining *why*, not what]
```

| Type | Use when |
|------|----------|
| `feat` | Adding a new feature |
| `fix` | Fixing a bug |
| `docs` | Documentation changes only |
| `test` | Adding or updating tests |
| `refactor` | Code change that is neither a fix nor a feature |
| `chore` | Build system, CI, or tooling changes |

Examples:

```
feat: add WhereIfNotEmpty extension method
fix: restore clears DeletedBy when soft-delete interceptor is enabled
docs: add cascade soft delete example to soft-delete guide
```

---

## Pull Request Process

1. Ensure your branch is up to date with `upstream/develop` before opening the PR.
2. Fill in the PR description — what changed, why, and how to test it.
3. All CI checks (build + tests) must pass.
4. At least one maintainer review is required before merge.
5. Squash commits if the history is noisy — a clean history per PR is preferred.
6. Once approved, a maintainer will merge into `develop`.



## Code of Conduct

Be respectful. Constructive criticism of code and design is welcome; personal criticism is not. We want this to be a project where everyone feels comfortable contributing.

If you experience or witness unacceptable behaviour, please open a private issue or contact the maintainer directly.

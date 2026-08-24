---
name: github-workflow
description: >-
  Procedures and guidelines for Git operations and GitHub CLI (`gh`) commands,
  including branch management, commit messages, and creating pull requests or issues.
---

# GitHub CLI & Git Workflow Runbook

Follow these structured procedures and guidelines when working with Git branches, commits, pull requests, and GitHub CLI (`gh`) commands.

---

## 1. GitHub CLI (`gh`) Guidelines for Windows

> [!IMPORTANT]
> **Always use `--body-file` (or `-F`) when passing markdown bodies to `gh` CLI commands on Windows.**
> Never use inline strings (`--body` or `-b`) for multiline markdown, PR descriptions, issue bodies, or release notes. PowerShell and Windows CMD shell quoting frequently cause broken newlines, mangled quotes, and unwanted character escapes.

### Creating a Pull Request with `gh pr create`

1. **Write the PR description to a file** (e.g., in a temporary or scratch directory):
   ```pwsh
   # Example: write PR body to a temp markdown file
   @'
   ## Context & Motivation
   Brief description of the problem, background, and why this change was made.

   ## Key Changes & Impact
   - Key change 1
   - Key change 2

   ## Testing & Verification
   - Ran automated tests via `dotnet run`
   - Verified build with Visual Studio 2026 MSBuild
   '@ | Set-Content -Path ./pr_body.md -Encoding utf8
   ```

2. **Execute `gh pr create` using `--body-file`**:
   ```pwsh
   gh pr create --title "feat: descriptive title" --body-file ./pr_body.md --base main
   ```

3. **Clean up the temporary file**:
   ```pwsh
   Remove-Item -Path ./pr_body.md -ErrorAction SilentlyContinue
   ```

### Creating or Editing Issues with `gh`
Similarly, always write issue bodies to a file and supply `--body-file`:
```pwsh
gh issue create --title "bug: descriptive title" --body-file ./issue_body.md
```

---

## 2. Commit Message & PR Title Conventions

- Use the [Conventional Commits](https://www.conventionalcommits.org/) standard.
- Format: `<type>(<optional-scope>): <short summary>`
  - `feat`: A new feature
  - `fix`: A bug fix
  - `docs`: Documentation changes only
  - `refactor`: Code changes that neither fix a bug nor add a feature
  - `perf`: Performance improvements
  - `test`: Adding or correcting tests
  - `chore`: Maintenance tasks, dependency updates, build tooling
- PR titles must follow conventional commit naming.
- PR descriptions must include **Context**, **Motivation**, and **Impact**, referencing any related issues (`Fixes #123`, `Resolves #456`).

---

## 3. Branching & Push Workflow

1. Ensure changes are committed with conventional messages.
2. Push branch to remote:
   ```pwsh
   git push -u origin <branch-name>
   ```
3. Open pull request using `gh pr create --body-file <path>`.

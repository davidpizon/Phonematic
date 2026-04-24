# Agents

This document describes the AI agents and coding guidelines for contributors and automated agents working on the Phonematic project.

## General Guidelines

- Follow existing code style and conventions found throughout the codebase.
- Target **.NET 10** for all projects unless otherwise specified.
- Make minimal changes to achieve the goal; avoid unnecessary refactoring.
- Do not add comments unless they match existing comment style or explain complex logic.
- Use existing libraries whenever possible; avoid adding new dependencies unless absolutely necessary.
- Validate all changes by building the solution and running relevant tests before considering a task complete.

## Project Structure

- Source code lives under the solution root at `C:\Users\david.pizon\source\repos\Phonematic\`.
- Documentation lives in the `/docs` folder.

## Coding Standards

- Use idiomatic C# and follow the conventions already present in the file being edited.
- Prefer `async`/`await` for asynchronous code.
- Keep pull requests focused and scoped to a single concern.

## Testing

- Run all relevant tests after making changes to verify nothing is broken.
- Add tests for new functionality where appropriate.

## Branching

- The default branch is `main`.
- Feature work should be done on a dedicated branch and submitted via pull request.

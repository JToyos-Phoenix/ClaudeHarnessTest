# Code conventions

> Extreme homogeneity. AI predicts better when the repository looks like itself everywhere.

## C# style

- **Version:** .NET 10, `LangVersion` latest. Set once in `Directory.Build.props`.
- **Compiler as a linter:** `Nullable` enabled and `TreatWarningsAsErrors` on. A warning
  is a broken build.
- **Format:** 4-space indentation, Allman braces, braces always (even one-line `if`).
  Max line length 120 characters.
- **Namespaces:** file-scoped (`namespace NotesCli;`). Tests use `NotesCli.Tests`.
- **Usings:** `System.*` first, then project namespaces. Rely on implicit usings for the
  basics (`System`, `System.IO`, `System.Linq`, `System.Collections.Generic`).
- **Strings:** interpolation `$"..."` always. No `string.Format`, no `+` concatenation.
  Raw string literals (`"""`) for multi-line text.
- **`var`** when the type is obvious from the right-hand side.
- **Collections:** collection expressions (`[]`, `[a, b]`) for new lists and arrays.

## Naming

| Kind                         | Convention         | Example                          |
|------------------------------|--------------------|----------------------------------|
| Files                        | `PascalCase.cs`, named after the main type | `NoteStorage.cs` |
| Types (class, record)        | `PascalCase`       | `Note`, `NoteStorage`            |
| Methods, properties          | `PascalCase`       | `Load`, `CreatedAt`              |
| Command handlers in `Cli`    | `Cmd<Command>`     | `CmdAdd`, `CmdSearch`            |
| Locals, parameters           | `camelCase`        | `notesPath`, `nextId`            |
| Private fields               | `_camelCase`       | `_path`                          |
| Constants                    | `PascalCase`       | `DefaultFileName`                |
| Exceptions                   | suffix `Exception` | `NoteNotFoundException`          |

## File structure

Every file in `src/` looks like this:

```csharp
using System.Globalization;          // usings (only what implicit usings do not cover)

namespace NotesCli;                  // file-scoped namespace

/// <summary>One line describing the purpose of the file.</summary>
public static class NoteStorage
{
    ...
}
```

One file per layer (see `docs/architecture.md`). Domain exceptions live next to the
domain model in `Note.cs`. Helper types used by a single layer are `private` nested types
of that layer (e.g. `Cli.Command`, `Cli.UsageException`).

## Tests

- One test class per source file: `tests/NotesCli.Tests/<File>Tests.cs`, class
  `<File>Tests`, `sealed`.
- Test method names: `Method_ExpectedResult_WhenCondition`, for example
  `Load_ReturnsEmpty_WhenFileMissing`.
- Arrange / Act / Assert separated by a blank line.
- Every test that touches disk uses `TempDirectory` (a real temp folder, deleted in
  `Dispose`). Never mock the filesystem.
- CLI tests call `Cli.Run(argv, stdout, stderr, path)` with `StringWriter`s and a temp
  path. They never touch `Console` or the real `.notes.json`.

## Error handling

Domain exceptions in `src/NotesCli/Note.cs`:

```csharp
/// <summary>Base class for domain errors.</summary>
public class NoteException(string message) : Exception(message);

/// <summary>Thrown when a note that does not exist is requested.</summary>
public sealed class NoteNotFoundException(string message) : NoteException(message);
```

`Cli.Run` catches `NoteException`, writes the message to `stderr` and returns 1.
Usage errors (unknown command, missing argument, non-numeric id) write the usage line to
`stderr` and return 2 (same codes as Python's argparse). Stack traces never reach the user.

## Comments

By default, **none**. Only allowed when they explain a non-obvious *why* (a documented
workaround, a subtle invariant). Names do the rest. `/// <summary>` one-liners on types
are the only exception (they play the role of Python module docstrings).

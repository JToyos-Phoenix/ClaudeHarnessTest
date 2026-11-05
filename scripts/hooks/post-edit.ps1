<#
    PostToolUse hook (matcher: Edit|Write), wired in .claude/settings.json.
    After Claude edits a C# file, build and run the tests.
      - green          -> exit 0 (silent)
      - red            -> exit 2 + summary on stderr, which Claude Code feeds back to Claude
      - non-code edit  -> exit 0 without running anything (docs/progress edits stay fast)
    The harness runs this, not the agent, so it cannot be skipped. Keep this file ASCII-only.
#>
$ErrorActionPreference = 'Continue'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location -LiteralPath $root

$file = ''
try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $file = [string]$payload.tool_input.file_path
} catch {
    $file = ''
}

if ($file -notmatch '\.(cs|csproj|props|slnx)$') {
    exit 0
}

$output = & dotnet test NotesCli.slnx --nologo 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    exit 0
}

$tail = ($output -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -Last 40) -join "`n"
[Console]::Error.WriteLine("[harness] Build or tests are red after editing ${file}:`n$tail")
exit 2

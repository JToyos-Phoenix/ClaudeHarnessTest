<#
    Stop hook, wired in .claude/settings.json.
    Before Claude ends its turn, run the full init.ps1 so the session never closes on a red project.
      - green -> exit 0
      - red   -> exit 2 + reason on stderr: Claude is told and keeps working ONCE.
    stop_hook_active (from the hook's stdin JSON) is true when Claude is already continuing
    because of this hook; then we only warn the human, to avoid an infinite loop.
    Keep this file ASCII-only.
#>
$ErrorActionPreference = 'Continue'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
Set-Location -LiteralPath $root

$alreadyContinuing = $false
try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $alreadyContinuing = [bool]$payload.stop_hook_active
} catch {
    $alreadyContinuing = $false
}

$log = Join-Path ([IO.Path]::GetTempPath()) 'harness_init.log'
& (Join-Path $root 'init.ps1') *> $log
if ($LASTEXITCODE -eq 0) {
    exit 0
}

$tail = (Get-Content -LiteralPath $log -Tail 20) -join "`n"
if ($alreadyContinuing) {
    $message = "[harness] init.ps1 is still red. Full log: $log"
    Write-Output (@{ systemMessage = $message } | ConvertTo-Json -Compress)
    exit 0
}

[Console]::Error.WriteLine(@"
[harness] init.ps1 FAILED, the project must not be left red (AGENTS.md section 5).
Fix it, or if you are mid-feature: record the state in progress/current.md and set the
feature to "blocked" in feature_list.json. Full log: $log
$tail
"@)
exit 2

<#
.SYNOPSIS
    init.ps1 - Environment verification and initialization for the harness.

.DESCRIPTION
    The agent runs this script at the START of every session and before declaring any
    feature `done`. If it fails, the session must not move forward.

    Output: a clear exit code (0 = ready, 1 = not ready) and [OK] / [WARN] / [FAIL] lines.
    Works on Windows PowerShell 5.1 and PowerShell 7+ (pwsh). Keep this file ASCII-only:
    Windows PowerShell 5.1 reads BOM-less scripts with the ANSI code page.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\init.ps1
    ./init.sh        # same thing, from Bash / Git Bash
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath $PSScriptRoot

$script:exitCode = 0

function Write-Ok([string]$message) {
    Write-Host '[OK]    ' -ForegroundColor Green -NoNewline
    Write-Host $message
}

function Write-Warn([string]$message) {
    Write-Host '[WARN]  ' -ForegroundColor Yellow -NoNewline
    Write-Host $message
}

function Write-Fail([string]$message) {
    Write-Host '[FAIL]  ' -ForegroundColor Red -NoNewline
    Write-Host $message
    $script:exitCode = 1
}

function Write-Section([string]$title) {
    Write-Host ''
    Write-Host ("-- $title " + ('-' * [Math]::Max(3, 52 - $title.Length)))
}

# ---------------------------------------------------------------------------
Write-Section '1. Checking environment'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail 'dotnet is not installed (https://dotnet.microsoft.com/download)'
    exit 1
}
$sdkVersion = (& dotnet --version | Out-String).Trim()
Write-Ok "dotnet SDK -> $sdkVersion"

$sdkMajor = 0
[void][int]::TryParse(($sdkVersion -split '\.')[0], [ref]$sdkMajor)
if ($sdkMajor -lt 10) {
    Write-Fail ".NET SDK >= 10 is required (found $sdkVersion)"
    exit 1
}
Write-Ok 'Compatible .NET SDK version (net10.0)'

# ---------------------------------------------------------------------------
Write-Section '2. Checking harness base files'

$baseFiles = @(
    'AGENTS.md',
    'feature_list.json',
    'progress/current.md',
    'docs/architecture.md',
    'docs/conventions.md',
    'docs/verification.md',
    'CHECKPOINTS.md'
)
foreach ($file in $baseFiles) {
    if (Test-Path -LiteralPath $file -PathType Leaf) {
        Write-Ok "Found $file"
    } else {
        Write-Fail "Missing base file: $file"
    }
}

# ---------------------------------------------------------------------------
Write-Section '3. Validating feature_list.json'

try {
    $data = Get-Content -LiteralPath 'feature_list.json' -Raw -Encoding UTF8 -ErrorAction Stop | ConvertFrom-Json
    $validStatus = @('pending', 'in_progress', 'done', 'blocked')
    $features = @($data.features)
    $invalid = @($features | Where-Object { $validStatus -notcontains $_.status })
    $inProgress = @($features | Where-Object { $_.status -eq 'in_progress' })
    $pending = @($features | Where-Object { $_.status -eq 'pending' })
    $duplicated = @($features | Group-Object -Property id | Where-Object { $_.Count -gt 1 })

    if ($invalid.Count -gt 0) {
        foreach ($feature in $invalid) { Write-Fail "Invalid status in feature $($feature.id): $($feature.status)" }
    } elseif ($inProgress.Count -gt 1) {
        Write-Fail "There are $($inProgress.Count) features in_progress (max 1)"
    } elseif ($duplicated.Count -gt 0) {
        Write-Fail "Duplicated feature ids: $(($duplicated | ForEach-Object { $_.Name }) -join ', ')"
    } else {
        Write-Ok "feature_list.json is valid ($($features.Count) features, $($pending.Count) pending)"
        foreach ($feature in $inProgress) {
            Write-Warn "Feature $($feature.id) '$($feature.name)' is in_progress: resume it (see progress/current.md)"
        }
    }
} catch {
    Write-Fail "feature_list.json is invalid: $($_.Exception.Message)"
}

# ---------------------------------------------------------------------------
Write-Section '4. Checking architecture rules'

$appProject = 'src/NotesCli/NotesCli.csproj'
$allowedSources = @('Cli.cs', 'Note.cs', 'NoteStorage.cs', 'Program.cs')

if (-not (Test-Path -LiteralPath $appProject -PathType Leaf)) {
    Write-Fail "Missing $appProject"
} else {
    if (Select-String -LiteralPath $appProject -Pattern '<PackageReference' -Quiet) {
        Write-Fail "$appProject has PackageReference entries (BCL only, see docs/architecture.md)"
    } else {
        Write-Ok "$appProject has no external dependencies"
    }

    $sources = @(Get-ChildItem -Path 'src/NotesCli' -Filter '*.cs' -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
    $unexpected = @($sources | Where-Object { $allowedSources -notcontains $_.Name })
    if ($unexpected.Count -gt 0) {
        Write-Fail "Unexpected files in src/NotesCli: $(($unexpected | ForEach-Object { $_.Name }) -join ', ') (layers are fixed, see docs/architecture.md)"
    } else {
        Write-Ok "src/NotesCli only contains the layers from docs/architecture.md"
    }

    $consoleUse = @($sources | Where-Object { $_.Name -ne 'Program.cs' } |
        Select-String -Pattern '\bConsole\.')
    if ($consoleUse.Count -gt 0) {
        foreach ($hit in $consoleUse) { Write-Fail "Console used outside Program.cs: $($hit.Filename):$($hit.LineNumber)" }
    } else {
        Write-Ok 'No Console.* calls outside Program.cs (IO goes through injected TextWriters)'
    }

    $todos = @($sources | Select-String -Pattern '\b(TODO|FIXME|HACK)\b')
    foreach ($hit in $todos) { Write-Warn "Marker without context? $($hit.Filename):$($hit.LineNumber): $($hit.Line.Trim())" }
}

# ---------------------------------------------------------------------------
Write-Section '5. Building and running tests'

if (Test-Path -LiteralPath 'tests' -PathType Container) {
    & dotnet test NotesCli.slnx --nologo
    if ($LASTEXITCODE -eq 0) {
        Write-Ok 'Build is clean (warnings are errors) and all tests pass'
    } else {
        Write-Fail 'Build or tests are broken'
    }
} else {
    Write-Warn 'tests/ folder does not exist yet'
}

# ---------------------------------------------------------------------------
Write-Section '6. Summary'

if ($script:exitCode -eq 0) {
    Write-Ok 'Environment ready. You can start working.'
} else {
    Write-Fail 'Environment is NOT ready. Fix the errors above before moving on.'
}

exit $script:exitCode

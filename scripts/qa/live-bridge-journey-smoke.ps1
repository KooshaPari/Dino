#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Minimal live-bridge journey evidence smoke — documents steps when the game bridge responds.

.DESCRIPTION
    Verifies named pipe, MCP health, and GameControlCli status without screen capture.

    - Writes a text receipt under docs/qa/evidence/live-bridge-journey_<date>/
    - Without DINO_GAME_PATH: still runs bridge/MCP checks
    - Connectivity evidence does not prove installed-pack gameplay or persistence
    - Exit 0 when status check passes; exit 1 when bridge status fails

.EXAMPLE
    pwsh -File scripts/qa/live-bridge-journey-smoke.ps1
#>
[CmdletBinding()]
param(
    [string]$EvidenceDate = (Get-Date -Format 'yyyy-MM-dd')
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $repoRoot

$gameControlProj = Join-Path $repoRoot 'src\Tools\GameControlCli\GameControlCli.csproj'
$evidenceDir = Join-Path $repoRoot "docs\qa\evidence\live-bridge-journey_$EvidenceDate"
$receiptPath = Join-Path $evidenceDir 'smoke-receipt.json'
$mcpHealthUrl = 'http://127.0.0.1:8765/health'
$pipePath = '\\.\pipe\dinoforge-game-bridge'

function Invoke-GameControlCli {
    param([Parameter(Mandatory)][string[]]$Args)
    $out = & dotnet run --project $gameControlProj -- @Args 2>&1
    $code = $LASTEXITCODE
    return @{ ExitCode = $code; Output = ($out | Out-String).Trim() }
}

New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null

$steps = [System.Collections.Generic.List[object]]::new()
$overallPass = $true

function Add-Step {
    param(
        [string]$Name,
        [bool]$Pass,
        [string]$Detail,
        [hashtable]$Extra = @{}
    )
    if (-not $Pass) { $script:overallPass = $false }
    $row = [ordered]@{
        step   = $Name
        pass   = $Pass
        detail = $Detail
        at     = (Get-Date).ToString('o')
    }
    foreach ($k in $Extra.Keys) { $row[$k] = $Extra[$k] }
    $steps.Add([pscustomobject]$row) | Out-Null
}

# Step 1: DINO_GAME_PATH (informational)
$dinoGamePath = [Environment]::GetEnvironmentVariable('DINO_GAME_PATH', 'Process')
if ([string]::IsNullOrWhiteSpace($dinoGamePath)) {
    $dinoGamePath = [Environment]::GetEnvironmentVariable('DINO_GAME_PATH', 'User')
}
$gamePathSet = -not [string]::IsNullOrWhiteSpace($dinoGamePath)
Add-Step -Name 'dino_game_path' -Pass $true -Detail $(if ($gamePathSet) { $dinoGamePath } else { 'unset — bridge checks only' })

# Step 2: Named pipe
$pipePresent = Test-Path -LiteralPath $pipePath
Add-Step -Name 'named_pipe' -Pass $pipePresent -Detail $(if ($pipePresent) { $pipePath } else { 'pipe not found' })

# Step 3: MCP health
try {
    $health = Invoke-RestMethod -Uri $mcpHealthUrl -Method Get -TimeoutSec 5
    Add-Step -Name 'mcp_health' -Pass $true -Detail ($health | ConvertTo-Json -Compress)
}
catch {
    Add-Step -Name 'mcp_health' -Pass $false -Detail $_.Exception.Message
}

# Step 4: GameControlCli status
$status = Invoke-GameControlCli -Args @('status')
$statusOk = ($status.ExitCode -eq 0) -and ($status.Output -match 'Connected to game bridge')
Add-Step -Name 'game_control_cli_status' -Pass $statusOk -Detail $status.Output -Extra @{ exit_code = $status.ExitCode }

$receipt = [ordered]@{
    schema      = 'dinoforge.live-bridge-journey-smoke/v1'
    observed_at = (Get-Date).ToString('o')
    evidence_dir = $evidenceDir
    dino_game_path_set = $gamePathSet
    overall_pass = $overallPass
    steps       = $steps
}

$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptPath -Encoding utf8

Write-Host "Live bridge journey smoke: overall_pass=$overallPass" -ForegroundColor $(if ($overallPass) { 'Green' } else { 'Red' })
Write-Host "Receipt: $receiptPath"

exit $(if ($overallPass) { 0 } else { 1 })

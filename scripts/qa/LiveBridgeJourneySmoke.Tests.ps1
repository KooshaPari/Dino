BeforeAll {
    $script:smokePath = Join-Path $PSScriptRoot 'live-bridge-journey-smoke.ps1'
    function dotnet { }
}

Describe 'Capture-free live bridge connectivity receipt' {
    BeforeEach {
        $global:dinoSmokeReceipt = $null
        Mock Set-Location { }
        Mock New-Item { }
        Mock Test-Path { $true }
        Mock Write-Host { }
        Mock Invoke-RestMethod { @{ status = 'healthy' } }
        Mock Set-Content { $global:dinoSmokeReceipt = $Value | ConvertFrom-Json }
    }

    It 'records successful connectivity without requesting capture' {
        Mock dotnet { $global:LASTEXITCODE = 0; 'Connected to game bridge' }
        & $script:smokePath
        $global:dinoSmokeReceipt.overall_pass | Should -BeTrue
        @($global:dinoSmokeReceipt.steps).Count | Should -Be 4
        $global:dinoSmokeReceipt.steps[-1].step | Should -Be 'game_control_cli_status'
        Should -Invoke dotnet -Exactly 1
        Should -Invoke dotnet -Exactly 0 -ParameterFilter { $args -contains 'screenshot' }
    }

    It 'rejects a successful CLI exit without a bridge connection receipt' {
        Mock dotnet { $global:LASTEXITCODE = 0; 'CLI ready' }
        & $script:smokePath
        $global:dinoSmokeReceipt.overall_pass | Should -BeFalse
        $global:dinoSmokeReceipt.steps[-1].pass | Should -BeFalse
        Should -Invoke dotnet -Exactly 1
    }
}

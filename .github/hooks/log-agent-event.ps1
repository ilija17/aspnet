param(
    [string]$LogPath = ".github/hooks/labos2/agent_log.txt"
)

$ErrorActionPreference = "Stop"

try {
    $inputJson = [Console]::In.ReadToEnd().Trim()

    if ([string]::IsNullOrWhiteSpace($inputJson)) {
        throw "Claude hook did not provide JSON on stdin."
    }

    $event = $inputJson | ConvertFrom-Json -Depth 100
    $logEntry = $event | ConvertTo-Json -Compress -Depth 100

    $resolvedLogPath = if ([System.IO.Path]::IsPathRooted($LogPath)) {
        $LogPath
    } else {
        Join-Path (Get-Location) $LogPath
    }

    $logDirectory = Split-Path -Parent $resolvedLogPath
    if (-not [string]::IsNullOrWhiteSpace($logDirectory)) {
        New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    }

    Add-Content -Path $resolvedLogPath -Value $logEntry -Encoding utf8
} catch {
    Write-Error "Failed to log Claude hook event: $($_.Exception.Message)"
    exit 1
}

Set-Location "$PSScriptRoot\.."
$registry = & "$PSScriptRoot\Get-DockerRegistry.ps1"
docker build -f "$PSScriptRoot\build.dockerfile" -t "$registry/dyndns-updater:latest" .
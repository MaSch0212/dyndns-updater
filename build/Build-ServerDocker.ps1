Set-Location "$PSScriptRoot\.."
$registry = & "$PSScriptRoot\Get-DockerRegistry.ps1"
docker build -f "$PSScriptRoot\build-server.dockerfile" -t "$registry/dyndns-updater:latest" .
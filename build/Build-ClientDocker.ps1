Set-Location "$PSScriptRoot\.."
$registry = & "$PSScriptRoot\Get-DockerRegistry.ps1"
docker build -f "$PSScriptRoot\build-client.dockerfile" -t "$registry/dyndns-updater-client:latest" .
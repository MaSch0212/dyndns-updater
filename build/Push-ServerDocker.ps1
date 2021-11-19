$registry = & "$PSScriptRoot\Get-DockerRegistry.ps1"
docker push "$registry/dyndns-updater:latest"
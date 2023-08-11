# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/Q2EMultiplayerPlus/*" -Force -Recurse
dotnet publish "./Q2EMultiplayerPlus.csproj" -c Release -o "$env:RELOADEDIIMODS/Q2EMultiplayerPlus" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location
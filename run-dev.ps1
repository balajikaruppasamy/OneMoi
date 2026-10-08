# Starts the OneMoi API and Angular web app in two windows.
#   API  → http://localhost:5080/swagger
#   Web  → http://localhost:4200
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Start-Process powershell -ArgumentList "-NoExit", "-Command", "`$env:ASPNETCORE_ENVIRONMENT='Development'; cd '$root\src\backend'; dotnet run --project OneMoi.Api --urls http://localhost:5080"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\src\frontend\onemoi-web'; npx ng serve --port 4200 --open"
Write-Host "Starting OneMoi... API: http://localhost:5080/swagger   Web: http://localhost:4200"

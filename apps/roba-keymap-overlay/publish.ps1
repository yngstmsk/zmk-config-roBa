#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "RoBaKeymapOverlay\RoBaKeymapOverlay.csproj"

Write-Host "Publishing RoBaKeymapOverlay..."
dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

$output = Join-Path $root "RoBaKeymapOverlay\bin\Release\net8.0-windows\win-x64\publish\RoBaKeymapOverlay.exe"
if (Test-Path $output) {
    Write-Host "Success: $output"
} else {
    Write-Error "Publish failed: output not found."
}

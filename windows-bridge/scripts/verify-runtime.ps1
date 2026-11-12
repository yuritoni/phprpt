$ErrorActionPreference = 'Stop'
Write-Host 'Checking .NET Framework 4.8...'
$release = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction Stop).Release
if ($release -lt 528040) { throw ".NET Framework 4.8 or later is required. Detected release: $release" }
Write-Host ".NET Framework release: $release"

$names = @(
  'CrystalDecisions.CrystalReports.Engine',
  'CrystalDecisions.Shared',
  'CrystalDecisions.ReportAppServer.ClientDoc',
  'CrystalDecisions.ReportAppServer.DataDefModel',
  'CrystalDecisions.ReportAppServer.ReportDefModel',
  'CrystalDecisions.ReportAppServer.Controllers'
)
$gacRoots = @(
  "$env:windir\Microsoft.NET\assembly\GAC_MSIL",
  "$env:windir\Microsoft.NET\assembly\GAC_64",
  "$env:windir\assembly\GAC_MSIL",
  "$env:windir\assembly\GAC_64"
)
foreach ($name in $names) {
  $found = $false
  foreach ($root in $gacRoots) {
    if (Test-Path $root) {
      $match = Get-ChildItem $root -Directory -Filter $name -ErrorAction SilentlyContinue | Select-Object -First 1
      if ($match) { $found = $true; break }
    }
  }
  if (-not $found) { Write-Warning "$name not found in common GAC locations. Confirm the SAP .NET SDK installation and add the reference path in Visual Studio." }
  else { Write-Host "Found $name" }
}
Write-Host 'Runtime check finished. Confirm the installed SAP support pack and license before deployment.'

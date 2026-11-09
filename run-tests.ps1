# Runs the Unity tests headless, the same way CI does.
# Usage: .\run-tests.ps1 [-Platform EditMode|PlayMode]
param([ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode')

$version = (Select-String -Path "$PSScriptRoot\ProjectSettings\ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
$unity = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
if (-not (Test-Path $unity)) { throw "Unity $version not found at $unity" }

$out = Join-Path $PSScriptRoot 'TestResults'
New-Item -ItemType Directory -Force $out | Out-Null
$results = Join-Path $out "$($Platform.ToLower()).xml"

Write-Host "Running $Platform tests with Unity $version (close the editor first)..."
& $unity -batchmode -nographics -projectPath $PSScriptRoot -runTests -testPlatform $Platform `
    -testResults $results -logFile (Join-Path $out 'unity.log') | Out-Null
$code = $LASTEXITCODE

[xml]$xml = Get-Content $results -ErrorAction SilentlyContinue
if ($xml) {
    $r = $xml.'test-run'
    Write-Host ("{0}: {1} passed, {2} failed, {3} skipped (total {4})" -f $r.result, $r.passed, $r.failed, $r.skipped, $r.total)
} else {
    Write-Host "No results file. See $out\unity.log"
}
exit $code

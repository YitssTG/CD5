param([switch]$ExportSql, [string]$OutputDirectory)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$dotnet=Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
$sdk=Get-ChildItem (Join-Path $env:ProgramFiles 'dotnet/sdk') -Directory | Where-Object {$_.Name -match '^9\.'} | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
$refs=Get-ChildItem (Join-Path $env:ProgramFiles 'dotnet/packs/Microsoft.NETCore.App.Ref') -Directory | Where-Object {$_.Name -match '^9\.'} | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
if(!$sdk -or !$refs){throw 'Requires installed .NET 9 SDK. This script installs nothing.'}
$out=Join-Path ([IO.Path]::GetTempPath()) 'CyberDefenseCoop-MissionTests'
if ($OutputDirectory) { $out=[IO.Path]::GetFullPath($OutputDirectory) }
New-Item -ItemType Directory -Path $out -Force|Out-Null
$source=@('Tests/Mission/EngineStubs.cs','Tests/Mission/MissionTests.cs','Assets/Scripts/Roles/RoleType.cs','Assets/Scripts/Roles/RoleManager.cs','Assets/Scripts/Roles/IncidentRules.cs','Assets/Scripts/Roles/IncidentDefinition.cs','Assets/Scripts/Roles/CooperativeIncident.cs','Assets/Scripts/Incidents/IncidentStep.cs','Assets/Scripts/Incidents/IncidentEngine.cs','Assets/Scripts/Incidents/SqlInjectionFlow.cs','Assets/Scripts/Incidents/ObjectiveType.cs','Assets/Scripts/Incidents/MinigameDefinition.cs','Assets/Scripts/Incidents/MinigameSelection.cs','Assets/Scripts/Incidents/ScoringConfig.cs','Assets/Scripts/Incidents/MissionRecords.cs')
$argsList=@('-nologo','-target:exe',('-out:"'+$out+'/MissionTests.dll"'))+@($source|ForEach-Object {'"'+(Join-Path $repo $_)+'"'})+@(Get-ChildItem (Join-Path $refs.FullName 'ref/net9.0') -Filter *.dll|ForEach-Object {'-r:"'+$_.FullName+'"'})
$argsList|Set-Content -LiteralPath "$out/tests.rsp"
'{"runtimeOptions":{"tfm":"net9.0","framework":{"name":"Microsoft.NETCore.App","version":"9.0.0"}}}'|Set-Content -LiteralPath "$out/MissionTests.runtimeconfig.json"
& $dotnet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') "@$out/tests.rsp"
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
if($ExportSql){& $dotnet "$out/MissionTests.dll" '--export' "$out/sql-steps.json" | Tee-Object -FilePath "$out/tests.log"}else{& $dotnet "$out/MissionTests.dll" | Tee-Object -FilePath "$out/tests.log"}
if($LASTEXITCODE -ne 0){throw 'Tests failed'}
Write-Output "Results: $out"

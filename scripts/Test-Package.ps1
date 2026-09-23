param([Parameter(Mandatory=$true)][string]$PackageDirectory,[Parameter(Mandatory=$true)][string]$ReShadeVersion)
$ErrorActionPreference='Stop'
$r=@('MHW - Reshade Injector Helper.exe','inject.exe','ReShade64.dll',"ReShade_Setup_$ReShadeVersion.exe",'MadMilkman.Ini.dll','LICENSE','NOTICE.md','LICENSES\ReShade-BSD-3-Clause.txt')
foreach($x in $r){if(-not(Test-Path (Join-Path $PackageDirectory $x) -PathType Leaf)){throw "Missing: $x"}}
$v=(Get-Item (Join-Path $PackageDirectory ReShade64.dll)).VersionInfo.ProductVersion
if($v -notmatch ('^'+[regex]::Escape($ReShadeVersion)+'(\.|$)')){throw "DLL version mismatch: $v"}
if(Get-ChildItem $PackageDirectory -Recurse -File -Filter *.fx){throw 'Shaders must not be bundled'}
if(Get-ChildItem $PackageDirectory -Recurse -Directory|? Name -eq reshade-shaders){throw 'reshade-shaders must not be bundled'}
Write-Host "Package checks passed for $ReShadeVersion"

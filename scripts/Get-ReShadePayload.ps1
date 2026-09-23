param(
 [Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
 [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference='Stop'
function Assert-Signer([string]$Path){
 $s=Get-AuthenticodeSignature -LiteralPath $Path
 if($null -eq $s.SignerCertificate){throw "Missing signer: $Path"}
 $subject=$s.SignerCertificate.Subject
 if($subject -notmatch 'CN=ReShade' -or $subject -notmatch 'E=info@reshade\.me'){throw "Unexpected signer: $subject"}
 if($s.Status -in @('NotSigned','HashMismatch')){throw "Invalid signature: $($s.Status)"}
 Write-Host "Signer verified: $subject ($($s.Status))"
}
function Assert-Version([string]$Path,[string]$Expected){
 $v=(Get-Item -LiteralPath $Path).VersionInfo.ProductVersion
 if([string]::IsNullOrWhiteSpace($v) -or $v -notmatch ('^'+[regex]::Escape($Expected)+'(\.|$)')){throw "Expected $Expected, got '$v': $Path"}
 Write-Host "Version verified: $v"
}
New-Item -ItemType Directory -Path $Destination -Force|Out-Null
$dest=(Resolve-Path -LiteralPath $Destination).Path
$name="ReShade_Setup_$Version.exe";$setup=Join-Path $dest $name;$dll=Join-Path $dest ReShade64.dll
Invoke-WebRequest -Uri "https://reshade.me/downloads/$name" -OutFile $setup -UseBasicParsing
Assert-Version $setup $Version;Assert-Signer $setup
& 7z e $setup "-o$dest" ReShade64.dll -y | Write-Host
if($LASTEXITCODE -ne 0 -or -not(Test-Path -LiteralPath $dll)){throw '7-Zip could not extract ReShade64.dll from the official installer.'}
Assert-Version $dll $Version;Assert-Signer $dll
Write-Host "Installer SHA256: $((Get-FileHash $setup -Algorithm SHA256).Hash)"
Write-Host "DLL SHA256: $((Get-FileHash $dll -Algorithm SHA256).Hash)"

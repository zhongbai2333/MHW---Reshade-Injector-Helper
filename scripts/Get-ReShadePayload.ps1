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
$b=[IO.File]::ReadAllBytes($setup);$offset=-1
for($i=0;$i -le $b.Length-4;$i++){if($b[$i]-eq 0x50-and$b[$i+1]-eq 0x4b-and$b[$i+2]-eq 3-and$b[$i+3]-eq 4){$offset=$i;break}}
if($offset -lt 0){throw 'Installer has no appended ZIP payload.'}
$tmp=Join-Path ([IO.Path]::GetTempPath()) ("reshade-"+[guid]::NewGuid().ToString('N')+".zip")
try{
 $payload=New-Object byte[] ($b.Length-$offset);[Array]::Copy($b,$offset,$payload,0,$payload.Length);[IO.File]::WriteAllBytes($tmp,$payload)
 Add-Type -AssemblyName System.IO.Compression.FileSystem;$z=[IO.Compression.ZipFile]::OpenRead($tmp)
 try{$e=$z.Entries|? FullName -eq ReShade64.dll|select -First 1;if($null -eq $e){throw 'DLL missing from payload'};[IO.Compression.ZipFileExtensions]::ExtractToFile($e,$dll,$true)}finally{$z.Dispose()}
}finally{Remove-Item $tmp -Force -ErrorAction SilentlyContinue}
Assert-Version $dll $Version;Assert-Signer $dll
Write-Host "Installer SHA256: $((Get-FileHash $setup -Algorithm SHA256).Hash)"
Write-Host "DLL SHA256: $((Get-FileHash $dll -Algorithm SHA256).Hash)"

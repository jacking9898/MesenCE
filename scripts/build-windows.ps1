[CmdletBinding()]
param(
	[switch]$Clean,
	[switch]$Zip
)

$ErrorActionPreference = "Stop"

if($env:OS -ne "Windows_NT") {
	throw "This script must be run on Windows."
}

$repoDir = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoDir "Mesen.sln"
$publishProfile = "UI\Properties\PublishProfiles\Release.pubxml"
$publishDir = Join-Path $repoDir "build\TmpReleaseBuild"
$exePath = Join-Path $publishDir "Mesen.exe"
$zipPath = Join-Path $repoDir "build\Mesen-windows-x64.zip"

function Find-MSBuild {
	$existing = Get-Command msbuild -ErrorAction SilentlyContinue
	if($existing) {
		return $existing.Source
	}

	$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
	if(Test-Path $vswhere) {
		$located = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
		if($located) {
			return $located
		}
	}

	throw "MSBuild was not found. Install Visual Studio with the Desktop development with C++ workload."
}

if(-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
	throw ".NET SDK was not found. Install the .NET 10 SDK."
}

$msbuild = Find-MSBuild
$buildTarget = if($Clean) { "Clean,UI" } else { "UI" }

Push-Location $repoDir
try {
	Write-Host "Restoring Windows x64 dependencies..."
	dotnet restore $solution `
		-p:TargetFramework=net10.0 `
		-r win-x64 `
		-p:PublishAot=true `
		-p:BuildWithNetFrameworkHostedCompiler=true

	Write-Host "Building the native core and UI..."
	& $msbuild $solution `
		-nologo `
		-m `
		-p:Configuration=Release `
		-p:Platform=x64 `
		-t:$buildTarget `
		-p:TargetFramework=net10.0 `
		-p:OptimizeUi=true

	Write-Host "Publishing a self-contained Windows executable..."
	dotnet publish $solution `
		--no-restore `
		-c Release `
		-r win-x64 `
		-p:PublishAot=true `
		-p:SelfContained=true `
		-p:PublishSingleFile=false `
		-p:OptimizeUi=true `
		-p:Platform="Any CPU" `
		-p:TargetFramework=net10.0 `
		/p:PublishProfile=$publishProfile

	if(-not (Test-Path $exePath)) {
		throw "Build completed without producing the expected executable: $exePath"
	}

	if($Zip) {
		if(Test-Path $zipPath) {
			Remove-Item $zipPath -Force
		}
		Compress-Archive -Path $exePath -DestinationPath $zipPath -CompressionLevel Optimal
	}
}
finally {
	Pop-Location
}

Write-Host ""
Write-Host "Build complete: $exePath"
if($Zip) {
	Write-Host "ZIP package:    $zipPath"
}

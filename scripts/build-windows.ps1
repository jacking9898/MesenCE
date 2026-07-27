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
$uiProject = Join-Path $repoDir "UI\UI.csproj"
$nativeProject = Join-Path $repoDir "InteropDLL\InteropDLL.vcxproj"
$publishProfile = Join-Path $repoDir "UI\Properties\PublishProfiles\Release.pubxml"
$publishDir = Join-Path $repoDir "build\TmpReleaseBuild"
$exePath = Join-Path $publishDir "Mesen.exe"
$zipPath = Join-Path $repoDir "build\Mesen-windows-x64.zip"
$solutionDir = $repoDir + [IO.Path]::DirectorySeparatorChar

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
$nativeBuildTarget = if($Clean) { "Clean,Build" } else { "Build" }

Push-Location $repoDir
try {
	Write-Host "Restoring Windows x64 dependencies..."
	dotnet restore $uiProject `
		-p:Configuration=Release `
		-p:TargetFramework=net10.0 `
		-r win-x64 `
		-p:PublishAot=true `
		-p:SolutionDir=$solutionDir `
		-p:BuildWithNetFrameworkHostedCompiler=true
	if($LASTEXITCODE -ne 0) {
		throw "Dependency restore failed with exit code $LASTEXITCODE."
	}

	Write-Host "Building the native core..."
	& $msbuild $nativeProject `
		-nologo `
		-m `
		-p:Configuration=Release `
		-p:Platform=x64 `
		-p:SolutionDir=$solutionDir `
		-t:$nativeBuildTarget
	if($LASTEXITCODE -ne 0) {
		throw "Native build failed with exit code $LASTEXITCODE."
	}

	Write-Host "Publishing a self-contained Windows executable..."
	dotnet publish $uiProject `
		--no-restore `
		-c Release `
		-r win-x64 `
		-p:PublishAot=true `
		-p:SelfContained=true `
		-p:PublishSingleFile=false `
		-p:OptimizeUi=true `
		-p:Platform="Any CPU" `
		-p:TargetFramework=net10.0 `
		-p:BuildProjectReferences=false `
		-p:SolutionDir=$solutionDir `
		/p:PublishProfile=$publishProfile
	if($LASTEXITCODE -ne 0) {
		throw "Publish failed with exit code $LASTEXITCODE."
	}

	if(-not (Test-Path $exePath)) {
		throw "Build completed without producing the expected executable: $exePath"
	}

	if($Zip) {
		if(Test-Path $zipPath) {
			Remove-Item $zipPath -Force
		}
		$runtimeFiles = @(
			$exePath,
			(Join-Path $publishDir "av_libglesv2.dll"),
			(Join-Path $publishDir "libHarfBuzzSharp.dll"),
			(Join-Path $publishDir "libSkiaSharp.dll")
		)
		$missingRuntimeFiles = @($runtimeFiles | Where-Object { -not (Test-Path $_) })
		if($missingRuntimeFiles.Count -gt 0) {
			throw "Cannot create ZIP package because runtime files are missing: $($missingRuntimeFiles -join ', ')"
		}
		Compress-Archive -LiteralPath $runtimeFiles -DestinationPath $zipPath -CompressionLevel Optimal
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

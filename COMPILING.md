> **Tested platforms:** The release build instructions below have only been tested on Windows 11 x64 and an Apple Silicon Mac with an M1 processor. Other Windows versions, Intel Macs, and other macOS configurations may work, but have not been verified.

## Windows

Install the following prerequisites:

- Visual Studio 2022 or newer with the **Desktop development with C++** workload
- The [.NET 10 SDK (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

### PowerShell build script

From the repository root, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1
```

`ExecutionPolicy Bypass` applies only to this PowerShell process and does not change the system execution policy. The script restores NuGet dependencies, builds the native core as `Release`/`x64`, and publishes a self-contained Windows application to `build\TmpReleaseBuild`. Keep all non-PDB files in that directory together when running or copying the application.

Use `-Clean` to clean and rebuild the native projects:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -Clean
```

Use `-Zip` to also create `build\Mesen-windows-x64.zip`. The archive contains the runtime files and excludes debug symbols:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-windows.ps1 -Zip
```

To confirm that the required SDK is installed, run `dotnet --list-sdks` and verify that a `10.0.x` SDK is listed.

### Visual Studio

1) Open the solution in Visual Studio (2022 or 2026)
2) Compile as `Release`/`x64`
3) Set the startup project to the `UI` project and run

## Linux

To build under Linux you need a version of Clang or GCC that supports C++17.  

Additionally, SDL2 and the [.NET 10 SDK](https://learn.microsoft.com/en-us/dotnet/core/install/linux) must also be installed.

Once SDL2 and the .NET 10 SDK are installed, run `make` to compile with Clang.  
To compile with GCC instead, use `USE_GCC=true make`.  
**Note:** Mesen usually runs faster when built with Clang instead of GCC.


## macOS

The macOS release build requires:

- Xcode installed at `/Applications/Xcode.app`, or `DEVELOPER_DIR` set to another Xcode developer directory
- SDL2, for example `brew install sdl2`
- The [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

Confirm that the required tools are available before building:

```bash
xcodebuild -version
sdl2-config --version
dotnet --list-sdks
```

The SDK list must include a `10.0.x` SDK.

### Release build script

From the repository root, run:

```bash
./scripts/build-macos.sh
```

This builds the native core and publishes a self-contained .NET `Release` application. The runtime identifier is selected from the current Mac architecture:

- Apple Silicon: `osx-arm64`
- Intel: `osx-x64` (not tested)

The application is written to:

```text
bin/<runtime-id>/Release/<runtime-id>/publish/Mesen.app
```

Use `--clean` to remove generated native object files before building:

```bash
./scripts/build-macos.sh --clean
```

Use `--zip` to also create a distributable ZIP archive at `bin/<runtime-id>/Release/Mesen.app.zip`:

```bash
./scripts/build-macos.sh --clean --zip
```

Use `--install` to copy the built application to `/Applications/Mesen.app`. This may request administrator privileges when `/Applications` is not writable:

```bash
./scripts/build-macos.sh --clean --zip --install
```

By default, the script applies an ad-hoc signature suitable for local use. Use `--no-sign` to skip it. Public distribution requires signing with an Apple Developer ID certificate and Apple notarization; the script does not perform those steps.

The application includes the .NET runtime, so the destination Mac does not need to install .NET separately. SDL2 is still a native runtime dependency and must be available on the destination Mac.

### Direct make build

For a basic local Release build, run:

```bash
make
```

`DEBUG=1 make` produces a Debug build instead. The release script is recommended for packaging because it configures the Xcode SDK and Apple Clang paths, signs the app locally, and can create the ZIP or install the application.

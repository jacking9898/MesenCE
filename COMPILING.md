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

To build macOS, install SDL2 (i.e via Homebrew) and the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).  

Once SDL2 and the .NET 10 SDK are installed, run `make`.

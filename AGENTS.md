# Repository Guidelines

## Project Structure & Module Organization

MesenCE combines a C++17 emulation core with a .NET 10/Avalonia desktop UI. Console implementations live under `Core/` (`NES/`, `SNES/`, `GBA/`, and others), while shared audio, archive, rendering, and platform helpers are in `Utilities/`. `InteropDLL/` exposes the native core to C# code in `UI/`; views, view models, configuration, localization, and packaged assets are organized below that directory. Platform integrations live in `Windows/`, `Linux/`, `MacOS/`, and `Sdl/`. Third-party sources are kept in directories such as `Lua/`, `SevenZip/`, and `UI/ThirdParty/`.

## Build, Test, and Development Commands

- `make`: build the native core and publish the Avalonia UI with Clang on Linux or macOS.
- `make run`: launch the locally built application.
- `DEBUG=1 make`: produce an unoptimized debug build; add `SANITIZER=address` or `SANITIZER=thread` when diagnosing native issues.
- `USE_GCC=true make`: build with GCC instead of the preferred Clang toolchain.
- `make clean`: remove generated native object files.
- `dotnet format --verify-no-changes`: verify C# formatting as CI does.

Windows contributors should open `Mesen.sln` in Visual Studio 2022 or newer, select `Release` and `x64`, and run the `UI` project. SDL2 and the .NET 10 SDK are required on Linux and macOS.

## Coding Style & Naming Conventions

Use tabs with a width of three, as configured in `.editorconfig`. Format C/C++ with the repository `.clang-format` (CI uses clang-format 20) and C# with `dotnet format`. Name functions and types in PascalCase (`ExampleFunction`), local variables in camelCase (`exampleVariable`), and private fields with a leading underscore (`_exampleMemberVariable`). Keep warnings clean; supported builds treat warnings as errors. Avoid adding branches or allocations to hot emulation paths without measuring unlocked FPS.

## Testing Guidelines

There is no standalone unit-test project. Build every affected platform/component and manually exercise the changed console or UI path. Recorded ROM tests use `*.mtp` files and run in CI through `PGOHelper.exe <Tests-folder> citests`; include or identify relevant test ROMs and expected behavior in the PR. Never commit copyrighted ROM content.

## Commit & Pull Request Guidelines

Follow the history’s scoped, imperative subjects, for example `NES: Fix mapper timing` or `UI: Add controller option`. Keep commits focused. PRs should explain the problem, implementation, test evidence, and performance impact; link issues and add screenshots for visible UI changes. Discuss significant features with maintainers before implementation.

Contributions must be MIT-compatible even though the repository is currently GPLv3. AI-generated code is prohibited; contributors must write and understand all submitted code.

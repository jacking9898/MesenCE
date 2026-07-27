# Mesen Community Edition

Mesen is a multi-system emulator for Windows, Linux, and macOS. It supports NES, SNES, Game Boy (GB/SGB/GBC), Game Boy Advance, PC Engine, SMS/Game Gear, and WonderSwan (WS/WSC).

This is a community-managed fork, created to maintain and expand this emulator into the future.

## 个人 Fork 说明 / Personal Fork Notice

本仓库的 `master` 分支是为了实现维护者个人 UI 需求而保留的非官方 fork，并非上游
MesenCE 的正式版本。相关改动包括可配置的 NES 游戏库、递归目录扫描、ZIP 游戏发现，
以及旧编码中文 ZIP 文件名兼容。部分代码由 AI 辅助生成，因此不符合上游
[`CONTRIBUTING.md`](CONTRIBUTING.md) 中禁止提交 AI 生成代码的贡献要求；这些改动不会
作为贡献或 Pull Request 提交给上游项目。

维护者创建和维护此 fork 仅用于个人学习和使用，不用于发布 Release、分发游戏 ROM
或其他目的。本说明仅描述维护者的用途和发布意图，不限制 GPLv3 已授予他人的权利。
仓库不包含任何受版权保护的游戏 ROM，所有源代码仍按 [GPLv3](LICENSE) 提供。

## Releases

The latest stable version is available from the [releases page on GitHub](https://github.com/nesdev-org/MesenCE/releases).

## Development Builds

[![Mesen](https://github.com/nesdev-org/MesenCE/actions/workflows/build.yml/badge.svg)](https://github.com/nesdev-org/MesenCE/actions/workflows/build.yml?query=branch%3Amaster)

* [Windows](https://nightly.link/nesdev-org/MesenCE/workflows/build/master/Mesen%20%28Windows%20-%20net10.0%20-%20AoT%29.zip)
  * Windows 7 or higher is required. Windows 7 users must use SP1 and have all updates installed.
* [Linux x64](https://nightly.link/nesdev-org/MesenCE/workflows/build/master/Mesen%20%28Linux%20-%20ubuntu-22.04%20-%20clang_aot%29.zip)  (requires **SDL2**)  
* [Linux ARM64](https://nightly.link/nesdev-org/MesenCE/workflows/build/master/Mesen%20%28Linux%20-%20ubuntu-22.04-arm%20-%20clang_aot%29.zip)  (requires **SDL2**)  
* [macOS - Intel](https://nightly.link/nesdev-org/MesenCE/workflows/build/master/Mesen%20%28macOS%20-%20macos-15-intel%20-%20clang_aot%29.zip)  (requires **SDL2**)  
* [macOS - Apple Silicon](https://nightly.link/nesdev-org/MesenCE/workflows/build/master/Mesen%20%28macOS%20-%20macos-15%20-%20clang_aot%29.zip)  (requires **SDL2**)  

#### <ins>Notes</ins> ####

* Other builds are also available in the [Actions](https://github.com/nesdev-org/MesenCE/actions/workflows/build.yml?query=branch%3Amaster) tab.
* **macOS**: Builds are self-signed and will require approval via Gatekeeper before they are able to be run.  
* **SteamOS**: See [SteamOS.md](SteamOS.md)  

## Compiling

See [COMPILING.md](COMPILING.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md)

## License

Mesen is available under the GPL V3 license.  Full text here: <http://www.gnu.org/licenses/gpl-3.0.en.html>

Copyright (C) 2014-2026 Sour, 2026 contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

#!/usr/bin/env bash

set -euo pipefail

usage()
{
	cat <<'EOF'
Usage: ./scripts/build-macos.sh [options]

Options:
  --clean       Remove native object files before building.
  --install     Copy Mesen.app to /Applications after building.
  --zip         Create bin/osx-arm64/Release/Mesen.app.zip.
  --no-sign     Skip local ad-hoc code signing.
  -h, --help    Show this help.
EOF
}

clean=false
install=false
create_zip=false
sign=true

while (($# > 0)); do
	case "$1" in
		--clean) clean=true ;;
		--install) install=true ;;
		--zip) create_zip=true ;;
		--no-sign) sign=false ;;
		-h|--help) usage; exit 0 ;;
		*) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
	esac
	shift
done

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_dir="$(cd -- "$script_dir/.." && pwd)"

if [[ "$(uname -s)" != "Darwin" ]]; then
	echo "This script must be run on macOS." >&2
	exit 1
fi

developer_dir="${DEVELOPER_DIR:-/Applications/Xcode.app/Contents/Developer}"
if [[ ! -d "$developer_dir" ]]; then
	echo "Xcode was not found at: $developer_dir" >&2
	echo "Install Xcode or set DEVELOPER_DIR before running this script." >&2
	exit 1
fi

export DEVELOPER_DIR="$developer_dir"

for command_name in dotnet make sdl2-config xcrun codesign; do
	if ! command -v "$command_name" >/dev/null 2>&1; then
		echo "Required command not found: $command_name" >&2
		exit 1
	fi
done

sdk_root="$(xcrun --sdk macosx --show-sdk-path)"
apple_cc="$(xcrun --sdk macosx --find clang)"
apple_cxx="$(xcrun --sdk macosx --find clang++)"
architecture="$(uname -m)"

case "$architecture" in
	arm64) runtime_id="osx-arm64" ;;
	x86_64) runtime_id="osx-x64" ;;
	*) echo "Unsupported Mac architecture: $architecture" >&2; exit 1 ;;
esac

app_path="$repo_dir/bin/$runtime_id/Release/$runtime_id/publish/Mesen.app"
zip_path="$repo_dir/bin/$runtime_id/Release/Mesen.app.zip"
jobs="$(sysctl -n hw.logicalcpu 2>/dev/null || getconf _NPROCESSORS_ONLN 2>/dev/null || echo 1)"

cd "$repo_dir"

if $clean; then
	make clean
fi

echo "Building the native core for $runtime_id..."
SDKROOT="$sdk_root" make -j"$jobs" core CC="$apple_cc" CXX="$apple_cxx"

# macOS ships an old GNU Make that can hang when a parallel job launches the
# long-running dotnet publish recipe. Keep native compilation parallel and run
# the UI packaging recipe serially.
echo "Publishing the macOS application..."
SDKROOT="$sdk_root" make ui CC="$apple_cc" CXX="$apple_cxx"

if [[ ! -d "$app_path" ]]; then
	echo "Build completed without producing the expected app: $app_path" >&2
	exit 1
fi

if $sign; then
	echo "Applying a local ad-hoc signature..."
	codesign --force --deep --sign - "$app_path"
	codesign --verify --deep --strict "$app_path"
fi

if $create_zip; then
	echo "Creating: $zip_path"
	rm -f "$zip_path"
	ditto -c -k --sequesterRsrc --keepParent "$app_path" "$zip_path"
fi

if $install; then
	echo "Installing to /Applications/Mesen.app..."
	if [[ -w /Applications ]]; then
		ditto "$app_path" /Applications/Mesen.app
	else
		sudo ditto "$app_path" /Applications/Mesen.app
	fi
fi

echo
echo "Build complete: $app_path"
if $create_zip; then
	echo "ZIP package:    $zip_path"
fi
if $install; then
	echo "Installed app:  /Applications/Mesen.app"
fi

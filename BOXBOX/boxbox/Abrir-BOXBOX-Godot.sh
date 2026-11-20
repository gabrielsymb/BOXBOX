#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
default_godot_bin="$HOME/Downloads/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64"
godot_bin="${GODOT_BIN:-$default_godot_bin}"

if [[ ! -x "$godot_bin" ]]; then
    printf 'Não encontrei o Godot .NET neste caminho:\n%s\n' "$godot_bin" >&2
    printf 'Defina GODOT_BIN para usar outra instalação.\n' >&2
    exit 1
fi

dotnet_candidates=()
if [[ -n "${DOTNET_ROOT:-}" ]]; then
    dotnet_candidates+=("$DOTNET_ROOT/dotnet")
fi
dotnet_candidates+=("$HOME/.dotnet/dotnet")

path_dotnet="$(command -v dotnet || true)"
if [[ -n "$path_dotnet" ]]; then
    dotnet_candidates+=("$path_dotnet")
fi

dotnet_bin=""
for candidate in "${dotnet_candidates[@]}"; do
    if [[ -x "$candidate" ]] && "$candidate" --list-sdks 2>/dev/null | grep -Eq '^8\.'; then
        dotnet_bin="$(readlink -f -- "$candidate")"
        break
    fi
done

if [[ -z "$dotnet_bin" ]]; then
    printf 'Não encontrei um SDK .NET 8 válido.\n' >&2
    printf 'O BOXBOX espera encontrá-lo em %s ou no PATH.\n' "$HOME/.dotnet" >&2
    exit 1
fi

dotnet_root="$(dirname -- "$dotnet_bin")"
export DOTNET_ROOT="$dotnet_root"
export PATH="$dotnet_root:$PATH"

launch_mode=(--editor)
if [[ "${1:-}" == "--run" ]]; then
    launch_mode=()
fi

exec "$godot_bin" --path "$script_dir" "${launch_mode[@]}"

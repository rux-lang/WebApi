#!/bin/sh
# Entrypoint for the Rux Playground sandbox.
#
# Usage: run-playground.sh [run|asm]
#
# The user's program is mounted read-only at /playground/Main.rux. We copy the
# pre-scaffolded package template into a writable tmpfs, drop the source in, and
# either run it or emit its assembly. All build artifacts stay inside the
# throwaway work directory.
set -eu

mode="${1:-run}"

work="$(mktemp -d)"
cp -a /home/runner/template/. "$work/"
cp /playground/Main.rux "$work/Src/Main.rux"
cd "$work"

case "$mode" in
    asm)
        # Build progress goes to stdout; keep it out of the assembly output.
        rux -q build --dump-asm >/dev/null
        cat Temp/Asm/out.asm
        ;;
    run)
        exec rux -q run
        ;;
    *)
        echo "unknown mode: $mode" >&2
        exit 2
        ;;
esac

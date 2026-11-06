#!/usr/bin/env bash
# Builds the base images every other Automatica image starts from:
#
#   automaticacore/automatica-plugin-runtime:amd64-<N>     docker/plugins     linux/amd64
#   automaticacore/automatica-plugin-runtime:arm32v7-<N>   docker/plugins     linux/arm/v7
#   automaticacore/automatica-plugin-build:latest-amd64    docker/plugin-sdk  linux/amd64
#   automaticacore/automatica-plugin-build:amd64-<N>       (same image, immutable-ish tag)
#
# Why the arm32v7 image matters: the app images (core, plugin standalone, satellite,
# plugins) are built on amd64 agents. Their SDK stage cross-publishes with -r linux-arm
# and the final stage only COPYs onto this image, so its architecture decides theirs.
#
# Each tag is a plain single-platform manifest (no provenance/SBOM attestations, which
# would turn it into an index): the consuming Dockerfiles FROM the arch tag directly
# while building on amd64, and that only works with a single-platform manifest.
#
# Usage: docker/build-base-images.sh [--push]
#   without --push the images are loaded into the local engine for testing.
# Needs QEMU binfmt for arm (Docker Desktop has it; on Linux:
#   docker run --privileged --rm tonistiigi/binfmt:qemu-v10.2.3 --install arm).
# Env: DOTNET_MAJOR (default 10), REGISTRY_ORG (default automaticacore).
set -euo pipefail

# Git Bash on Windows would rewrite container paths like /usr/lib/... into C:/Program Files/Git/...
export MSYS_NO_PATHCONV=1

DOTNET_MAJOR="${DOTNET_MAJOR:-10}"
DOTNET_VERSION="${DOTNET_MAJOR}.0"
REGISTRY_ORG="${REGISTRY_ORG:-automaticacore}"

OUTPUT="--load"
if [[ "${1:-}" == "--push" ]]; then
  OUTPUT="--push"
fi

cd "$(dirname "$0")"

RUNTIME="$REGISTRY_ORG/automatica-plugin-runtime"
BUILD="$REGISTRY_ORG/automatica-plugin-build"

build() {
  local platform=$1 context=$2
  shift 2
  echo "::: building $context for $platform ($*)"
  docker buildx build \
    --platform "$platform" \
    --provenance=false --sbom=false \
    --pull \
    --build-arg DOTNET_VERSION="$DOTNET_VERSION" \
    "$@" "$OUTPUT" "$context"
}

# Fails unless the image and its native libnserial really are the expected architecture.
verify_runtime() {
  local tag=$1 platform=$2 expected_arch=$3 expected_elf=$4
  if [[ "$OUTPUT" == "--push" ]]; then
    docker pull -q --platform "$platform" "$tag" >/dev/null
  fi

  local arch elf
  arch=$(docker image inspect -f '{{.Architecture}}{{if .Variant}}/{{.Variant}}{{end}}' "$tag")
  # ELF e_machine (offset 18, little endian): 3e00 = x86-64, 2800 = ARM
  elf=$(docker run --rm --platform "$platform" --entrypoint od "$tag" \
        -An -tx1 -j18 -N2 /usr/lib/libnserial.so.1.1 | tr -d ' \n')

  echo "::: $tag -> image $arch, libnserial e_machine $elf"
  if [[ "$arch" != "$expected_arch" || "$elf" != "$expected_elf" ]]; then
    echo "::: ERROR: expected image $expected_arch with e_machine $expected_elf" >&2
    exit 1
  fi
}

build linux/amd64  plugins    -t "$RUNTIME:amd64-$DOTNET_MAJOR"
build linux/arm/v7 plugins    -t "$RUNTIME:arm32v7-$DOTNET_MAJOR"
build linux/amd64  plugin-sdk -t "$BUILD:latest-amd64" -t "$BUILD:amd64-$DOTNET_MAJOR"

verify_runtime "$RUNTIME:amd64-$DOTNET_MAJOR"   linux/amd64  amd64  3e00
verify_runtime "$RUNTIME:arm32v7-$DOTNET_MAJOR" linux/arm/v7 arm/v7 2800

echo "::: done ($OUTPUT)"

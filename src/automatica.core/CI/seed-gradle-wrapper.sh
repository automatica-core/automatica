#!/usr/bin/env bash
# Downloads the Gradle distribution named in gradle-wrapper.properties into a cache folder and points the
# wrapper at that local copy. services.gradle.org only redirects to
# github.com/gradle/gradle-distributions/releases/download, which can fail for hours (HTTP 504) while the
# same file is still served through the GitHub API, so that is the fallback. The wrapper checks
# distributionSha256Sum on the local copy as well, so a file from either source is verified before use.
#
# usage: seed-gradle-wrapper.sh <path to gradle-wrapper.properties> <cache folder>
# GITHUB_TOKEN is optional: without it the API allows 60 requests per hour per IP.
set -euo pipefail

props="$1"
cache="$2"

url=$(sed -n 's/^distributionUrl=//p' "$props" | sed 's/\\:/:/g')
sha=$(sed -n 's/^distributionSha256Sum=//p' "$props")
if [ -z "$sha" ]; then
  echo "##[error]distributionSha256Sum is missing in $props"
  exit 1
fi

name=$(basename "$url")
version=$(echo "$name" | sed -E 's/^gradle-(.+)-(all|bin)\.zip$/\1/')
zip="$cache/$name"
mkdir -p "$cache"

verified() { [ -f "$zip" ] && echo "$sha  $zip" | sha256sum -c --status -; }

if verified; then
  echo "Using cached $name"
else
  rm -f "$zip"
  if curl -fsSL --retry 2 --retry-delay 10 --max-time 600 -o "$zip" "$url" && verified; then
    echo "Downloaded $name from $url"
  else
    echo "##[warning]$url failed, downloading $name through the GitHub API"
    rm -f "$zip"
    api=https://api.github.com/repos/gradle/gradle-distributions
    auth=()
    # an undefined pipeline variable arrives as the literal text $(GITHUB_TOKEN)
    case "${GITHUB_TOKEN:-}" in '$('*) GITHUB_TOKEN= ;; esac
    if [ -n "${GITHUB_TOKEN:-}" ]; then auth=(-H "Authorization: Bearer $GITHUB_TOKEN"); fi
    id=$(curl -fsSL --retry 3 "${auth[@]}" "$api/releases/tags/v$version" \
      | python3 -c "import sys, json; print(next(a['id'] for a in json.load(sys.stdin)['assets'] if a['name'] == sys.argv[1]))" "$name")
    curl -fsSL --retry 5 --retry-delay 15 --max-time 900 "${auth[@]}" -H "Accept: application/octet-stream" \
      -o "$zip" "$api/releases/assets/$id"
    if ! verified; then
      echo "##[error]checksum mismatch for $name"
      exit 1
    fi
  fi
fi

# only changes this checkout: the wrapper copies the verified zip instead of downloading it
sed -i "s|^distributionUrl=.*|distributionUrl=file\\\\://$zip|" "$props"
grep '^distributionUrl=' "$props"

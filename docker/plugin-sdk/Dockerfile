# Build image for plugins and the supervisor (automaticacore/automatica-plugin-build:latest-amd64).
# Only amd64 is needed: ARM images are cross-published (-r linux-arm) on amd64 and then
# copied onto the arm32v7 runtime image. Build it with docker/build-base-images.sh.
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

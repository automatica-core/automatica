FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# update and install in one layer: a cached "apt-get update" layer on its own leaves the install without a package index
RUN apt-get update && apt-get install -y --no-install-recommends zip net-tools && rm -rf /var/lib/apt/lists/*

ARG VERSION

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

RUN automatica-cli setversion $VERSION -W /src/src/Automatica.Core.Plugin.Standalone/
RUN dotnet publish -c Release -o /app/plugin /src/src/Automatica.Core.Plugin.Standalone/ -r linux-x64

RUN echo $VERSION
RUN rm -rf /src

# Root for now: every plugin image starts FROM this one (automatica.build plugin Dockerfile) and
# the satellite runs it --privileged with the host's /dev. Plugins still write next to their own
# assemblies (HomeKit unpacks libsodium into /plugin at start), so a non-root USER here breaks them.
# To switch: plugins write to a data directory, the plugin Dockerfile copies with --chown, then
# "USER 10001" with supplementary groups dialout (20) and plugdev (46) for serial / HID devices.
FROM automaticacore/automatica-plugin-runtime:amd64-10 AS runtime
WORKDIR /app/

COPY --from=build /app/ ./
# Base runtime image for Automatica.Core, the plugin standalone host and the satellite
# (automaticacore/automatica-plugin-runtime:amd64-<N> and :arm32v7-<N>).
#
# Platform-agnostic: the architecture comes from `docker buildx build --platform ...`.
# Build it with docker/build-base-images.sh. A plain `docker build` on an amd64 machine
# always produces an amd64 image (with an x64 libnserial), whatever the tag says.
ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS libnserial

# Pinned so a rebuild produces the same native library; bump deliberately.
ARG SERIALPORTSTREAM_REF=6296737d0afcdfb9b1b027fba03193d482c334bd

RUN apt-get update \
 && apt-get install -y --no-install-recommends cmake build-essential \
 && rm -rf /var/lib/apt/lists/*

RUN git clone https://github.com/jcurl/serialportstream.git /src/serialportstream \
 && git -C /src/serialportstream checkout ${SERIALPORTSTREAM_REF}
RUN cd /src/serialportstream/dll/serialunix && ./build.sh


FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app/

COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so /usr/lib

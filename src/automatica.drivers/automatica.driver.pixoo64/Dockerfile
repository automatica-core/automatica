# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

ARG MANIFEST_DIR
ARG VERSION
ARG CONFIG


RUN dotnet --list-sdks
RUN dotnet tool install automatica-plugin-standalone --global
RUN dotnet tool install automatica-cli --global

COPY . ./

ENV PATH="${PATH}:/root/.dotnet/tools"
# WORKDIR /plugin
RUN pwd
RUN ls

RUN mkdir /build
RUN mkdir /plugin
RUN automatica-cli pack -W $MANIFEST_DIR -V $VERSION -C $CONFIG -o /build/ -U false

RUN for file in /build/*.acpkg; do mv $file /build/pluginFile.acpkg; break; done

RUN ls /build

RUN automatica-cli InstallPlugin -p /build/pluginFile.acpkg -I /plugin


FROM mcr.microsoft.com/dotnet/sdk:7.0 AS runtime
COPY --from=build /plugin /plugin

RUN dotnet --list-sdks
RUN dotnet tool install automatica-plugin-standalone --global
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN ls /plugin

ENTRYPOINT ["automatica-plugin-standalone", "/plugin"]

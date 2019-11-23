FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /app

RUN apt-get update 
RUN apt-get install zip net-tools -y

ARG AUTOMATICA_VERSION

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

RUN automatica-cli setversion $AUTOMATICA_VERSION -W /src/src/Automatica.Core.Plugin.Standalone/
RUN dotnet publish -c Release -o /app/plugin /src/src/Automatica.Core.Plugin.Standalone/ -r linux-x64

RUN echo $AUTOMATICA_VERSION
RUN rm -rf /src

FROM automaticacore/automatica-plugin-runtime:amd64 AS runtime
WORKDIR /app/

COPY --from=build /app/ ./
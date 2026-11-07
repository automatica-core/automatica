ARG RUNTIME_IMAGE_TAG
FROM automaticacore/automatica-plugin-build:latest-amd64 AS build
WORKDIR /app

ARG VERSION

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

RUN automatica-cli setversion $VERSION -W /src/Automatica.Core.Supervisor/
RUN dotnet publish -c Release -o /app/supervisor /src/Automatica.Core.Supervisor/ -r linux-x64

RUN rm -rf /src
RUN rm -rf /app/supervisor/appsettings.json

# Runs as root on purpose: the supervisor creates, updates and removes the core / satellite
# containers through the host's docker.sock (bind-mounted by supervisor.service), which is root on
# the host anyway.
FROM automaticacore/automatica-plugin-standalone:$RUNTIME_IMAGE_TAG AS runtime
WORKDIR /app/supervisor

ARG DEFAULT_IMAGE
ARG DEFAULT_TAG
ARG DOCKER_REGISTRY

ENV image=$DEFAULT_IMAGE
ENV imageTag=$DEFAULT_TAG
ENV dockerRegistry=$DOCKER_REGISTRY

COPY --from=build /app/supervisor ./
ENTRYPOINT ["/app/supervisor/Automatica.Core.Supervisor"]
FROM node:18-alpine as node
WORKDIR /www

COPY ./src/Automatica.Core.Satellite.Web /www
RUN rm -rf /www/node_modules
RUN npm install --global devextreme
RUN npm install
RUN npm run build-docker

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

ARG VERSION

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

RUN automatica-cli setversion $VERSION -W /src/src
RUN dotnet publish -c Release -o /app/satellite /src/src/ -r linux-x64

# Runs as root on purpose: the satellite starts and removes plugin containers through the host's
# docker.sock (mounted by the supervisor, together with --privileged and /dev). With the socket
# inside, a non-root USER would not contain anything.
FROM automaticacore/automatica-plugin-runtime:amd64-10 AS runtime
WORKDIR /app/satellite

COPY --from=node /www/dist /app/satellite/wwwroot

EXPOSE 8080

COPY --from=build /app/satellite ./
ENTRYPOINT ["/app/satellite/Automatica.Core.Satellite"]
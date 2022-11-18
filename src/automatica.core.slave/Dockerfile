FROM node:18-alpine as node
WORKDIR /www

COPY ./src/Automatica.Core.Slave.Web /www
RUN rm -rf /www/node_modules
RUN npm install --global devextreme
RUN npm install
RUN npm run build-docker

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

ARG VERSION

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

RUN automatica-cli setversion $VERSION -W /src/src
RUN dotnet publish -c Release -o /app/slave /src/src/ -r linux-x64

FROM automaticacore/automatica-plugin-runtime:amd64-7 AS runtime
WORKDIR /app/slave

COPY --from=node /www/dist /app/slave/wwwroot

EXPOSE 8080

COPY --from=build /app/slave ./
ENTRYPOINT ["/app/slave/Automatica.Core.Slave"]
FROM node:12-alpine as node
WORKDIR /src

ARG FONTAWESOME_TOKEN


# Copy everything else and build
COPY ./src/automatica.core/Automatica.WebNew /src

RUN ls -lah /src
RUN npm config set "@fortawesome:registry" https://npm.fontawesome.com/ 
RUN npm config set "//npm.fontawesome.com/:_authToken" $FONTAWESOME_TOKEN 

RUN npm install
RUN npm run build-docker


FROM microsoft/dotnet:latest AS build
WORKDIR /app

RUN apt-get update 
RUN apt-get install zip net-tools -y

ARG AUTOMATICA_VERSION
ARG CLOUD_API_KEY
ARG CLOUD_URL

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

COPY --from=node /src/dist /app/automatica/wwwroot


RUN automatica-cli setversion $AUTOMATICA_VERSION -W /src/src/automatica.core/
RUN dotnet publish -c Release -o /app/automatica /src/src/automatica.core/ -r linux-x64

COPY ./src/automatica.core/Automatica.Core/appsettings.json /app/automatica/appsettings.json
RUN echo docker has some strange errors sometimes
COPY ./src/automatica.core/Automatica.Core/appsettings.json .

RUN mkdir -p /app/plugins
RUN echo $AUTOMATICA_VERSION
RUN automatica-cli InstallLatestPlugins -I /app/plugins -M $AUTOMATICA_VERSION -A $CLOUD_API_KEY -C  $CLOUD_URL

RUN rm -rf /src

FROM automaticacore/automatica-plugin-runtime:amd64 AS runtime
WORKDIR /app/

COPY --from=build /app/ ./

EXPOSE 1883/tcp
EXPOSE 5001/tcp	

# Build runtime image
ENTRYPOINT ["/app/automatica/Automatica.Core"]
#build libserialport
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS libnserial
WORKDIR /app

RUN apt-get -y update && apt-get -y upgrade
RUN apt-get -y install cmake build-essential 

RUN mkdir /src
RUN git clone https://github.com/jcurl/serialportstream.git /src/serialportstream
RUN cd /src/serialportstream/dll/serialunix && ./build.sh
RUN cd -


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


FROM mcr.microsoft.com/dotnet/sdk:6 AS build
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

COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so /usr/lib

COPY --from=node /src/dist /app/automatica/wwwroot


RUN automatica-cli setversion $AUTOMATICA_VERSION -W /src/src/automatica.core/
RUN dotnet publish -c Release -o /app/automatica /src/src/automatica.core/
RUN dotnet publish -c Release -o /app/automatica.bootloader /src/src/automatica.core/Automatica.Core.Bootloader

COPY ./src/automatica.core/Automatica.Core/appsettings.json /app/automatica/appsettings.json
RUN true
COPY ./src/automatica.core/Automatica.Core/appsettings.json .

RUN echo $AUTOMATICA_VERSION

RUN dotnet publish -c Release -o /tmp/db/ /src/src/automatica.core/CI/Automatica.Core.CI.CreateDatabase
RUN automatica-cli InstallLatestPlugins -I /app/automatica -M $AUTOMATICA_VERSION -A $CLOUD_API_KEY -C  $CLOUD_URL

COPY ./src/automatica.core/Automatica.Core/appsettings.json /tmp/db/appsettings.json

RUN dotnet /tmp/db/Automatica.Core.CI.CreateDatabase.dll /app/automatica


RUN rm -rf /src
RUN rm -rf /tmp/db 

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app/

RUN curl -s https://ngrok-agent.s3.amazonaws.com/ngrok.asc | \
  sudo tee /etc/apt/trusted.gpg.d/ngrok.asc >/dev/null && \
  echo "deb https://ngrok-agent.s3.amazonaws.com buster main" | \
  sudo tee /etc/apt/sources.list.d/ngrok.list && \
  sudo apt update && sudo apt install ngrok

COPY --from=build /app/ ./

EXPOSE 1883/tcp
EXPOSE 5001/tcp	

# Build runtime image
ENTRYPOINT ["dotnet", "/app/automatica/Automatica.Core.dll"]
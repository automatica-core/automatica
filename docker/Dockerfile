ARG version

#build libserialport
FROM microsoft/dotnet:sdk AS libnserial
WORKDIR /app
ARG version

RUN apt-get -y update && apt-get -y upgrade
RUN apt-get -y install cmake build-essential

RUN mkdir /src
RUN git clone https://github.com/jcurl/serialportstream.git /src/serialportstream
RUN cd /src/serialportstream/dll/serialunix && ./build.sh
RUN cd -


FROM microsoft/dotnet:latest
WORKDIR /app

RUN apt-get update 
RUN apt-get install zip -y
RUN apt-get install nginx -y

ARG version


#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so /usr/lib

RUN ls -lah /usr/lib

RUN dotnet publish -c Release -o /app/automatica /src/src/automatica.core/
RUN dotnet publish -c Release -o /app/automatica.bootloader /src/src/automatica.core/Automatica.Core.Bootloader

COPY ./src/automatica.core/Automatica.Core/appsettings.json /app/automatica/appsettings.json
RUN true
COPY ./src/automatica.core/Automatica.Core/appsettings.json .

RUN chmod 755 /src/docker/build-packages.sh
RUN /src/docker/build-packages.sh /src $version

RUN dotnet publish -c Release -o /tmp/db/ /src/src/automatica.core/CI/Automatica.Core.CI.CreateDatabase
COPY ./src/automatica.core/Automatica.Core/appsettings.json /tmp/db/appsettings.json

RUN dotnet /tmp/db/Automatica.Core.CI.CreateDatabase.dll /app/automatica

RUN rm -rf /src
RUN rm ./appsettings.json
RUN rm -rf /tmp/db 

# Build runtime image
ENTRYPOINT ["dotnet", "/app/automatica.bootloader/Automatica.Core.Bootloader.dll"]
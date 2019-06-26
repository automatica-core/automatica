FROM microsoft/dotnet:sdk AS libnserial

WORKDIR /app

RUN apt-get -y update && apt-get -y upgrade
RUN apt-get -y install cmake build-essential 

RUN mkdir /src
RUN git clone https://github.com/jcurl/serialportstream.git /src/serialportstream
RUN cd /src/serialportstream/dll/serialunix && ./build.sh
RUN cd -


FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS runtime
WORKDIR /app/

COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so.1 /usr/lib
COPY --from=libnserial /src/serialportstream/dll/serialunix/bin/usr/local/lib/libnserial.so /usr/lib
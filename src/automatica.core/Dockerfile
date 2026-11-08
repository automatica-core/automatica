FROM node:22-alpine AS node
WORKDIR /src

# Copy everything else and build
COPY ./Automatica.WebNew /src

RUN ls -lah

RUN npm install 
# DevExtreme license (optional): docker build --secret id=devexpress_license,src=<DevExpress_License.txt>
# Without it the bundle gets the TRIAL key (eval banner). Never pass it as a build-arg: those end up in the image history.
RUN --mount=type=secret,id=devexpress_license,target=/root/.config/DevExpress/DevExpress_License.txt npm run build-docker


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

RUN apt-get update 
RUN apt-get install zip net-tools -y

ARG VERSION
ARG CLOUD_URL
ARG AUTOMATICA_CLOUD_ENVIRONMENT
# The cloud API key is a BuildKit secret, never a build-arg (build args end up in build logs and
# image metadata):  docker build --secret id=cloud_api_key,env=CLOUD_API_KEY ...

#install automatica-cli
RUN dotnet tool install --global automatica-cli
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy everything else and build
COPY . /src

COPY --from=node /src/dist /app/automatica/wwwroot

RUN dotnet test /src/

RUN automatica-cli setversion $VERSION -W /src/
# no NuGet packing here: the solution publish runs projects in parallel and races on the .nupkg files
RUN dotnet publish -c Release -o /app/automatica /src/ -r linux-x64 --self-contained -p:GeneratePackageOnBuild=false

COPY ./Automatica.Core/appsettings.json /app/automatica/appsettings.json
RUN echo docker has some strange errors sometimes
COPY ./Automatica.Core/appsettings.json .

RUN mkdir -p /app/automatica/persistent/plugins
RUN echo $VERSION
RUN --mount=type=secret,id=cloud_api_key,required=true \
    automatica-cli InstallLatestPlugins -I /app/automatica/persistent/plugins -M $VERSION -A "$(cat /run/secrets/cloud_api_key)" -C  $CLOUD_URL -Cl $AUTOMATICA_CLOUD_ENVIRONMENT

RUN rm -rf /src

# frp for Remote Connect. Bump deliberately: version plus the sha256 from that release's
# frp_sha256_checksums.txt. Keep it compatible with the frps version on the cloud tunnel server.
ARG FRP_VERSION=0.72.0
ARG FRP_SHA256=daf719b32ca2778770bb92955e231a07414d754378b9af328aa2642862479804
RUN curl -fsSL -o frp.tgz https://github.com/fatedier/frp/releases/download/v${FRP_VERSION}/frp_${FRP_VERSION}_linux_amd64.tar.gz \
 && echo "${FRP_SHA256}  frp.tgz" | sha256sum -c - \
 && tar xzf frp.tgz \
 && mkdir -p frp \
 && mv frp_${FRP_VERSION}_linux_amd64/* frp/ \
 && rm -rf frp.tgz frp_${FRP_VERSION}_linux_amd64
RUN ./frp/frpc --version

# go2rtc for the camera live views (Go2RtcProcessHost starts it on 127.0.0.1:1984). Bump deliberately:
# version plus the sha256 of the asset (the "digest" of the release asset in the GitHub API).
ARG GO2RTC_VERSION=1.9.15
ARG GO2RTC_SHA256=20b46c6910734cb05ecefb234066f36868ff19b42ba1418a7a32e13a3e035712
RUN curl -fsSL -o /opt/go2rtc https://github.com/AlexxIT/go2rtc/releases/download/v${GO2RTC_VERSION}/go2rtc_linux_amd64 \
 && echo "${GO2RTC_SHA256}  /opt/go2rtc" | sha256sum -c - \
 && chmod 755 /opt/go2rtc
RUN /opt/go2rtc -version


# Runs as root on purpose. The supervisor starts this container privileged, with the host's
# docker.sock, /dev and /tmp (RuntimeSupervisor), and the watchdog replaces the files in /app in
# place on updates. With docker.sock inside, a non-root USER would not contain anything, and it
# would break updates on installed devices whose bind-mounted host directories are root-owned.
# Precondition for a USER here: the supervisor stops passing docker.sock and --privileged.
FROM automaticacore/automatica-plugin-runtime:amd64-10 AS runtime
WORKDIR /app/

ARG VERSION
ENV AUTOMATICA_VERSION=$VERSION

COPY --from=build /app/ ./
VOLUME /app/plugins

COPY --from=build /app/frp /app/frp
COPY --from=build /opt/go2rtc /usr/local/bin/go2rtc
COPY --from=build /app/ ./

RUN mkdir -p /app/automatica/frp
COPY ./Automatica.Core/frp/* /app/automatica/frp/

VOLUME /app/automatica/persistent
COPY --from=build /app/automatica/persistent/plugins /app/automatica/persistent/plugins

EXPOSE 1883/tcp
EXPOSE 5000-6000

# Build runtime image
ENTRYPOINT ["/app/automatica/Automatica.Core.Watchdog"]
ARG BRANCH

FROM automaticacore/automatica:latest-develop AS automatica
WORKDIR /app

RUN rm -f /app/automatica/automatica.core.db
RUN rm -f /app/automatica/automatica.core.init.db

COPY ./automatica.core.db /app/automatica/automatica.core.db
COPY ./automatica.core.db /app/automatica.core.db

COPY ./appsettings.json /app/automatica/appsettings.json
COPY ./appsettings.json /app/automatica/config/appsettings.json
COPY ./appsettings.json /app/appsettings.json

# The demo runs core on its own (no supervisor, no docker.sock, no devices), so it does not need
# root. BuildKit keeps the ownership change on the VOLUME paths of the base image.
RUN groupadd --system --gid 10001 automatica \
 && useradd --system --uid 10001 --gid automatica --home-dir /app --no-create-home --shell /usr/sbin/nologin automatica \
 && chown -R automatica:automatica /app
USER automatica

EXPOSE 5001/tcp
EXPOSE 5000/tcp

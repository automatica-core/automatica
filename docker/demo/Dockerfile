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

EXPOSE 5001/tcp
EXPOSE 5000/tcp
ARG BRANCH

FROM automaticacore/automatica:latest-$BRANCH AS automatica
WORKDIR /app

RUN cat /app/automatica/appsettings.json
RUN rm -f /app/automatica/automatica.core.db
RUN rm -f /app/automatica/automatica.core.init.db

COPY ./automatica.core.db /app/automatica/automatica.core.db
COPY ./automatica.core.db /app/automatica.core.db

EXPOSE 5001/tcp
EXPOSE 5000/tcp
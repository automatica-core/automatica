ARG BRANCH

FROM microsoft/dotnet:latest AS database
WORKDIR /app

COPY ./ /app
RUN ls /app/*

ARG BRANCH

FROM automaticacore/automaticacore:$BRANCH-latest AS automatica
WORKDIR /app

RUN cat /app/automatica/appsettings.json
RUN rm -f /app/automatica/automatica.core.db
RUN rm -f /app/automatica/automatica.core.init.db

COPY --from=database /app/automatica.core.db /app/automatica/automatica.core.db
COPY --from=database /app/automatica.core.db /app/automatica.core.db
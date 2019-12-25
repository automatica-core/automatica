# Quick reference
[Automatica.Core @ GitHub](https://github.com/automatica-core/automatica)

# How to use this image

## Test automatica
```console
docker run -it -p 5001:5001 automaticacore/automatica:latest-develop --name automatica
```

## Database
Automatica works with different database systems. Currently we support MySQL and SQlite.

The first startup can takeup some time, because the database needs to be initialized.

### SQLite
```console
$ docker run -it \
    -p 5001:5001 \
    --mount type=bind,source=./database,target=/app/database \
    -e DATABASE_TYPE="sqlite" \
    -e ConnectionStrings:"AutomaticaDatabaseSqlite=Data Source=/app/database/automatica.core.db" \
    --name automaticacore \
    automaticacore/automatica:latest-develop
```

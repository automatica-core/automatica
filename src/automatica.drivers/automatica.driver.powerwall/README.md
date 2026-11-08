# Tesla Powerwall

Tesla Powerwall 2 / Powerwall+ through the **local REST API of the Backup Gateway 2** on the LAN. This is a separate
driver from [Tesla](../automatica.driver.tesla), which talks to cars through the Fleet API.

```
Powerwall gateway        address, customer password, e-mail (optional), polling interval, allow writes, certificate fingerprint
├── Connected                       the last poll reached the gateway
├── Grid power [kW]                 + import, - export
├── Solar power [kW]
├── Battery power [kW]              + charging, - discharging
├── House consumption [kW]
├── State of charge [%]             app scale: (raw - 5) / 0.95
├── Grid status, Grid connected     SystemGridConnected / SystemIslandedActive / ...
├── Operation mode                  self_consumption / autonomous / backup   (write: see below)
├── Backup reserve [%]              app scale                                (write: see below)
└── Grid import / export, Solar, Battery charged / discharged, House consumption [kWh]   lifetime counters
```

The power signs match the energy monitor and the energy manager logic, so the values can be linked directly
(grid → `Pgrid`, battery → `Pbat`, SoC → `SoC`).

## API

| Request | Use |
|---|---|
| `POST /api/login/Basic` `{"username":"customer","password":…,"email":…}` | token, sent as `Authorization: Bearer`; on 401 the driver logs in again once |
| `GET /api/meters/aggregates` | powers (W) and energy counters (Wh) of site, solar, battery, load. Required: if it fails, *Connected* is false |
| `GET /api/system_status/soe` | raw SoC |
| `GET /api/system_status/grid_status` | grid status |
| `GET /api/operation` | mode and reserve (raw scale). A 404 (Powerwall 3) is remembered, the endpoint is not asked again until the driver restarts |
| `POST /api/operation` + `GET /api/config/completed` | write mode/reserve (only with *allow writes*) |

The gateway uses a self-signed certificate. The driver pins it: with an empty fingerprint the first certificate is
trusted and stored in the settings (trust on first use), afterwards only that certificate is accepted.

## Writing mode and reserve

Off by default (*allow writes*). Since 2024 the gateway firmware answers `POST /api/operation` with **403 for the
customer login** (pypowerwall: "LAN control requires v1r mode"); only installer logins or the Tesla cloud can change
it. The driver then logs an error and dispatches the gateway's values again. The local endpoint overwrites mode and
reserve together, so the driver reads both first and sends both. The reserve is converted from the app scale
(`raw = app * 0.95 + 5`).

## Not supported

* **TEDAPI** (`https://192.168.91.1/tedapi/...`, protobuf, gateway Wi-Fi password, only from the gateway's own
  Wi-Fi access point since firmware 25.10) and its extended data (vitals, strings, per-battery data).
* **Powerwall 3**: its wired LAN login only serves aggregates, SoE and grid status; those three work with this
  driver, mode and reserve do not exist.
* Control through the Tesla cloud/Fleet API (energy sites).

## Not verified

Only tested against a fake gateway built from pypowerwall and the community API docs. Not checked with a real gateway:
the login body (pypowerwall sends it form-encoded, the docs show JSON), the token lifetime, the certificate pinning
against the real certificate, and whether any current firmware still accepts the operation write.

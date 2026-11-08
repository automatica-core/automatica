# Miele

Miele@home appliances through the official [Miele 3rd Party API](https://www.miele.com/developer/).

```
Miele account          API URL, token URL, client id, client secret, refresh token, language, full-read interval
├── Login status
├── Event stream       connected
└── Appliance          device id (found by the scan)
    ├── Connected, Status (code + text), Program, Program id, Program phase
    ├── Power                 write true/false: powerOn / powerOff
    ├── Running, Paused       write: processAction start / stop / pause
    ├── Program id            write: start that program (programs endpoint)
    ├── Remaining time, Elapsed time [min], Start time [min]   start time write: delayed start
    ├── Temperature, Temperature 2, Core temperature [°C]
    ├── Target temperature, Target temperature 2 [°C]   write: fridge / freezer zones
    ├── Door open, Signal info, Signal failure, Remote control, Mobile start, Smart grid
    ├── Light                 write: on / off
    ├── Superfreezing, Supercooling   write
    ├── Ventilation step      write 0–4 (hoods)
    └── Spinning speed, Drying step, Energy / water consumption, Battery level
```

The scan only creates the values the appliance reports.

## Setup

1. Request API access at [miele.com/developer](https://www.miele.com/developer/) and note client id and client secret.
2. Get a refresh token for the Miele@home account with the authorization code flow of the client (the "Authorize"
   button of the Swagger page on the developer portal does this). Miele offers no device flow.
3. Enter client id, client secret and refresh token, start the driver and scan the account.

Miele moves its login to a new Keycloak server in 2026. The default token URL is the new one
(`https://auth.domestic.miele-iot.com/partner/realms/mcs/protocol/openid-connect/token`); the old
`/thirdparty/token` endpoint is being switched off. The refresh token is rotated and stored in a hidden property; a
token entered in the settings replaces the stored one. When the token is rejected, the login status asks for a new
one (there is no automatic re-login).

## Rate limits

Miele does not publish numbers but answers `503` / `429` when throttled; the driver then pauses for 5 minutes (or
`Retry-After`). Changes come through the event stream (`/v1/devices/all/events`), the full read runs at the
interval (default 30 min, minimum 5 min).

Temperatures are sent by Miele in 1/100 °C, `-32768` means "no value"; times as `[h, m]`.

## Not verified live

Built and tested against a fake Miele server only:

* the new Keycloak token endpoint with a real client,
* `processAction`, `programId` and `startTime` writes on real appliances (the allowed actions differ per model and
  state, the appliance must be in remote-start mode),
* the shape of `ident` events on the event stream.

# Husqvarna Automower

Husqvarna Automower robot mowers (with Automower Connect) through the official
[Automower Connect API](https://developer.husqvarnagroup.cloud/apis/automower-connect-api).

```
Husqvarna account      application key, application secret, full-read interval
├── Status, Live connection
└── Mower              mower id (found by the scan), default mowing duration [min]
    ├── Connected, Battery [%], Mode, Activity, State, Inactive reason, Error code, Error time, Next start
    ├── Override, Restriction
    ├── Mowing                write true: mow for the default duration, false: park until the next schedule
    ├── Mow for [min]         write minutes (0 = park until the next schedule)
    ├── Park for [min], Park until further notice, Pause, Resume schedule   (momentary)
    ├── Cutting height        write 1–9
    ├── Headlights            write ALWAYS_ON / ALWAYS_OFF / EVENING_ONLY / EVENING_AND_NIGHT
    └── Latitude, Longitude, Total cutting / running time [h], Blade usage [h], Charging cycles, Collisions
```

## Setup

1. Sign in at the [Husqvarna developer portal](https://developer.husqvarnagroup.cloud/) with the Husqvarna account
   of the mowers, create an application and connect it to the **Authentication API** and the
   **Automower Connect API**.
2. Enter application key and application secret. Scan the account and add the mowers.

The driver uses the client-credentials grant. Husqvarna allows only 3 valid tokens per application and user, so the
token is reused for its full hour (REST and web socket share it).

## Rate limits

The API allows **21000 requests a week** and 120 a minute per application. Changes arrive live over the web
socket (`wss://ws.openapi.husqvarna.dev/v1`, only the v2 events: battery, mower, planner, cutting height,
headlights, position); the driver sends `ping` every 60 s and reconnects before the 2 h limit. A full read
(`GET /mowers`, one request for all mowers) runs at the full-read interval (default 15 min, minimum 5 min) for the
statistics, which have no event. Over the quota the API answers `429` or `403` "Limit Exceeded"; the driver then
pauses for 15 minutes.

Times (error time, next start) are in the mower's local time, as Husqvarna sends them.

## Not verified live

Built and tested against a fake server only:

* the web socket with the real server (authorization header, ping, the exact v2 event payloads),
* actions and settings on a real mower (`202` = queued, the result arrives as an event),
* whether `403` over-quota answers always contain "limit" / "quota".

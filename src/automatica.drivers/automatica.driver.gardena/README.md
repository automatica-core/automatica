# Gardena smart system

Gardena smart system devices through the official
[Gardena smart system API v2](https://developer.husqvarnagroup.cloud/apis/gardena-smart-system-api).

```
Gardena account        application key, application secret, location id (optional, first location if empty)
├── Status, Live connection
├── Valve              service id (scan), default duration [min]
│   ├── Watering            write true: water for the default duration, false: stop until the next task
│   ├── Water for [min]     write minutes (0 = stop)
│   ├── Pause               write true: pause the schedule, false: resume it
│   └── Activity, State, Last error, Battery [%], Radio link
├── Mower (SILENO)     service id, default duration [min]
│   ├── Mowing              write true: mow for the default duration, false: park until the next task
│   ├── Mow for [min], Park until further notice, Resume schedule   (momentary)
│   └── Activity, State, Last error, Operating hours, Battery [%], Battery state, Radio link
├── Power socket       service id, default duration [min]
│   ├── On                  write true: on until further notice, false: off until the next task
│   ├── On for [min]
│   └── Activity, State, Radio link
└── Sensor             service id
    └── Soil humidity [%], Soil temperature, Ambient temperature [°C], Light [lx], Battery [%], Radio link
```

## Setup

1. Sign in at the [Husqvarna developer portal](https://developer.husqvarnagroup.cloud/) with the Gardena account,
   create an application and connect it to the **Authentication API** and the **GARDENA smart system API**.
2. Enter application key and application secret. Scan the account and add the services.

The driver uses the client-credentials grant; the access token (1 h) is reused until it expires.

## Rate limits

Gardena allows **700 requests a week** and 10 per 10 seconds per application. The driver therefore never polls:
it reads the location once at start and then keeps a web socket open (`POST /websocket`, ping every 150 s,
reconnect before the 2 h limit). Every change arrives as a JSON:API patch over the socket. Each reconnect costs one
request (the socket URL), each command one. On `429` the driver stops for the time in `Retry-After` (default 1 h).

Durations are sent in seconds and must be multiples of 60, so all durations are entered in minutes.

## Not verified live

Built and tested against a fake Gardena server only:

* the web socket with the real server (keep-alive, closing after inactivity, reconnect),
* commands on real devices (accepted with `202`, the result arrives over the socket),
* the `rfLinkState` / `batteryLevel` values of each device type.

# Segway Navimow

Segway Navimow robot mowers through the Navimow smart-home cloud (EU region).

> **No public API documentation exists.** Segway publishes no API reference and no way for third parties to
> register a client. This driver follows the contract of Segway's own open-source code: the official Home Assistant
> integration [segwaynavimow/NavimowHA](https://github.com/segwaynavimow/NavimowHA) and
> [navimow-sdk](https://github.com/segwaynavimow/navimow-sdk). It needs an OAuth client issued by Segway for
> Automatica; the client id and secret embedded in the Home Assistant integration must not be used.

```
Navimow account        client id, client secret, redirect URI, authorization code / refresh token, poll interval
├── Status             OK, the login URL when a login is needed, or the last error
└── Mower              device id (found by the scan)
    ├── Online, State (isDocked, isRunning, isPaused, isDocking, isLifted, Error, Offline, …)
    ├── Mowing                write true: start (StartStop on), false: return to the dock
    ├── Paused                write true: pause, false: resume (PauseUnpause)
    ├── Return to dock, Stop   (momentary: write true)
    └── Docked, Returning, Error, Lifted, Battery [%], Firmware
```

## Setup

1. Ask Segway for an OAuth client for Automatica (the PyPI page of navimow-sdk names `segwaynavimowapp@gmail.com`).
   Note client id, client secret and the registered redirect URI.
2. Enter them and start the driver. The **Status** shows a login URL
   (`https://navimow-h5-fra.willand.com/smartHome/login?response_type=code&client_id=…`).
   Open it, sign in with the Navimow account and copy the `code` parameter from the address the browser is sent to
   (the page itself may not load).
3. Paste it as **Authorization code**. The driver exchanges it once; afterwards it refreshes the token itself if
   Segway issued a refresh token. Alternatively enter a refresh token directly.
4. Scan the account and add the mowers.

The tokens are stored in a hidden property. A newly entered code or refresh token replaces them. According to
Segway's code the first token may come without a refresh token; then a new login is needed when it expires
(about 1–2 days), and the status says so.

## Polling

One status request for all mowers per interval (default 60 s, 30–3600 s), the device list once an hour, and a status
read 5 s after each command. Segway documents no limits; Loxone polls every 50 s. When the cloud answers `429` or
"too frequent", the driver pauses for 5 minutes.

The Navimow cloud also offers MQTT (credentials from `/openapi/mqtt/userInfo/get/v2`); the driver does not use it
because its contract is undocumented and marked as work in progress in Segway's SDK.

## Not verified live

Nothing was tested against the real cloud (no client available):

* the token endpoint with a non-Home-Assistant client (`channel` parameter of the login page, redirect URI handling,
  whether a refresh token is issued, plain or `{code,data}`-wrapped token response — both are accepted),
* the status fields beyond `vehicleState` and `capacityRemaining`,
* command results (`alreadyInState` is treated as success),
* rate limits.

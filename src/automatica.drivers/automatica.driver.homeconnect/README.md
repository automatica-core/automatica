# Home Connect

Bosch, Siemens, Neff, Gaggenau (BSH) appliances through the official
[Home Connect API](https://api-docs.home-connect.com/).

```
Home Connect account   API URL, client id, client secret, refresh token (optional), full-read interval
├── Login status       OK, or the link + code of a pending login
├── Event stream       connected
└── Appliance          haId (found by the scan)
    ├── Connected, Operation state, Door state, Remote control / remote start allowed, Local control active
    ├── Power                 write true/false: On / Off (Standby if Off is not supported)
    ├── Active program        write a program key: start it, empty: stop the program
    ├── Selected program      write a program key: select it
    ├── Running               write true: start the selected program with its options, false: stop the program
    ├── Paused                write: BSH.Common.Command.PauseProgram / ResumeProgram
    ├── Remaining time [s], Elapsed time [s], Progress [%], Last event
    └── Custom number / switch / text   any status, setting, option or command key of the appliance
```

## Setup

1. Register at the [Home Connect developer portal](https://developer.home-connect.com/) and create an application
   with **OAuth flow "Device Flow"**. Note the client id and the client secret.
2. Enter client id and client secret and start the driver. The **Login status** shows a link and a code:
   open the link, sign in with the Home Connect account of the appliances and confirm the code.
3. Scan the account and add the appliances.

The driver requests the scopes `IdentifyAppliance Monitor Control Settings`. The refresh token is rotated by Home
Connect; the newest one is stored in a hidden property. A refresh token entered in the settings replaces the stored
one. When the login is revoked, the driver starts a new device login and shows it in the login status.

Remote start has to be enabled on the appliance (the *Remote start allowed* value) before programs can be started.

## Rate limits

Home Connect allows 1000 requests a day and 50 a minute per user. Changes arrive through the server-sent event
stream (`/api/homeappliances/events`), so the driver reads everything once at start and then only at the full-read
interval (default 1 h, minimum 10 min). Appliances are re-read at most once a minute after an event that needs it.
On `429` the driver stops sending requests for the time in `Retry-After`.

## Custom values

`Custom number`, `Custom switch` and `Custom text` take any key in their **key** property:

* status and setting keys (`BSH.Common.Status.…`, `…Setting.…`) are read, settings are written,
* option keys (`….Option.…`) write the option of the active program,
* command keys (`….Command.…`) are executed when `true` is written.

## Not verified live

Built and tested against a fake Home Connect server only, from the published documentation:

* device flow login and token rotation against the real identity server,
* program start with options for each appliance type (keys and allowed options differ per model),
* the Standby fallback for appliances that do not support Off.

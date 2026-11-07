# Tesla

Tesla vehicles through the official [Tesla Fleet API](https://developer.tesla.com/docs/fleet-api).

```
Tesla account          Fleet API URL (region), client id, refresh token, command proxy URL, polling interval
└── Vehicle            VIN
    ├── State                 online / asleep / offline
    ├── Battery level [%], Range [km], Charging state, Charging power [kW], Time to full [h]
    ├── Charging              write true/false: charge_start / charge_stop
    ├── Charge limit [%]      write: set_charge_limit
    ├── Charge port open      write: charge_port_door_open / close
    ├── Inside / outside temperature [°C]
    ├── Climate               write: auto_conditioning_start / stop
    ├── Target temperature    write: set_temps
    ├── Locked                write: door_lock / door_unlock
    ├── Sentry mode           write: set_sentry_mode
    ├── Odometer [km], Latitude, Longitude, Gear
    └── Wake up, Flash lights, Honk (momentary: write true)
```

## Setup

1. Register an application on [developer.tesla.com](https://developer.tesla.com) (Fleet API access, scopes
   `openid offline_access vehicle_device_data vehicle_cmds vehicle_charging_cmds`) and complete the partner registration
   for your region.
2. Get a refresh token for your account (authorization code flow of your app).
3. Enter the client id and the refresh token. Pick the Fleet API URL of your region (EU is the default).

Tesla replaces the refresh token on every refresh. The driver stores the newest one in a hidden property; if you
enter a new token in the settings, the stored one is discarded.

## Polling and costs

Every poll asks for the vehicle state first (`GET /api/1/vehicles/{vin}`). Vehicle data is only requested while the
car is online, so polling never wakes the car or keeps it awake. Tesla bills Fleet API calls; the default interval is
5 minutes.

## Commands

Commands wake the car if it is asleep (up to 45 s) and read the vehicle again afterwards.
Vehicles built since 2021 only accept commands signed with the
[Vehicle Command Protocol](https://github.com/teslamotors/vehicle-command). Run Tesla's `tesla-http-proxy` with your
app's key and set its URL as **command proxy URL**. Without the proxy, these cars reject commands
(`vehicle_command_protocol_required`, logged as an error); reading data works either way.

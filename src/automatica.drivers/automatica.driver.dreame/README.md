# Dreame

Dreame robot vacuums through the **Dreamehome** cloud (the account of the Dreamehome app).

```
Dreame account     region (eu, us, cn, ...), user name, password, polling interval, "Find vacuums"
└── Vacuum         device id (filled in by the scan)
    ├── State (+ text), Error code, Battery [%], Charging status, Task status
    ├── Cleaning time [min], Cleaned area [m²]
    ├── Suction level, Water volume        writable
    └── Start, Pause, Stop, Return to dock, Locate   (momentary: write true)
```

## Protocol

The Dreamehome app API is not public. The driver follows the protocol of the Home Assistant integration
[Tasshack/dreame-vacuum](https://github.com/Tasshack/dreame-vacuum) (`protocol.py`, `types.py`):

- Login: `POST https://{region}.iot.dreame.tech:13267/dreame-auth/oauth/token`, form body, password = `md5(password + salt)`,
  refresh token afterwards.
- Requests are signed: `sign = md5(sorted parameters + epoch ms + key)`.
- Device list: `dreame-user-iot/iotuserbind/device/listV2`; the device's `bindDomain` (`device/info`) selects the command
  service `dreame-iot-com-{first label}/device/sendCommand`.
- Data and commands are MIoT `get_properties` / `set_properties` / `action` with siid/piid/aiid:
  state 2.1, error 2.2, battery 3.1, charging 3.2, status 4.1, time 4.2, area 4.3, suction 4.4, water 4.5;
  start 2.1, pause 2.2, return to dock 3.1, stop 4.2, locate 7.1.

The values are polled (default every 60 s); the push channel (MQTT) of the app is not used.

## Not verified

The driver was tested against a fake server only, there was no real account or robot. Points to check first with a
real account:
- The Python integration uses its own TLS stack for the cloud; if the login fails with a TLS or HTTP 403 error, the
  server rejects .NET's TLS fingerprint.
- Models whose MIoT mapping differs from the common one (siid/piid) report wrong or no values for some data points.
- Mova and Trouver accounts (sister brands with their own hosts and keys) are not supported.

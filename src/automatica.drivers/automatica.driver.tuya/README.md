# Tuya

Tuya Wi-Fi smart plugs with power meter (LSC Smart Connect from Action, Smart Life, Tuya Smart and other brands on the
Tuya platform) through the **local protocol 3.3 / 3.4** on TCP port 6668. No cloud while running.

```
Tuya devices              search devices
└── Smart plug                      address, device id, local key, protocol version, polling interval
    ├── Connected                   the local session is open
    ├── Switch                      on / off, can be written
    ├── Power [W], Voltage [V], Current [A]
    ├── Energy added [kWh]          DP 17
    ├── Countdown [s]               can be written
    └── State after power loss      off / on / memory
```

The plug sends its new status by itself when it is switched in the app or with its button; the driver keeps the session
open and asks for the status every polling interval (default 10 s), which also keeps the session alive. A plug takes
**one local session at a time**: while the driver is connected, other local clients (Home Assistant localtuya,
tinytuya) cannot connect. The app keeps working through the cloud.

## Local key

Every plug encrypts with its own 16 character **local key**, which only the Tuya cloud hands out:

1. Account on [platform.tuya.com](https://platform.tuya.com), cloud project (Smart Home, the data center of the app
   account, e.g. Central Europe) with the **IoT Core** service active (the free trial has to be extended now and then).
2. Project → Devices → Link App Account → scan the QR code with the **Tuya Smart** or **Smart Life** app. Brand apps such
   as LSC Smart Connect refuse the code ("use the designated app"): remove the plugs from that app and pair them in Tuya
   Smart / Smart Life instead.
3. API Explorer → Device Management → *Query Device Details in Bulk* with the device ids → field `local_key`.

The key changes whenever the plug is paired again.

## Search

*Search devices* listens about 12 s for the UDP broadcasts of the devices (port 6666 plain, 6667 encrypted with the
fixed key md5("yGAdlopoPVldABfn")) and adds the ones not configured yet with address, device id and protocol version;
enter the local key afterwards. Automatica has to see the broadcasts (host network in Docker).

## Protocol

| | 3.3 | 3.4 |
|---|---|---|
| Encryption | AES-128-ECB with the local key | AES-128-ECB with a session key |
| Session | none | local nonce → device nonce + HMAC → HMAC; key = AES(local key, nonce XOR nonce) |
| Integrity | CRC32 | HMAC-SHA256 |
| Status query | cmd 10 `{gwId, devId, uid, t}` | cmd 16 `{}` |
| Set | cmd 7 `{devId, uid, t, dps}` | cmd 13 `{protocol: 5, t, data: {dps}}` |

Protocol 3.5 (AES-GCM, newer firmware) and 3.1 are not supported yet.

DPs of the LSC Power Plug w/ Power Meter EU (product 5s0ygsmd6hcirsog): 1 switch, 9 countdown s, 17 energy added (Wh),
18 current mA, 19 power 0.1 W, 20 voltage 0.1 V, 21–25 calibration, 26 fault, 38 state after power loss, 40 LED mode.

## Checked

- Protocol 3.4, status and session against two real LSC Power Plugs w/ Power Meter EU (2026-11-14).
- Switching, 3.3 and the energy counter only against the fake device in the tests. DP 17 is taken as Wh; on some plugs
  it restarts, so link it to a meter logic for a running total.

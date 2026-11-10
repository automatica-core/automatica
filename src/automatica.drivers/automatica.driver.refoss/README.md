# Refoss energy monitor

Refoss **EM16P** (18 CT channels) and **EM06P** (6 channels) through the **local JSON-RPC API** on the LAN. No cloud.

```
Refoss energy monitor     address, password (optional), polling interval, search channels
├── Connected                       the last poll reached the device
└── Channel (A1 … C6)               channel number 1–18, found by the scan
    ├── Power [W]                   active power, negative when energy is fed back
    ├── Current [A], Voltage [V], Power factor
    ├── Energy today / this week / this month [kWh]
    └── Returned energy today / this week / this month [kWh]
```

EM16P channel numbers: A1–A6 = 1–6 (black lead), B1–B6 = 7–12 (red), C1–C6 = 13–18 (blue). Each letter is one
phase: the channel shows the voltage of its phase. A1 and B1 take the 200 A CTs, the others 60 A.

The scan (*Search channels*) reads `Em.Config.Get` and adds the channels that are not configured yet, named
`A1 Heat pump` after the names set in the Refoss app (or `A1` without one). Merged channels (`Em.Chmerge.*`, set up in
the app) appear in the scan with the id the device reports for them.

## Energy totals

The device only has **day, week and month counters** that restart at 0; there is no lifetime counter. For a running
total, link *Energy this month* (or *today*) to a **meter** logic (`metering.meter`): it takes a falling value as the
new start and counts every increase, so the total keeps growing across month ends. Turn on the history of the meter
output, not of the restarting counter.

## API

| Request | Use |
|---|---|
| `GET /rpc/Em.Status.Get?id=65535` | every poll: power, current, voltage, pf and the energy counters of all channels in one call |
| `GET /rpc/Em.Status.Get?id=<n>` | fallback when a firmware refuses `id=65535` (an RPC error, not 401): every configured channel is asked on its own until the driver restarts |
| `GET /rpc/Em.Config.Get?id=65535` | scan: the channel names |

Responses are accepted as `{"status":[…]}`, wrapped in `{"result":…}`, or as the component map of
`Refoss.Status.Get` (`{"em:1":{…}}`). The examples of the Refoss documentation contain trailing commas, the parser
accepts them.

**Authentication** (when switched on with `Refoss.Auth.Set`): the device answers 401, the driver repeats the request
with an SHA-256 HTTP digest, user `admin`, realm = device id (the Shelly Gen2 scheme). The challenge is taken from the
`WWW-Authenticate` header or from the JSON-RPC error message `{"auth_type":"digest","nonce":…,"realm":…}`. The nonce
is reused with a growing nonce count; an expired nonce is replaced once, a second refusal is reported as a wrong
password and *Connected* goes false.

## Not verified

Built from the Refoss open API documentation and the Home Assistant integration (`refoss_rpc` / `aiorefoss`), tested
against a fake device only. To check on a real EM16P: that `Em.Status.Get` accepts `id=65535` over HTTP GET, the exact
response shape (list vs. wrapped), how authentication is announced on HTTP GET (header or JSON body), and the ids of
merged channels.

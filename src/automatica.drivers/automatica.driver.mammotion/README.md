# Mammotion

Mammotion robot mowers (Luba, Luba 2, Yuka) through the **Mammotion** cloud (the account of the Mammotion app).

```
Mammotion account  user name, password, report interval, "Find mowers"
└── Mower          IoT id (filled in by the scan; RTK base stations are left out)
    ├── Online, Battery [%], State (+ text), Charge state
    ├── Job progress [%], Job area [m²], Remaining time [min], Cutting height [mm]
    └── Start job, Pause, Resume, Cancel job, Return to dock   (momentary: write true)
```

## Protocol

The Mammotion API is not public. The driver follows [PyMammotion](https://github.com/mikey0000/PyMammotion), the library
behind the Home Assistant integration:

- Login: `POST https://id.mammotion.com/oauth2/token` with the parameters in the query string (password base64 encoded,
  plain as fallback), signed with `Ma-Signature` = HMAC-SHA256 over app key + timestamp + endpoint + JSON of the
  parameters. Refresh token afterwards.
- The `iot` claim of the access token is the IoT API: `v1/user/device/page` (devices), `v1/mqtt/auth/jwt` (MQTT
  credentials) and `v1/mqtt/rpc/thing/service/invoke` (commands).
- Commands and report requests are protobuf `LubaMsg` messages (base64 in the invoke call): `NavTaskCtrl` action
  1 start, 2 pause, 3 resume, 4 cancel, 5 return to dock; `report_cfg` asks the mower for continuous reports.
- State comes in over MQTT (`/sys/{pk}/{dn}/thing/event/+/post`, `/sys/proto/...`, `.../app/down/thing/status`) as
  protobuf `LubaMsg` (`report_data`, `mow_info`, `work_state`, `batinfo`) or JSON with a base64 `content`.
- Job progress/area and remaining time are split from `report_data.work.area` / `.progress` (high/low 16 bits) as the
  Home Assistant integration does.

Every report interval (default 60 s) the driver asks for new reports and reconnects when the MQTT connection is gone.

"Start job" starts the job planned in the app; planning areas and routes is not part of the driver.

## Not verified

The driver was tested against a fake server and a fake MQTT broker only, there was no real account or mower. Points to
check first with a real account:
- Login: the app version header (`App-Version`) is a placeholder; if the login is refused, compare the headers with the
  current app.
- The MQTT broker address format of `v1/mqtt/auth/jwt` (host with or without port/scheme; default TLS on 8883).
- Whether the mower reacts to report requests sent with an empty product key/device name (as PyMammotion does).
- The protobuf field numbers come from PyMammotion's `.proto` files; firmware updates may move fields.

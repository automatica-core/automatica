# BYD

BYD cars through the cloud of the **BYD app** (overseas markets, e.g. Europe).

```
BYD account     user name, password, control PIN, country, polling interval, API URL, "Find cars"
└── Car         VIN (filled in by the scan), climate temperature, climate duration
    ├── Online, Battery [%], Range [km], Odometer [km], Charging, Inside temperature, Speed
    ├── Latitude, Longitude
    ├── Locked        writable: true locks, false unlocks
    ├── Climate       writable: true starts (temperature/duration of the car), false stops
    └── Flash lights, Find car   (momentary: write true)
```

Commands need the 6-digit remote control PIN that is set in the BYD app.

## Protocol

The BYD app API is not public. The driver follows [pyBYD](https://github.com/jkaberg/pyBYD) (MIT):

- Every request is `POST {"request": "F" + base64(...)}` to `https://dilinkappoversea-eu.byd.auto`; the outer layer is a
  white-box AES ("Bangcle") whose lookup tables come from the app's `libencrypt.so`.
- Inside is a signed JSON payload (`sign` = mixed-case SHA1 over the sorted fields, `checkcode` = reordered MD5); its
  `encryData` is AES-128-CBC encrypted, at login with `md5(md5(password))`, afterwards with `md5(encryToken)` of the session.
- Data is requested with a trigger call and then polled with the returned `requestSerial`
  (`vehicleRealTimeRequest/Result`, `getGpsInfo/Result`, `remoteControl/Result`).
- Commands: `LOCKDOOR`, `OPENDOOR`, `OPENAIR` (with `controlParamsMap`), `CLOSEAIR`, `FLASHLIGHTNOWHISTLE`, `FINDCAR`.

Every poll asks the car for fresh data (default every 10 minutes); keep the interval long to spare the 12 V battery. The
MQTT push channel of the app is not used.

## Third-party data

`P3.Driver.Byd.Driver/Resources/bangcle_tables.bin` is taken unchanged from pyBYD
(`src/pybyd/data/bangcle_tables.bin`, commit 995ef70a, SHA-256 `5d7577f6…a1c99a1f`):

```
MIT License
Copyright (c) 2026 Joel Kåberg
```

`Crypto/Bangcle.cs` and `Crypto/BydCrypto.cs` are ports of pyBYD's `_crypto` package under the same license.

## Not verified

The driver was tested against a fake server only, there was no real account or car. The white-box cipher is checked
by round trips (encryption and decryption use separate tables), not against captured app traffic. Points to check first
with a real account:
- Login: the server may reject the `appName` ("Automatica+1.0.0") or the device profile (pyBYD's defaults).
- Accounts outside Europe need another API URL.
- Hybrid cars: the `energyType` of the vehicle list is passed through; whether range values for the combustion engine
  arrive is untested.

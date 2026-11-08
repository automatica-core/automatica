# BMW CarData

BMW and MINI vehicles through the official [BMW CarData customer API](https://bmw-cardata.bmwgroup.com/customer/public/api-documentation).
CarData is **read only**: there are no commands (no lock, climate or charging control).

```
BMW CarData client     client id, poll interval, live values (stream) on/off, stream host, container id (optional)
├── Login status       OK, or the link + code of a pending login
├── Stream connected
└── Vehicle            VIN (found by the scan)
    ├── State of charge [%], Target state of charge [%], Electric range, Range, Mileage
    ├── Charging status, Charging time remaining [min], Charging port
    ├── Latitude, Longitude, Last update (UTC)
    └── Custom number / text   any key of the BMW Telematics Data Catalogue
```

## Setup

Requirements: EU vehicle with an active ConnectedDrive contract, mapped to the My BMW account as **primary** user.

1. In the My BMW (or MINI) portal open **CarData**, click **Create CarData Client** and copy the client id.
2. Subscribe the client to **CarData API** and, for live values, to **CarData Stream** — before logging in.
3. Enter the client id and start the driver. The **Login status** shows a link and a code: open the link, sign in
   and confirm. The driver uses the device code flow with PKCE (no client secret).
4. Scan the account and add the vehicles.

The refresh token lives 14 days and is replaced on every refresh; the newest one is stored in a hidden property.
If Automatica is offline longer than that, the driver starts a new login. A different client id discards the
stored login.

BMW's FAQ for "Invalid Access Token" / "user has declined authorization": delete the client in the portal, wait
30 s, create a new one, subscribe it again and log in again.

## Container and request budget

CarData returns values per *container* (a list of telematic keys). The driver creates one named `Automatica` with
the keys of all data points and custom values, and replaces it when the keys change (BMW allows 10 containers per
account; leftovers named `Automatica` are removed when the limit is reached). An existing container can be
configured instead.

BMW allows **50 requests a day** (reset 00:00 UTC). Each poll costs one request per vehicle, so the interval is
stretched to fit 40 polls a day (1 vehicle: at least 36 min, 3 vehicles: 108 min); the rest is left for login,
container and scan calls. The driver also counts its requests and stops at 50 itself; on `CU-429` it waits until
midnight UTC.

## Live values (stream)

With **Live values** on, the driver connects to the CarData MQTT stream (TLS, user name = gcid, password = id
token, topic `{gcid}/{VIN}`, QoS 0) and reconnects with a fresh id token every 50 minutes (it expires after 1 h).
BMW allows one connection per account. The keys in the stream are chosen in the portal (**Configure data stream**),
not by the driver. The default host `customer.streaming-cardata.bmwgroup.com:9000` is the one shown by the portal
to other integrations; use the host the portal shows you.

## Data point keys

The location keys are from BMW's integration guide; the other default keys are those used by evcc's CarData
integration, not from BMW's catalogue (which could not be read). If a vehicle reports a value under a different key,
add it as a custom value.

## Not verified live

Built and tested against a fake server only:

* the device code flow with a real client, token rotation, `gcid` in the token response,
* container creation (an unknown key makes BMW reject the whole container with `CU-402`; the status shows it),
* the default telematic keys for each vehicle type,
* the MQTT host/port and the message format (not documented by BMW; the driver reads
  `{vin, data: {key: {value, unit, timestamp}}}` as evcc does).

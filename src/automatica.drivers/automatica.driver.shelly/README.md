# Shelly driver

Automatica driver for Shelly devices over the local network (no cloud). Driver version **0.3.0.0**.

- **Gen2 / Gen3 / Gen4** (Plus, Pro, Mini, Gen3, Gen4, Wall Display, BLU gateways): JSON-RPC 2.0.
  - `Shelly.GetDeviceInfo`, `Shelly.GetStatus` and `Shelly.GetConfig` over `POST /rpc`.
  - SHA-256 digest auth. The user is always `admin`; the realm is the device id.
  - Live updates over the device websocket (`NotifyStatus`, `NotifyFullStatus`, `NotifyEvent`), with a slow poll (60 s) as fallback.
  - Battery devices report through the driver's **outbound websocket server**.
- **Gen1**: REST.
  - `GET /shelly`, `/status`, `/settings` and the control URLs (`/relay`, `/roller`, `/light`, `/color`, `/white`, `/thermostats`).
  - HTTP basic auth.
  - Push updates via **CoIoT** (CoAP over UDP 5683, multicast `224.0.1.187`) plus the `/status` poll (5 s).
  - Battery devices report via CoIoT, mapped with the `/cit/d` description stored during the scan.
- **Discovery**: mDNS (`_shelly._tcp` for Gen2+; `_http._tcp` with a `shelly*` host name for Gen1), plus a list of IP addresses on the driver node.
- **Learn mode**: battery devices that wake up while the driver runs are learned. Learn mode reports them, and the next scan adds them.

## How a device is built (component based)

A scan reads every device it finds (`/shelly`, then `Shelly.GetStatus` + `Shelly.GetConfig`, or Gen1 `/status` + `/settings` + `/cit/d`). It then creates:

```
Shelly (driver)
└─ Shelly Gen2+ device | Shelly Gen1 device    (properties: id = MAC, IP, password, polling interval, sleeping, model, generation)
   └─ component per instance                   (switch:0, cover:0, em:0, input:2, bthomesensor:201 … / Gen1 relays:0, meters:1, sensors …)
      └─ data points the device actually reports (output, power, energy, position, temperature, battery, event …)
```

- Component and data point templates come from one catalog: [`Catalog/ShellyRpcCatalog.cs`](Automatica.Driver.ShellyFactory/Catalog/ShellyRpcCatalog.cs) and [`Catalog/ShellyGen1Catalog.cs`](Automatica.Driver.ShellyFactory/Catalog/ShellyGen1Catalog.cs).
- Their ids are stable name-based GUIDs (namespace + component type + key).
- Only data points present in the device status are created. For example, a Plus 1 gets no power data points, and a Plus 2PM in cover profile gets no switches.
- Names come from the device config (`switch:0.name`, Gen1 `relays[0].name`, BLU device names).
- New hardware with known component types works without a driver change.

### Component coverage (what can be read and written)

| Component (Gen2+) | Data points (R = read, W = write, C = command, E = event) | Write method |
|---|---|---|
| `switch` | output RW, toggle C, power, voltage, current, power factor, frequency, energy, returned energy, device temperature | `Switch.Set`, `Switch.Toggle` |
| `cover` | state, position RW, target position, slat position RW, open/close/stop/calibrate C, metering | `Cover.GoToPosition`, `Cover.Open/Close/Stop/Calibrate` |
| `light` | output RW, brightness RW, metering | `Light.Set` |
| `rgb`, `rgbw`, `rgbcct` | output RW, brightness RW, red/green/blue RW, white RW (rgbw), color temperature RW (rgbcct), metering | `RGB.Set`, `RGBW.Set`, `RGBCCT.Set` (the other colour channels come from the last status) |
| `cct` | output RW, brightness RW, color temperature RW, metering | `CCT.Set` |
| `em` + `emdata` | per phase: active/apparent power, voltage, current, pf, frequency, energy, returned energy; neutral current, totals | read only |
| `em1` + `em1data`, `pm1` | active/apparent power, voltage, current, pf, frequency, energy, returned energy | read only |
| `input` | state (switch type), event E (button type), percent / value (analog), counts + frequency (count) | – |
| `temperature`, `humidity`, `illuminance`, `voltmeter`, `devicepower` | °C, %RH, lux + illumination, voltage, battery %, battery V, external power | read only |
| `smoke` | alarm, muted, mute C | `Smoke.Mute` |
| `flood` | alarm, muted | read only (the API has no `Flood.Mute`) |
| `presencezone` (`presence` when it reports `value`) | presence, number of objects | read only |
| `thermostat` (Wall Display) | enable RW, target temperature RW, current temperature, output | `Thermostat.SetConfig` |
| `cb` (Pro 1-4CB) | output RW, switching cycles, device temperature | `CB.Set` |
| `bthomedevice` (BLU via gateway) | battery, RSSI, event E (button pushes, fw ≥ 1.6) | – |
| `bthomesensor` (BLU via gateway) | value (numeric objects) or state (binary objects), named after the BLU device and the BTHome object | – |
| `blutrv` (BLU Gateway Gen3) | current temperature, target temperature RW, valve position, battery, RSSI | `BluTrv.Call` → `TRV.SetTarget` |
| `number`, `boolean`, `text`, `enum` (virtual, id 200+) | value RW | `Number.Set`, `Boolean.Set`, `Text.Set`, `Enum.Set` |
| `button` (virtual) | trigger C, event E | `Button.Trigger` |
| `sys`, `wifi` | update available, restart required, uptime, reboot C, RSSI | `Shelly.Reboot` |

| Section (Gen1 `/status`) | Data points | Write URL |
|---|---|---|
| `relays[]` | output RW, timer running, overpower | `/relay/N?turn=on|off` |
| `rollers[]` | state, position RW, open/close/stop C, power, stop reason | `/roller/N?go=to_pos&roller_pos=`, `?go=open|close|stop` |
| `meters[]`, `emeters[]` | power, energy (Wh; Gen1 meters count Wmin, converted), overpower; emeters: reactive power, pf, voltage, current, returned energy | read only |
| `lights[]` | output, brightness, gain, red, green, blue, white, color temperature (all RW), power, overpower | RGBW2: `/color/N` (color mode) or `/white/N` (white mode). Dimmer, Bulb, Duo, Vintage: `/light/N` |
| `inputs[]` | state, event (S, L, SS…), event count | – |
| `adcs[]`, `ext_temperature`, `ext_humidity`, `ext_switch` | voltage, add-on temperature / humidity / input | read only |
| `thermostats[]` (TRV) | temperature, target temperature RW, valve position RW, window open | `/thermostats/N?target_t=`, `?pos=` |
| root sensors | temperature, humidity, lux + illumination, door open, motion, vibration, tilt, flood, smoke, gas alarm + concentration, battery %, battery V, charger | read only |
| root device | update available, RSSI, device temperature, overtemperature, supply voltage, reboot C | `/reboot` |

## Device coverage matrix

The inventory was built from shelly-api-docs.shelly.cloud and kb.shelly.cloud on 2026-11-08.
- **Model ids** come from the KB ("Device identification").
- **Components** come from the API pages.
- *(mem)* marks values that are not printed in the official docs.

**Legend**
- ✅ Supported: every documented component is mapped.
- ◐ Partial: the main function works; the listed parts do not.
- ❌ Out of scope.

**Hardware testing:** nothing in this table has been run against real hardware. The tests replay recorded responses (see below).

### Gen1 (REST + CoIoT)

| Device | Type id | Components used | Status |
|---|---|---|---|
| Shelly 1 / 1PM / 1L | `SHSW-1` / `SHSW-PM` / `SHSW-L` | relays, meters (PM), inputs, device temperature, Sensor Add-on (ext_temperature/humidity/switch) | ✅ |
| Shelly 2 / 2.5 | `SHSW-21` / `SHSW-25` | relay mode: relays; roller mode: rollers; meters, inputs | ✅ (mode read during the scan; re-scan after changing the mode) |
| Shelly 4Pro | `SHSW-44` *(mem)* | relays, meters | ✅ |
| Plug / Plug E / Plug S / Plug US | `SHPLG-1` / `SHPLG2-1` / `SHPLG-S` / `SHPLG-U1` | relays, meters, device temperature | ✅ |
| i3 | `SHIX3-1` | inputs (events) | ✅ |
| Button1 | `SHBTN-1` (v2 `SHBTN-2` *(mem)*) | inputs (events), battery, charger; sleeping | ✅ |
| Bulb, Bulb RGBW/Duo RGBW, Vintage, Duo | `SHBLB-1`, `SHCB-1`, `SHVIN-1`, `SHBDUO-1` | lights (`/light/0`), meters | ✅ (effects not mapped) |
| RGBW2 | `SHRGBW2` *(mem)* | color mode: 1 RGBW light (`/color/0`); white mode: 4 lights (`/white/N`); meters, inputs | ✅ |
| Dimmer / Dimmer 2 | `SHDM-1` / `SHDM-2` *(mem)* | lights, meters, inputs, device temperature | ◐ (calibration not mapped) |
| H&T, Smoke, Flood, Door/Window 1/2 | `SHHT-1`, `SHSM-01`, `SHWT-1`, `SHDW-1`, `SHDW-2` | root sensors, battery; sleeping (CoIoT) | ✅ |
| Motion / Motion 2 | `SHMOS-01` / `SHMOS-02` | motion, vibration, lux, battery, charger (WiFi stays on) | ✅ |
| TRV | `SHTRV-01` | thermostats (target, position, window), battery | ◐ (boost and schedule not mapped) |
| Uni | `SHUNI-1` | relays, inputs, adcs, ext_temperature/humidity | ✅ |
| EM / 3EM | `SHEM` / `SHEM-3` | emeters, relay (contactor) | ✅ |
| Gas | `SHGS-1` | gas alarm state, concentration | ◐ (valve add-on, self test, mute not mapped) |
| Sense | `SHSEN-1` | temperature, humidity, lux, battery, charger | ◐ (motion is a root bool on the Sense and is not mapped; IR blaster not mapped) |
| Air, Dimmer W1 | `SHAIR-1` *(mem)*, `SHDIMW-1` *(mem)* | not in the API docs; relays / lights would be mapped generically | ◐ (undocumented, untested) |

### Gen2 (Plus / Pro), Gen3, Gen4 (JSON-RPC)

| Device | Model id(s) | Components used | Status |
|---|---|---|---|
| Plus 1, Plus 1 Mini, Pro 1 / Pro 2 / Pro 3, 1 Gen3, 1 Mini Gen3, 1L/2L Gen3, 1 Gen4, 1 Mini Gen4, 1L/2L Gen4 | `SNSW-001X16EU`, `SNSW-001X15UL`, `SNSW-001X8EU`, `SPSW-001XE16EU`, `SPSW-201XE16EU`, `SPSW-002XE16EU`, `SPSW-202XE16EU`, `SPSW-003XE16EU`, `S3SW-001X16EU`, `S3SW-001X8EU`, `S3SW-0A1X1EUL`, `S3SW-0A2X4EUL`, `S4SW-001X16EU`, `S4SW-001X8EU`, `S4SW-0A1X1EUL`, `S4SW-0A2X4EUL` | switch, input | ✅ |
| Plus 1PM, 1PM Mini, Pro 1PM, Pro 4PM, 1PM Gen3, 1PM Mini Gen3, 1PM Gen4, 1PM Mini Gen4 | `SNSW-001P16EU`, `SNSW-001P15UL`, `SNSW-001P8EU`, `SPSW-001PE16EU`, `SPSW-201PE16EU`, `SPSW-004PE16EU`, `SPSW-104PE16EU`, `S3SW-001P16EU`, `S3SW-001P8EU`, `S4SW-001P16EU`, `S4SW-001P8EU` | switch (metering), input | ✅ |
| Plus 2PM, Pro 2PM, 2PM Gen3, 2PM Gen4 | `SNSW-002P16EU`, `SNSW-002P15UL`, `SPSW-002PE16EU`, `SPSW-202PE16EU`, `S3SW-002P16EU`, `S4SW-002P16EU` | switch profile: switch; cover profile: cover; input | ✅ (profile read during the scan; re-scan after changing it) |
| Pro Dual Cover PM, Shutter Gen3 | `SPSH-002PE16EU`, `S3SH-0A2P4EU` | cover, input | ✅ |
| Plus Plug S/IT/UK/US, Plug S/Outdoor/M Gen3, AZ Plug, Plug CN Gen3, Plug US Gen4, Power Strip 4 Gen4 | `SNPL-00112EU`, `SNPL-10112EU`, `SNPL-00110IT`, `SNPL-00112UK`, `SNPL-00116US`, `S3PL-00112EU`, `S3PL-20112EU`, `S3PL-30110EU`, `S3PL-10112EU`, `S4PL-00116US`, `S4PL-00416EU` | switch (metering); Plug US Gen4 also illuminance | ✅ (LED ring `*_ui` config not mapped) |
| Plus PM Mini, PM Mini Gen3, Plug PM Gen3 | `SNPM-001PCEU16`, `S3PM-001PCEU16`, `S3PL-30116EU` | pm1 | ✅ |
| Plus i4 / i4DC, i4 Gen3, i4 Gen4 | `SNSN-0024X`, `SNSN-0D24X`, `S3SN-0024X`, `S4SN-0A24X` | input (button events / switch state) | ✅ |
| Plus Uni | `SNSN-0043X` | switch, input (digital, count), voltmeter / temperature / humidity peripherals (id 100+) | ✅ |
| Plus H&T, H&T Gen3, AZ H&T | `SNSN-0013A`, `S3SN-0U12A`, `S3SN-1U12A` | temperature, humidity, devicepower; sleeping (outbound websocket) | ✅ |
| Plus Smoke | `SNSN-0031Z` | smoke (alarm, mute), devicepower; sleeping | ✅ |
| Flood Gen4, Flood S Gen4 | `S4SN-0071A`, `S4SN-0071Z` | flood, devicepower; sleeping | ✅ |
| Plus 0-10V Dimmer, Plus Wall Dimmer, Pro Dimmer 1PM/2PM, Pro Dimmer 0/1-10V PM, Dimmer Gen3, Dimmer 0/1-10V PM Gen3, Dimmer Gen4 (+US), Dimmer 0/1-10V PM Gen4 | `SNDM-00100WW`, `SNDM-0013US`, `SPDM-001PE01EU`, `SPDM-002PE01EU`, `SPCC-001PE10EU`, `S3DM-0A101WWL`, `S3DM-0010WW`, `S4DM-0A101WWL`, `S4DM-0A102US`, `S4DM-0010WW` | light, input | ✅ (dim up/down and calibration not mapped) |
| DALI Dimmer Gen3 | `S3DM-0A1WW` | light, input | ◐ (`dali` scan not mapped) |
| Plus RGBW PM | `SNDC-0D4P10WW` | light / rgb / rgbw profile, input | ✅ |
| Pro RGBWW PM | `SPDC-0D5PE16EU` | light, rgbcct (rgb + cct), cctx2 (cct ×2), rgbx2light profiles; input | ✅ |
| Duo Bulb Gen3, Multicolor Bulb Gen3 | `S3BL-D010009AEU`, `S3BL-C010007AEU` | cct, rgbcct | ◐ (the colour/white `mode` of the multicolour bulb is not a data point) |
| Pro 3EM, Pro 3EM-400, Pro 3EM-3CT63, 3EM-63 Gen3 | `SPEM-003CEBEU`, `SPEM-003CEBEU400`, `SPEM-003CEBEU63`, `S3EM-003CXCEU63` | triphase: em + emdata; monophase: em1 + em1data; temperature; Pro Output Add-on switch | ✅ |
| Pro EM-50, EM Gen3, EM Gen4, EM Mini Gen4 | `SPEM-002CEBEU50`, `S3EM-002CXCEU`, `S4EM-002CXCEU`, `S4EM-001PXCEU16` | em1, em1data, switch (contactor) | ✅ |
| Pro 1CB / 2CB / 3CB / 4CB | `SPCB-01VENEU` … `SPCB-04VENEU` | cb, voltmeter | ✅ |
| Presence Gen4 | `S4SN-0U61X` | presencezone, illuminance | ◐ (`presence:0` live tracking not mapped) |
| BLU Gateway (Gen2), any Gen2 Pro / Gen3 / Gen4 as BLU host | `SNGW-BT01` | bthomedevice, bthomesensor (BLU devices below) | ✅ |
| BLU Gateway Gen3 | `S3GW-1DBT001` | blutrv, bthomedevice, bthomesensor | ✅ |
| The Pill | `S3SN-0U53X` | input, voltmeter, switch, temperature, humidity (by add-on) | ◐ (serial / Modbus not mapped) |
| Wall Display (X1i, X2, X2i, XL) | `SAWD1` / `SAWD-0A1XX10EU1` *(mem)*, `SAWD-2A1XX10EU1`, `SAWD-3A1XE10EU2`, `SAWD-5A1XX10EU0`, `SAWD-6A1XX10EU0` | switch, input, temperature, humidity, illuminance, thermostat | ◐ (the Wall Display RPC API and the thermostat component are not documented; mapped from firmware observation; untested) |
| XMOD1, XT1 and XT1 products, Ogemray 25A, Yihao blinds motor | `S3XT-0S`, vendor ids | virtual number/boolean/enum/text/button; switch, input; cover | ◐ (the virtual `object` and `group` types are not mapped) |
| Camera | `S1CM-0DXW00` | camera, streamer, storage | ❌ video device; WebRTC/RTSP streaming is not a smart-home data point |
| Cury | `S4PB-00CU000002` | `cury` | ❌ undocumented component |

### BLU (Bluetooth, through a gateway)

BLU devices are battery BLE beacons with no IP address. They are supported **through a Gen2 Pro / Gen3 / Gen4 / BLU Gateway** that has them paired. Each paired device becomes a `bthomedevice` with its `bthomesensor`s. The driver does not talk Bluetooth itself.

| Device | Model id | BTHome objects → data points | Status |
|---|---|---|---|
| BLU Button1 / Button Tough 1 (ZB), Wall Switch 4 (ZB), RC Button 4 (ZB), Remote Control ZB | `SBBT-002C`, `SBBT-102C`, `SBBT-004CEU`, `SBBT-104CEU`, `SBBT-004CUS`, `SBBT-104CUS`, `SBRC-005B` | button events on `bthomedevice` (single/double/triple/long push, rotate) | ✅ |
| BLU Door/Window (ZB, Mini ZB, Outdoor ZB) | `SBDW-002C`, `SBDW-103C`, `SBDW-203C`, `SBDW-302C` | window (state), rotation, illuminance / light level | ✅ |
| BLU Motion (ZB) | `SBMO-003Z`, `SBMO-103Z` | motion (state), illuminance | ✅ |
| BLU H&T (ZB, Display ZB) | `SBHT-003C`, `SBHT-203C`, `SBHT-103C` | temperature, humidity | ✅ |
| BLU Distance | `SBDI-003E` | distance, vibration | ✅ |
| BLU TRV | `SBTR-001AEU` | through BLU Gateway Gen3 `blutrv` | ✅ |
| Ecowitt WS90 (BLU Weather Station) | `SBWS-90CM` | every BTHome object as a value | ✅ |

### Out of scope

| What | Why |
|---|---|
| **Shelly Wave / Wave Pro / Wave LR** (Z-Wave, about 60 SKUs: `QN*`, `QM*`, `QP*`, `QL*`, `QT*`, `QU*`, Qubino-branded) | Z-Wave radio devices with no IP API. Use a Z-Wave controller and the Automatica Z-Wave driver. |
| Zigbee mode of Gen4 and BLU "ZB" devices | Needs a Zigbee coordinator. In WiFi mode the same Gen4 devices are covered here. |
| Matter | Gen3/Gen4 expose Matter in addition to RPC. The driver uses RPC. |
| Shelly LAN Switch | Unmanaged Ethernet switch, no API. |
| LoRa add-ons (SheLr) | The radio is not a data point. SheLr dynamic components are not mapped. |
| Direct Bluetooth (BLE scanning by the server) | BLU devices are reached through a Shelly gateway instead. |
| Cloud API | The driver is local only. |

## Setup

### Driver node properties

| Property | Meaning |
|---|---|
| Scan addresses | Extra IPs (`host` or `host:port`, separated by comma, `;` or space) that the scan reads. Use it when mDNS does not cross VLANs or Docker networks. |
| Default username / password | Used by the scan and by learning for devices with login enabled. Gen2+ always uses the user `admin`. The password is stored as a secret on every device it is used for. |
| Learn devices | Learn mode: battery devices that wake up are reported. |
| CoIoT | Receive Gen1 CoIoT pushes on UDP 5683 (default on). |
| Outbound websocket port | `0` = off. Set a port and configure every **battery Gen2+ device** with outbound websocket `ws://<automatica-host>:<port>/` (web UI → Settings → Outbound websocket, or `Ws.SetConfig {"config":{"enable":true,"server":"ws://…"}}`). The device connects when it wakes and pushes `NotifyFullStatus`. Unknown devices are learned this way. |

### Device properties

| Property | Meaning |
|---|---|
| Id | The MAC. Gen2+ device ids like `shellyplus1pm-a8032ab12345` are accepted too. |
| IP | Empty means mDNS by MAC. Battery devices take the address they report from. |
| Password | Gen2+ digest password, or Gen1 basic-auth password (Gen1 also has Username). |
| Polling interval | Gen1 5 s, Gen2+ 60 s (the websocket delivers changes right away). |
| Sleeping | Battery device, set by the scan. Sleeping devices are never polled; a failed start is not an error. |

### Docker / network notes

- **CoIoT** is multicast `224.0.1.187:5683`. Inside Docker it needs host networking (`network_mode: host`). Otherwise, set each Gen1 device's CoIoT peer to `<automatica-host>:5683` (unicast).
- **Outbound websocket**: publish the configured port (`-p <port>:<port>`).
- **mDNS** does not cross a bridge network. Use host networking, or the *Scan addresses* property.

## Legacy templates (driver 0.2)

The six model templates of driver 0.2 still work for existing configurations: Shelly 1, Shelly 1PM, Shelly 2.5, Plus 1PM, Plus 2PM and Plus Plug S. They keep their code path ([`ShellyDriverDevice`](Automatica.Driver.ShellyFactory/ShellyDriverDevice.cs)) and their template ids. New devices are created through the scan with the generic templates above. To move an old device to the generic templates, delete it and scan again.

## Tests

`tests/Automatica.Driver.Shelly.Unit.Tests` covers the driver with fake devices. No test needs real hardware or touches the LAN.

- **Fake device:** [`FakeShellyDevice`](tests/Automatica.Driver.Shelly.Unit.Tests/Fakes/FakeShellyDevice.cs) runs on a Kestrel server on 127.0.0.1. It replays the fixtures in `Fixtures/*.json`: one representative device per component type and generation, 30 devices in all, Gen1 to Gen4.
  - Gen1: `/shelly`, `/status`, `/settings`, `/cit/d`, the control URLs, and basic auth.
  - Gen2+: `/rpc` with SHA-256 digest checked as the device does it, plus the websocket.
- **Node builder:** for every fixture, the exact components and data points.
- **RPC client:** digest retry and nonce reuse, wrong or missing password, JSON-RPC errors, timeouts, the websocket hello and its 401 auth resend, notifications.
- **Outbound websocket server**, and **CoIoT**: packet parsing, `/cit/d`, the UDP listener.
- **Running devices in the real driver:**
  - Status into data points; `NotifyStatus` deltas; button events.
  - Writes: Switch, Cover, RGBW channel, virtual components, CB, BLU TRV, Gen1 relay/colour/TRV.
  - Digest and basic auth; a wrong device id.
  - A sleeping Gen2+ device over the outbound websocket; learning an unknown one in learn mode.
  - Gen1 CoIoT without a poll; a sleeping Gen1 H&T over CoIoT, then reading `/status` while it is awake.
- **Scan:** of IP lists, skipping configured devices, the default password.
- **Catalog:** unique and stable template ids and keys; every text translated in en and de.

```bash
dotnet test tests/Automatica.Driver.Shelly.Unit.Tests -p:BaseOutputPath=bin/
```

(`-p:BaseOutputPath=bin/` keeps the factory build out of the main checkout's `Drivers/Shelly` folder that the csproj points at.)

### Not verified on real hardware

- **Gen2+ coverage:** all of it, including the payload shapes recorded from the API docs, is verified only against fixtures.
- **Undocumented or memory-based payloads:**
  - Wall Display: the `thermostat` and the `SAWD-0A1XX10EU1` model id.
  - Gen1 type ids marked *(mem)*.
  - mDNS TXT `gen=4`.
- **Gen1 CoIoT:**
  - Option parsing follows the Gen1 doc (options 3332, 3412, 3420; payload `G`).
  - Real multicast reception through Docker is untested.
- **Outbound websocket:** the handshake was tested with .NET's `ClientWebSocket`, not with Shelly firmware.
- **Digest auth:** the nonce expiry (`stale=true`) and the HTTP 429 behaviour of firmware ≥ 2.0 are untested.

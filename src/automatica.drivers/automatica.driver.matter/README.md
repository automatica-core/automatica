# Automatica Matter Bridge

Publishes Automatica.Core controls as [Matter](https://csa-iot.org/all-solutions/matter/) devices, so Apple Home,
Google Home, Amazon Alexa, Home Assistant and other Matter controllers can switch, dim and move them.

The Matter stack is written in plain C# (`P3.Driver.Matter`, no native SDK). It covers:

- the transport: UDP with MRP retransmissions and acks;
- PASE (SPAKE2+) commissioning and CASE operational sessions;
- the interaction model: read, subscribe, write, invoke and timed requests, with chunking and ACLs;
- device attestation, multiple fabrics, and the enhanced commissioning window;
- mDNS advertising.

## Supported controls

| Automatica control | Matter device type |
|---|---|
| `IDimmer` | Dimmable Light (On/Off + Level Control) |
| `ISwitch` | On/Off Light |
| `IBlind` | Window Covering (lift, percentage) |

Changes made in Automatica are reported to the controllers through their subscriptions.

## Manual devices

You can also add devices as child nodes of the bridge, the way you add HomeKit accessories. Each device's data
points are nodes that you link in logic or to other drivers (KNX, Modbus, ...):

- Values *written* to a node are shown in the controllers.
- Values a controller sets are *read* from the node.

Optional data points can be added to a device later.

| Device | Data points (optional in *italics*) |
|---|---|
| On/Off light, outlet | on-off |
| Dimmable light, dimmable outlet | on-off, level (%) |
| Tunable white light | on-off, level, color-temperature (K) |
| Color light | on-off, level, color (`#RRGGBB`), *color-temperature* |
| Blind / shutter | position (%, 0 = open), *open*, *close*, *stop* |
| Contact sensor, water leak detector, rain sensor | contact / leak / rain, *battery* |
| Occupancy sensor | occupied, *battery* |
| Light, temperature, humidity, pressure, flow sensor | illuminance (lx) / temperature (°C) / humidity (%) / pressure (hPa) / flow (m³/h), *battery* |
| Air quality sensor | air-quality (0–6), *co2*, *pm25*, *pm10*, *tvoc*, *temperature*, *humidity*, *battery* |
| Smoke / CO alarm | smoke, *co*, *battery* |
| Thermostat (heating), thermostat (heating & cooling) | local-temperature, heating-setpoint, (cooling-setpoint), *system-mode*, *heating-active*, *cooling-active* |
| Fan | speed (%) |
| Door lock | locked |
| Water valve | open |

Not bridged yet: generic switches/buttons (they need Matter events), energy metering, robot vacuums, and other
appliance device types.

## Setup

1. Add the **Matter Bridge** node and select the controls to publish under *Controls*.
2. Start the driver. The pairing code and QR payload appear as values under the bridge node and in the log.
3. In your Matter app, choose *Add device*, then *More options*, and enter the code or scan a QR code generated
   from the `MT:` payload.
4. To pair another ecosystem later, run the **Open commissioning window** action (15 minutes).
   **Reset pairings** removes every fabric.

### Network

- The bridge listens on UDP **5540** (configurable). Controllers find it via mDNS (`_matter._tcp`, `_matterc._udp`).
- In Docker, mDNS needs host networking (`network_mode: host`) or an mDNS reflector. IPv6 must be enabled on the
  host, because most controllers talk to Matter devices over IPv6 link-local addresses.

## Attestation and certification

The bridge identifies itself with the **CSA test attestation PKI** (VID `0xFFF1`, PID `0x8000`). Those test
certificates come from the Matter SDK under Apache-2.0 (`P3.Driver.Matter/Resources/TestCerts`).
Controllers accept them for development devices, but show an "uncertified device" warning during pairing.
Shipping without that warning needs a CSA membership, a vendor id and certification. You can then pass your own
DAC, PAI and Certification Declaration through `MatterNodeOptions.Attestation`.

## Projects

| Project | Purpose |
|---|---|
| `P3.Driver.Matter` | Matter protocol library (bridge API: `MatterBridge`, `BridgedOnOffDevice`, `BridgedDimmableLight`, `BridgedWindowCovering`) |
| `P3.Driver.Matter.Driver` | Automatica driver plugin |
| `P3.Driver.Matter.Console` | Stand-alone demo bridge: `dotnet run -- [statePath] [port]`; set `MATTER_TRACE=1` for protocol traces |
| `P3.Driver.Matter.Tests` | Unit tests plus an in-process commissioner that commissions the bridge over UDP and drives it via CASE |

### Interop test with matter.js

The demo console runs a bridge that any controller can pair with. For a scripted run, use
[matter.js](https://github.com/project-chip/matter.js) as the controller:

```bash
cd tools
npm install @matter/main@0.18.0
node commission.mjs <pairing-code-from-the-bridge-log>
```

[`tools/commission.mjs`](tools/commission.mjs) creates a `ServerNode` with `ControllerBehavior` and calls `controller.peers.commission({ pairingCode })`.
It dumps the endpoint tree, then toggles "Demo Light", dims "Demo Dimmer" and moves "Demo Blind" (the console demo devices).

## License

GPL-3.0 (see `LICENSE`). The bundled CSA test certificates are Apache-2.0.

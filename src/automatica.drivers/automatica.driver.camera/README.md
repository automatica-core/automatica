# Camera

IP cameras (RTSP, optionally found and configured via ONVIF) as live views and snapshots in the visualization, plus
their motion / person events as data points.

```
Camera                      go2rtc URL / user / password, polling interval, "Find cameras" (ONVIF discovery)
└── Camera (e.g. "Garden")  host, ONVIF port, user, password, use ONVIF, main / sub stream URL, snapshot URL, events
    ├── Online              bool, the RTSP (or HTTP) port answers
    ├── Motion              bool, ONVIF motion events
    └── Person              bool, ONVIF person / human events (cameras with object detection)
```

## How it works

The browser never talks to the camera or to go2rtc:

```
camera ──RTSP──► go2rtc (127.0.0.1:1984) ──WebSocket (MSE, fMP4)──► Automatica /webapi/camera/{id}/ws ──► visu tile
```

- [go2rtc](https://github.com/AlexxIT/go2rtc) pulls the RTSP streams and repackages H.264/H.265 for Media Source
  Extensions without transcoding. The server starts it itself (`Go2RtcProcessHost`, bound to 127.0.0.1:1984 only)
  when the binary is installed: the Docker images ship it in `/usr/local/bin/go2rtc`. Elsewhere put `go2rtc` next to the
  server, in the PATH, or set `server:go2rtc_path`. An own go2rtc (e.g. a container) is used with `server:go2rtc_url`
  (+ `server:go2rtc_username` / `server:go2rtc_password`) in the server configuration and the same URL in the driver.
- The driver registers the streams at runtime under the camera node id: `{id}` (main), `{id}_sub`, `{id}_snap`.
  The camera passwords stay on the server (stored encrypted, sent only to go2rtc on localhost).
- `/webapi/camera/{id}` relays go2rtc with the normal login and the user group of the camera node, so live views also
  work over Remote Connect. The WebSocket takes the token as `access_token` query parameter (browsers cannot set headers
  on WebSockets) and ends when the token expires; the tile reconnects with a fresh one.

## Setting up a camera

1. Add the **Camera** driver. Leave the go2rtc URL at `http://127.0.0.1:1984` unless you run go2rtc yourself.
2. Click **Find cameras**: lists the ONVIF cameras answering a WS-Discovery probe on the local network. Or add a camera
   by hand with its host.
3. Enter user and password of the camera. With **use ONVIF** on, the driver reads the stream URLs from the camera
   (main = the profile with the most pixels, sub = the one with the fewest) and the snapshot URL. Fill in **main stream**
   / **sub stream** / **snapshot URL** to override them, or for cameras without ONVIF
   (e.g. `rtsp://192.168.1.20:554/Streaming/Channels/101`). Credentials in the URL win over the user / password fields.
4. Add a **Camera** tile to a visu page and pick the camera (or one of its data points) as node.

The tile plays the sub stream while it is on screen and the page is visible; the fullscreen view plays the main
stream. Snapshot mode refreshes a JPEG every few seconds instead (less load for many cameras).

## Requirements and caveats

- Docker: go2rtc reaches the cameras from inside the container; with a bridged network that works for routed traffic,
  but **Find cameras** (multicast) needs `network_mode: host`.
- ONVIF port: often 80, some cameras use 8000, 8080 or 2020 (Tapo). Events need the camera clock roughly right or the
  WS-Security digest is rejected; the driver corrects the offset with GetSystemDateAndTime.
- Browsers play H.264 everywhere, H.265 only in Safari / recent Chrome with hardware support. Set the camera to H.264
  if the tile stays black. iOS needs 17.1+ (ManagedMediaSource).
- Snapshots without a snapshot URL are frames of the stream, which go2rtc can only make with ffmpeg installed next to it
  (not in the images): configure the snapshot URL of the camera or use the live mode.
- Two-way audio, PTZ and recording are not supported. go2rtc's own RTSP / WebRTC / RTMP listeners are switched off.

## Not verified yet

Tested against fakes only (driver: fake HTTP / SOAP; server: fake go2rtc). Not yet run against a real go2rtc and a
real camera: the empty `listen` keys switching the go2rtc listeners off, stream registration surviving go2rtc
restarts (the driver re-registers missing streams every poll), and the ONVIF event topics of specific vendors.

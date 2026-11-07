# Network presence

Detects who is at home from the phones that are connected to the local network.

```
Network presence            networks, polling interval, away timeout
├── Anyone home             bool
├── People home             int
└── Person (e.g. "Anna")    "Find phones" lists the devices on the network that nobody has yet
    ├── Home                bool, true while one of the phones is connected
    ├── Last seen           datetime
    ├── Phones connected    int
    └── Phone               MAC address
        ├── Connected       bool
        ├── IP address      string
        └── Last seen       datetime
```

## How it works

Every polling interval the driver sends one UDP datagram (discard port 9) and one ICMP echo to the last known
address of every tracked phone. Sending anything makes the kernel resolve the address with ARP, and phones in standby
still answer ARP even when they ignore pings. Then it reads the neighbor table (`/proc/net/arp` on Linux, `arp -a`
elsewhere). A phone is seen when its MAC is in the table and resolved.
A full sweep of every address in the networks runs every few minutes (and while a phone has never been seen), so
phones that got a new DHCP address are found again.

A phone counts as away only after it was not seen for the away timeout (default 10 minutes). Phones in standby drop
off the Wi-Fi for minutes, so short timeouts cause false "away" events.

## Assigning phones to users

1. Add a **Person** below the driver and name it after the user.
2. Click **Find phones** on the person. The list shows the devices currently on the network (host name and address)
   that no person has yet.
3. Add the phone: it is assigned to that person. A person can have several phones; one connected phone is enough.

## Requirements and caveats

- The server has to be in the same layer-2 network as the phones (Docker: `network_mode: host`). The ARP table of a
  bridged container only contains the bridge.
- Private Wi-Fi addresses: iOS and Android use a random MAC per network. It stays stable for the home network, unless
  "rotating" private addresses (iOS 18 "Rotating", Android "per connection") are on. Switch that off for the home Wi-Fi.
- Networks bigger than /22 are not swept host by host; only the /24 around the network address is. Configure the
  right /24 explicitly in that case.

# eBUS driver

Reads heating devices on the eBUS through a network adapter in **raw mode** (the adapter forwards every bus byte 1:1 over TCP, e.g. ser2net or esp-link; the ebusd "enhanced" protocol is not supported).

The driver is **read only**:
- it listens to the telegrams of the other devices (broadcasts, answers to the controllers);
- it polls registers with read requests as its own master (default address 0xFF, the lowest priority). It waits for an idle bus, checks the echo of its address (arbitration) and retries when another master wins.

Nothing is written to the devices. Wolf register writes (50 23) go to the device EEPROM and are deliberately not supported.

## Devices

### Wolf BWL-1 (air/water heat pump, slave address 0x08)

| Node | Source | Scale |
|---|---|---|
| Flow temperature | register 13 | 1/100 °C |
| Return temperature | register 22 | 1/100 °C |
| Flow setpoint | register 2 | 1/10 °C |
| Hot water temperature | register 14 | 1/10 °C |
| Hot water setpoint | register 19 | 1/10 °C |
| Outside temperature | register 12 | 1/10 °C |
| Flow temperature circuit 2 | register 15 | 1/10 °C, probably the mixed circuit, not confirmed |
| Compressor hours | register 554 | h |
| Compressor starts | registers 550 (low) and 551 (high word) | |
| Heat pump running, circulation pump | broadcast 05 03 of the heat generator (address 0x03) | |
| Fault code | broadcast FE 01 of the heat generator ("E000" = no fault) | |

"Wolf register" nodes read any other register (register number, scale, signed).

Wolf read request: `ZZ 50 22 03 <CRC8 poly 0x5C over the register LE16> <register LE16>`, answer `<value LE16>`, `0x8000` = not available.
The registers of newer Wolf heat pumps (12391 operating mode, 12392 compressor status, 12399/12400 energy, 13053 JAZ) are not available on the BWL-1.

## Test tool

`Tests/P3.Driver.EBus.ConsoleApp`: `dotnet run -- <host> <port> [seconds to listen] [slave hex] [register...]` prints the telegrams on the bus and reads the given registers.

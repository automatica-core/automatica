# Spot price

Day-ahead electricity prices from **aWATTar Austria / Germany** (no key) or the **ENTSO-E transparency platform**
(security token, every bidding zone), with digital outputs for the cheapest (or most expensive) hours.

```
Spot price               provider, ENTSO-E token + bidding zone, surcharge, VAT, refresh interval, API URL
├── Price now [EUR/kWh]             market price + surcharge, plus VAT
├── Market price now [EUR/kWh]      net day-ahead price
├── Price next hour [EUR/kWh]       average of the next full hour (also with 15-minute prices)
├── Lowest / highest / average price today [EUR/kWh]
├── Prices known until              end of the last known slot (local time)
├── Connected                       the last fetch worked
└── Cheapest hours (any number)     hours, period, contiguous, mode → true while inside the window
```

`Price now` is in EUR/kWh, so it can be linked straight to the import price of the energy monitor.

Price = round((market [EUR/MWh] / 1000 + surcharge) × (1 + VAT / 100), 5).

## Fetching

* The driver fetches local midnight today → local midnight + 2 days (DST-safe), every *refresh interval*
  (15–1440 min, default 60).
* If the known prices reach less than 12 hours ahead (tomorrow is published around 13:00–14:00) or the last fetch
  failed, it asks every 15 minutes instead. With the default that stays well below aWATTar's fair use of
  100 requests a day.
* On an error *Connected* goes false and the known prices are kept, so the outputs keep working until the data runs out.
* aWATTar: `GET /v1/marketdata?start=<ms>&end=<ms>`, EUR/MWh. The slot length is taken from the data (60 minutes today,
  15 minutes works too).
* ENTSO-E: `GET /api?securityToken=…&documentType=A44&contract_MarketAgreement.type=A01&in_Domain=<EIC>&out_Domain=<EIC>&periodStart=yyyyMMddHHmm&periodEnd=…` (UTC).
  Curve type A03 (repeated prices left out) is filled in; where a 15-minute and a 60-minute series overlap the finer one
  wins. "No matching data found" counts as no prices yet. The token is never written to the log or to error messages.
  Get a token by asking transparency@entsoe.eu for API access and creating it in your account.

| Zone | EIC |
|---|---|
| Austria | `10YAT-APG------L` |
| Germany / Luxembourg | `10Y1001A1001A82H` |

## Cheapest-hours windows

Add any number of *Cheapest hours* nodes below the driver:

| Property | Default | |
|---|---|---|
| Hours | 3 | 0.25–48, quarter hours allowed |
| Period [h] | 24 | 1–48, at least *Hours* |
| Contiguous | on | on: one block in a row (dishwasher, washing machine); off: the cheapest single slots (boiler, heat pump) |
| Mode | Cheapest | or *Most expensive*, e.g. to block loads at peak prices |

A period starts when the first prices are there and lasts *Period* hours; the next one starts when it ends. Within a
period the window is planned from the known prices that end inside the period. New prices (tomorrow's) can move a
window that has not started yet; a started window is kept. So the output is on for exactly *Hours* per period. If
fewer prices than *Hours* are known, the output stays off and the window is planned as soon as they arrive.

## Not verified

* No live call to aWATTar or ENTSO-E from the driver: the tests use a fake server with the documented formats.
* The configuration UI (property editor, enum lists) was not opened in Automatica.

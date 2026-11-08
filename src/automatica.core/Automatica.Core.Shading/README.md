# Automatica.Core.Shading

Automatic shading of roller blinds ("Rollo") and venetian blinds ("Jalousie", "Raffstore"). It runs on the core
only. Every minute, and right away when wind, rain, the outside temperature or a door contact changes, the engine reads
the sun position, the brightness of each facade and the weather. Then each configured blind decides what to do.

## Configuration

Three JSON documents, stored in `ShadingSettings` (one row), `ShadingFacades` and `ShadingBlinds`
(see `Definition/ShadingDefinitions.cs`). The API is `webapi/shading` (admins only).

- **Settings**: where the sun position, brightness, wind, rain and outside temperature come from; the brightness
  thresholds; the wind limit; the frost limit; the manual override mode; and the move throttling.
- **Facade**: name and azimuth (0 north, 90 east, 180 south, 270 west; the UI offers the 16 compass points), plus
  an optional brightness signal of that side of the building. A facade can also set its own thresholds, the
  acceptance angle (how far left or right of the facade direction the sun may be, default 80°), the horizon limit
  (`minElevation`, for trees or buildings), the overhang limit (`maxElevation`, for a balcony), and "only shade
  when warmer than X °C". `inclination` is the tilt of the glass from horizontal: 90° (default) is a wall, smaller
  values are roof windows (0° = flat skylight); a blind can override it for a single roof window.
- **Blind**: a shutter logic instance (the control id), its facade or its own azimuth, the type (roller or
  venetian), the shade position, and whether to restore the previous position afterwards. For venetian blinds also
  the slat geometry and how the angle maps to percent. Optional: wind protection, rain protection, a door contact
  and the travel time.

### Mixing data sources

Every input is a `Signal`, a list of sources tried in order. The first source with a fresh value wins:

```json
{ "sources": [
    { "type": "datapoint", "node": "<lux east of the weather station>", "factor": 1000, "staleAfterMinutes": 10 },
    { "type": "calculated", "cloudCover": { "sources": [ { "type": "datapoint", "node": "<OpenWeatherMap clouds>" } ] } }
] }
```

- `datapoint`: any node instance or logic output. The value is converted as `value * factor + offset`. A value
  older than `staleAfterMinutes` is skipped.
- `irradiance` (brightness only): global horizontal irradiance in W/m² (pyranometer, weather station). It is split
  into direct and diffuse light (Erbs, relative to the clear sky), then projected on the facade.
- `pv` (brightness only): the power of a PV system. With `peakPower`, the panel `azimuth`/`tilt` and the
  `performanceRatio`, the power gives the irradiance on the panels, which is turned back into sky irradiance and
  projected on the facade. When the sun is too low or too far from the panels (cos < 0.2), the source has no value
  and the next one is tried.
- `calculated`: the sun position (astronomical, from the Latitude/Longitude settings), or the brightness for the
  facade direction from a clear sky model (Meinel), reduced by the optional cloud cover (Kasten-Czeplak). Wind, rain
  and temperature cannot be calculated.

All brightness sources end in the same facade illuminance: direct · cos(incidence) + half the diffuse sky + half the
ground reflection (albedo 0.2), at 105 lm/W. For a tilted plane (roof window, inclination t) the incidence uses
`cos i = sin h·cos t + cos h·sin t·cos(Δaz)`, the sky term is diffuse·(1 + cos t)/2 and the ground term
albedo·global·(1 − cos t)/2; at 90° this is exactly the wall formula.
- If a facade's brightness signal has no value, the global brightness signal is used. A calculated source in the
  global signal still estimates the brightness for that facade's direction.

## Decision per blind

Highest priority first:

1. **Off**: switched off globally, for the facade or for the blind.
2. **Frost**: the outside temperature is below `frostBelow`, so nothing moves.
3. **Wind / rain**: the blind retracts to 0 %. It retracts again if someone lowers it during the alarm. The alarm
   ends `windReleaseMinutes` after the wind drops below the limit. Venetian blinds have wind protection by default,
   roller blinds don't.
4. **Paused**: by an automation (`shading` action), the switch on the visualization tile or
   `POST blinds/{id}/automatic`. The pause is saved (`ShadingBlinds.IsPaused`) and survives a restart.
5. **Manual**: the blind was moved by hand. A change in position, slats or "starts moving" that the automatic did
   not cause; changes during `travelSeconds` after an automatic move and in the first 2 minutes after start are
   ignored. The override ends depending on `manualOverride`:
   - `untilChange` (default): when the shading demand changes (the sun comes or goes)
   - `duration`: after `manualOverrideMinutes`
   - `untilEndOfDay`: at midnight
6. **Door open**: the blind is never lowered while its door contact reports open.
7. **Shading**: there is demand when the sun is on the window (geometry), the facade is sunny (brightness with
   hysteresis: on at `onLux` for `onDelayMinutes`, off below `offLux` for `offDelayMinutes`) and the temperature
   condition holds. The blind moves to `shadePosition`. Venetian slats follow the sun: they get the smallest angle
   that still blocks direct light, `asin(spacing·cos γ / width) − γ` with the profile angle γ, plus `extraAngle`.
   On a roof window (inclination below 90°) the sun is on the window when the incidence angle is at most the
   acceptance angle, so a high sun from behind the facade direction counts too; venetian slats there keep their
   `fixedAngle`, the profile-angle tracking only applies to walls.
   Slats move at most every `minMoveIntervalMinutes`, and only by at least `minSlatChange`. A blind that is
   already closed further than the shade position is not touched.
8. **Winter**: in winter, a blind with `winterGain` stays open on sun, so the sun warms the room, unless its room
   temperature is above `maxRoomTemperature`. Winter is set in the settings: `off`, `temperature` (outside
   temperature below `below`, 1 °C hysteresis) or `months` (`fromMonth` to `toMonth`, may wrap the year).
9. **Idle**: when the sun goes, a blind the automatic lowered goes back to its previous position (or to
   `openPosition`). If someone moved it in between, it stays where it is.

Positions are percent, with 0 open and 100 closed, for the blind and for the slats (KNX DPT 5.001). The shutter
logic has an optional slat input (feedback) and output (`ISlatBlind`).

## User interface and API

- **Admin page** "Beschattung" (`/admin/shading`): overview with a compass (sun, sun path of today, facades lit by the
  sun), a time slider for a preview of today (`GET preview?at=`, clear sky, no weather alarms), the conditions and
  every blind's state; tabs for facades, blinds and settings. Admins only, like `webapi/shading`.
- **Visualization**: the shutter tile gets a sun switch when the blind is set up for shading. Lit = the automatic is
  in charge, dashed = paused by hand (a tap resumes), dim = switched off. API `webapi/shading-visu`: reading for
  every signed-in user, switching for the roles visu and administrator.
- **MCP**: `get_shading_status`, `preview_shading`, `set_shading_automatic`, `resume_shading` (admin or API key;
  the switching tools need write access).

## Automations

- Action `shading`: pause (`enable: false`) or resume the automatic for the listed blinds and/or all blinds in an
  area or category.
- Condition `shading`: a blind is (or is not) being shaded right now. It is re-checked every minute.

Both are in the automation editor. The validator warns (`shadingNotConfigured`) when a blind is not set up for
shading.

## Tests

`Tests/Automatica.Core.Tests/Shading` runs on a fake time provider in Vienna, on midsummer.

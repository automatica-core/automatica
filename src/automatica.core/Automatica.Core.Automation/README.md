# Automations

"If this then that" rules for automatica.core: **triggers** start a run, **conditions** are checked at that moment, **actions** run one after the other. Admins edit them in the web UI (Automations page). They run on the core only; satellites only deliver values.

The rule is stored as one JSON document (`Automation.Definition`, model in `Definition/AutomationDefinition.cs`, serializer `Definition/AutomationJson.cs`). The objects it uses are indexed in `AutomationReference`, the latest 50 finished runs per rule are kept in `AutomationRun`.

## Definition (schema 1)

All property names are camelCase, enums are camelCase strings, every block has a `type`.

```json
{
  "schema": 1,
  "triggers": [
    { "type": "datapoint", "id": "motion", "node": "8f1c…", "change": "to", "value": true, "forSeconds": 0 }
  ],
  "conditions": { "type": "group", "operator": "all", "conditions": [
    { "type": "datapoint", "node": "3b07…", "operator": "lt", "value": 40 },
    { "type": "sun", "after": "sunset", "before": "sunrise" }
  ]},
  "actions": [
    { "type": "control", "control": "c41e…", "command": "on" },
    { "type": "waitUntil", "condition": { "type": "datapoint", "node": "8f1c…", "operator": "eq", "value": false },
      "forSeconds": 180, "timeoutSeconds": 1800, "continueOnTimeout": true },
    { "type": "control", "control": "c41e…", "command": "off" }
  ]
}
```

| Block | type | Properties |
|---|---|---|
| Trigger | `datapoint` | `node`, `change` (`any`/`to`/`above`/`below`), `value`, `from`, `forSeconds` |
| Trigger | `time` | `at` ("HH:mm[:ss]" local), `days` (`["monday",…]`, empty = every day) |
| Trigger | `sun` | `event` (`sunrise`/`sunset`/`dawn`/`dusk`), `offsetMinutes`, `days` |
| Trigger | `conditions` | `conditions` (group) – fires when the group changes from not met to met |
| Trigger | `control` | `control`, `to` (`any`/`on`/`off`) |
| Trigger | `startup` | – |
| Condition | `group` | `operator` (`all`/`any`/`not`), `conditions` |
| Condition | `datapoint` | `node`, `operator` (`eq`/`ne`/`lt`/`le`/`gt`/`ge`/`between`), `value`, `value2` |
| Condition | `time` | `after`, `before` ("HH:mm", wraps midnight), `days` |
| Condition | `sun` | `after`, `afterOffsetMinutes`, `before`, `beforeOffsetMinutes` |
| Condition | `control` | `control`, `on` |
| Condition | `trigger` | `triggerId` |
| Action | `setValue` | `node`, `mode` (`value`/`toggle`/`increment`/`copy`), `value`, `step`, `source` |
| Action | `control` | `control`, `command` (`on`/`off`/`toggle`/`dim`/`up`/`down`/`stop`/`position`), `value` (percent) |
| Action | `groupControl` | `area`, `includeSubAreas`, `category`, `command`, `value` |
| Action | `delay` | `seconds`, `randomToSeconds` |
| Action | `waitUntil` | `condition`, `forSeconds`, `timeoutSeconds` (0 = never), `continueOnTimeout` |
| Action | `notify` | `subject`, `body`, `severity` (`info`/`warning`/`error`), `email` (list) – placeholders below |
| Action | `mail` | `to` (list of addresses), `subject`, `body` – sends through the SMTP server of the settings, otherwise through the cloud |
| Action | `if` | `condition`, `then`, `else` |
| Action | `runAutomation` | `automation`, `skipConditions` |
| Action | `enableAutomation` | `automation`, `enable` |
| Action | `stop` | `reason` |

Every action also has `enabled` (default true) and `continueOnError` (default false).

Placeholders in `notify` and `mail` texts: `{automation}`, `{trigger}`, `{trigger.value}`, `{value:<node id>}`, `{date}`, `{time}`, `{datetime}` (local time of the core, formatted for its culture).

### Released for the visualization

`"userAccess": true` on the root releases the rule for the visualization. Every block can carry `"userEditable": true`; both are left out of the JSON when false. In a released rule users with the visu role (and admins) switch the rule on or off and change only these fields of the marked blocks (`Definition/AutomationUserAccess.cs`, the web UI mirrors it in `USER_EDITABLE_FIELDS`):

| Block | Fields |
|---|---|
| Trigger `time` | `at`, `days` |
| Trigger `sun` | `event`, `offsetMinutes`, `days` |
| Trigger `datapoint` | `value`, `forSeconds` |
| Condition `time` | `after`, `before`, `days` |
| Condition `sun` | `after`, `afterOffsetMinutes`, `before`, `beforeOffsetMinutes` |
| Condition `datapoint` | `value`, `value2` |
| Action `setValue` | `value`, `step` |
| Action `control` | `value` |
| Action `delay` | `seconds`, `randomToSeconds` |
| Action `waitUntil` | `forSeconds`, `timeoutSeconds` |
| Action `mail` | `to` |

Other types cannot be released. Nodes, controls, operators, commands, the marks and the structure stay with the admins.

## Paths

Run steps and validation problems point into the definition with a path:
`triggers/0`, `conditions/1` (index in the root group), `conditions/1/0` (nested group), `actions/2`, `actions/2/then/0`, `actions/3/condition`, `triggers/0/conditions/1`. An empty path means the whole rule.

## Engine rules

- Datapoint triggers fire on the edge: the previous value did not match, the new one does. An unknown previous value never fires. Only values read from the devices count (`DispatchValueSource.Read`).
- `forSeconds` starts a timer when the value matches, leaving the state cancels it.
- Conditions use the current values of the dispatcher at the moment of the trigger.
- Modes (`Automation.Mode`): `single` ignores triggers while running, `restart` stops the active run, `queued` runs them one after the other, `parallel` runs them side by side; `maxRuns` limits queued/parallel.
- Values written by a rule do not trigger the same rule again (5 s echo window). More than 30 triggers within a minute switch the rule off and create a notification. `runAutomation` chains stop after 5 levels.
- Missed time triggers (server down) are not caught up.
- Sun times use the settings `Latitude`/`Longitude`.

## REST API (`webapi/automations`, admin only)

Enum values in the DTOs are camelCase strings. Dates are UTC ISO strings.

| Method | Route | Body | Returns |
|---|---|---|---|
| GET | `` | | `AutomationDto[]` |
| GET | `{id}` | | `AutomationDto` |
| POST | `` | `AutomationSaveDto` | `AutomationDto` |
| PUT | `{id}` | `AutomationSaveDto` | `AutomationDto` |
| DELETE | `{id}` | | |
| POST | `{id}/enabled` | `{ "enabled": bool }` | `AutomationDto` |
| POST | `{id}/run?skipConditions=false` | | `{ "runId": guid }` |
| POST | `{id}/runs/{runId}/stop` | | |
| GET | `{id}/runs` | | `AutomationRunDto[]` newest first, active runs included |
| POST | `validate` | `{ "definition": {…} }` | `ValidationProblem[]` |
| POST | `test` | `{ "definition": {…}, "triggerId": "…" }` | `TestResult` |
| GET | `where-used/{objectId}` | | `AutomationDto[]` |

Saving an enabled rule with problems of severity `error` returns 400 with the `ValidationProblem[]`. A disabled rule can be saved with errors (draft).

```ts
interface AutomationDto {
  objId: string; name: string; description: string; isEnabled: boolean;
  mode: "single" | "restart" | "queued" | "parallel"; maxRuns: number;
  this2AreaInstance: string | null; this2CategoryInstance: string | null;
  definition: AutomationDefinition;
  createdAt: string; modifiedAt: string;
  lastRunAt: string | null; lastRunState: RunState | null; lastError: string | null;
  activeRuns: number;
  problems: ValidationProblem[];
}
type AutomationSaveDto = Pick<AutomationDto, "name" | "description" | "isEnabled" | "mode" | "maxRuns" | "this2AreaInstance" | "this2CategoryInstance" | "definition">;
type RunState = "running" | "waiting" | "done" | "conditionsNotMet" | "failed" | "stopped";
interface ValidationProblem { path: string; code: string; severity: "error" | "warning"; detail?: string; }
interface AutomationRunDto {
  objId: string; startedAt: string; finishedAt: string | null; state: RunState;
  triggerId: string | null; error: string | null; steps: RunStep[];
}
interface RunStep {
  at: string;                       // UTC
  kind: "trigger" | "condition" | "action" | "info" | "error";
  path: string;                     // into the definition, see Paths
  passed?: boolean;                 // conditions
  value?: any;                      // actual value (trigger value, compared value, written value)
  previous?: any;                   // trigger: previous value
  durationMs?: number;              // actions
  state?: "running" | "waiting" | "done" | "failed" | "skipped" | "timeout";
  message?: string;                 // English detail, e.g. an error
}
interface TestResult { passed: boolean; steps: RunStep[]; }
```

Validation codes: `noTriggers`, `noActions` (warnings), `nodeNotFound`, `nodeNotReadable`, `nodeNotWritable`, `controlNotFound`, `controlNotSupported`, `invalidTime`, `valueMissing`, `automationNotFound`, `areaNotFound`, `categoryNotFound`, `groupTargetMissing`, `recipientMissing`, `invalidMailAddress` (detail = the address), `negativeDuration`, `locationMissing` (warning), `selfReference` (warning).

## REST API for the visualization (`webapi/automations-visu`)

Every signed-in user may read, the visu and administrator roles may change (`AutomationsVisuController`). Only rules with `userAccess` are visible; anything else answers 404.

| Method | Route | Body | Returns |
|---|---|---|---|
| GET | `` | | `AutomationVisuDto[]` |
| PUT | `{id}` | `{ "blocks": [{ "path": "triggers/0", "values": { "at": "07:15" } }] }` | `AutomationVisuDto` |
| POST | `{id}/enabled` | `{ "enabled": bool }` | `AutomationVisuDto` |

A path or field that is not released answers 400 `{ "errors": [...] }`. Changes that leave an enabled rule with errors, and switching on a rule with errors, answer 400 `{ "problems": [{ path, code, detail }] }`. The engine reloads the rule after a change.

```ts
interface AutomationVisuDto {
  objId: string; name: string; description: string | null; isEnabled: boolean;
  this2AreaInstance: string | null; this2CategoryInstance: string | null;
  lastRunAt: string | null; lastRunState: RunState | null;
  canEdit: boolean;                 // visu or administrator role
  blocks: { path: string; kind: "trigger" | "condition" | "action"; type: string;
            label: string | null;   // name of the datapoint or control
            fields: string[];       // the released fields
            block: any }[];         // the whole block
}
```

The visualization shows them at `/visualization/automations` (menu entry only when at least one rule is released).

## MCP tools (`Automatica.Core.Mcp/Tools/AutomationTools.cs`)

Callers with the admin role or the MCP API key can see automations. `run_automation` and `set_automation_enabled` also need write access (`mcpAllowWrite` for the API key).

| Tool | Does |
|---|---|
| `list_automations(query?)` | name, enabled, mode, active runs, last run state/error |
| `get_automation_runs(id, take=5)` | runs with every step, so the caller can explain why an automation did or did not act |
| `run_automation(id, skipConditions=false)` | started / queued / not started (single mode busy or run limit reached) |
| `set_automation_enabled(id, enabled)` | switches on or off, stops running runs when switched off |

## Tests

`Tests/Automatica.Core.Tests/Automation/` runs the engine against a fake environment, an in-memory store and a `FakeTimeProvider`.

# HTTP

Virtual HTTP inputs and outputs: poll web pages and APIs and read values from them, send HTTP commands to devices.

```
HTTP                         allow local host
├── HTTP input               URL, polling interval, timeout, user, password
│   ├── Online               bool, true while the URL answers
│   ├── Value                double  ┐
│   ├── Digital value        bool    ├ read with + pattern
│   └── Text                 string  ┘
└── HTTP output              address, timeout, user, password
    ├── Status               int, HTTP status of the last command (0 = no answer)
    ├── Command              double, method + command/body for ON and OFF
    └── Digital command      bool
```

## Inputs

Every polling interval the URL is requested with GET. Each value below the input reads one value from the answer:

| Read with | Pattern | Example |
|---|---|---|
| JSON path | `$.a.b[0].c`, `a.b.0.c`, `$['a b']` | `$.sensors[0].temp` |
| Regular expression | the first group that matched, or the whole match | `temp=([0-9.]+)` |
| Command recognition | Loxone style, see below | `\iTemperature:\i\v` |
| Whole answer | – | |

Command recognition: `\iText\i` jumps behind the next `Text`, `\v` reads a number, `\h` a hexadecimal number,
`\1`..`\4` one character as byte 1..4 of an integer (low to high), `\sN` skips N characters, `\.` any character,
`\d` a digit, `\#` a number, `\w` a word, `\m` a letter or digit, `\a` a letter, `\t` `\r` `\n` `\xNN` `\\`
the character itself. Any other character must match exactly.

A value that is not found keeps its last value and the node shows the problem. Digital values understand
true/false, on/off, yes/no, open/closed and numbers (0 = off).

## Outputs

A command sends a request when its value is written. The command is a path appended to the address
(`/relay/0?turn=on`) or a full URL. `<v>` in the command and the body is replaced by the value, `<v.1>` with one
decimal; a digital command uses 1 and 0.

- A value other than 0 (true) sends the ON command, 0 (false) the OFF command.
- Without an OFF command a command sends the ON command with 0, a digital command sends nothing.
- POST and PUT send the body with the content type (default `application/json`).

## Security

- Only `http` and `https`. User and password belong in the properties (basic authentication), not in the URL.
  The password is stored encrypted. Over `http` it travels unencrypted, the log warns about it.
- The address is checked right before each connection, after DNS: the server itself (localhost) is blocked unless
  **Allow local host** is on; link-local addresses (169.254.x.x with cloud metadata services, fe80::), multicast,
  broadcast and 0.0.0.0 are always blocked. Devices in the local network and the internet are allowed.
- Redirects are not followed, no proxy is used, answers larger than 1 MB are dropped.
- Regular expressions get 250 ms per answer.
- The log shows URLs without their query, so tokens in a query stay out of it.

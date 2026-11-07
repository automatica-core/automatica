# Introduction 

[![Build Status](https://automatica-core.visualstudio.com/automatica/_apis/build/status/Plugins/Logics/P3.Rule.Logic.BaseOperations?branchName=develop)](https://automatica-core.visualstudio.com/automatica/_build/latest?definitionId=36&branchName=develop)

Automatica.Core.Logic rules. 

## Blocks

| Block | What it does |
|---|---|
| And, Or, Exclusive or, Not | Logic gates |
| If | True value if I1 = I2, otherwise the false value |
| Binary decoder | Splits a value into bits |
| RS flip-flop / SR flip-flop | Set/reset memory; reset (RS) or set (SR) wins when both are 1 |
| Push switch | Impulse relay: every 1 on T toggles, S sets, R resets |
| Edge detection | Pulse on R for 0 -> 1, on F for 1 -> 0 |
| Multiplexer | Passes the input selected by S (1-4) to the output |

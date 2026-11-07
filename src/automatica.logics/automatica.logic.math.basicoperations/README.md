# Introduction 

[![Build Status](https://automatica-core.visualstudio.com/automatica/_apis/build/status/Plugins/Logics/P3.Rule.Math.BasicOperations?branchName=develop)](https://automatica-core.visualstudio.com/automatica/_build/latest?definitionId=37&branchName=develop)

Automatica.Core.Math rules. 

## Blocks

| Block | What it does |
|---|---|
| Sum, Subtraction, Multiplication, Division, Modulo, Floor, Ceiling, Random | Basic arithmetic |
| Min/Max/Average | Smallest, largest and average value of the inputs that have a value |
| Up/down counter | + and - count by the step, R resets, Q = count reached the limit |
| Formula | Free formula over I1-I4, e.g. `if(I1 > 25, 100, 0)`; operators `+ - * / % ^`, comparisons, `&& \|\| !`, functions `abs sign min max round floor ceil sqrt pow exp ln log sin cos tan clamp if` |
| Scaler | Linear conversion from one range to another, optionally limited |
| PID controller | Controls an actual value to a setpoint once per cycle, output limited with anti-windup; Kd = 0 is a PI controller |

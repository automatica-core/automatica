# Automatica KNX Driver
Automatica.Core supports KNX by using an IP Interface.

## How to
Select the `Ethernet` node and go to `New`, you should find a node named `KNX IP Gateway`.

## Gateway properties
* IP Adress
    * The IP Adress of your IP Interface
* Port
    * The port used to connect to your IP Interface (default 3671)
* Use NAT
    * Used to connect in NAT Modus

## Group Adresses
Group adresses are added as you know it from ETS. You have a hirchachical structure. Currently only 3-Level installations are supported.

## DPTs
At the moment only DPTs from 1-10 are supported. Add your main DPT node, in the value node you can select the specific DPT.
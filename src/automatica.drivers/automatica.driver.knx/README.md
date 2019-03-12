# Introduction 

[![Build Status](https://automatica-core.visualstudio.com/automatica/_apis/build/status/Plugins/Drivers/P3.Driver.Knx.DriverFactory?branchName=develop)](https://automatica-core.visualstudio.com/automatica/_build/latest?definitionId=25&branchName=develop)


## KNX Baos
With the KNX Baos 830 kBerry Modul you can use your automatica.core system to connect with a knx system. For the configuration of the BAOS Module I want to refer you to the documentation from Weinzierl [here](https://www.weinzierl.de/index.php/de/alles-knx1/knx-module/knx-baos-modul-838).


### How to use
 KNX Baos can be added in the Virtual node.

 ![Baos1](https://github.com/automatica-core/automatica.driver.knx/blob/master/images/Screenshot_kberry_1.png?raw=true)

 After adding the kBerry BAOS node you can add your datapoints on the "Datapoints" node.

 ![Baos2](https://github.com/automatica-core/automatica.driver.knx/blob/master/images/Screenshot_kberry_2.png?raw=true)

 After adding a datapoint you can set the datapoint id (address) in the property grid.

 ![Baos3](https://github.com/automatica-core/automatica.driver.knx/blob/master/images/Screenshot_kberry_3.png?raw=true)


Save & reload your configuration and use your defined baos datapoints.
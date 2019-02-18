# Introduction 

[![Build Status](https://automatica-core.visualstudio.com/automatica/_apis/build/status/Plugins/Drivers/P3.Driver.Constants?branchName=develop)](https://automatica-core.visualstudio.com/automatica/_build/latest?definitionId=22&branchName=develop)

Automatica.Core.Constants driver is used to provide constant values as datapoints. Some constants are real constants like. But you can also create your own constants with any value you provide and use it across the system.

* PI
* PI * 2
* PI / 2
 

 # How to use
 Constants can be added in the Virtual node.

 ![Constants1](/images/Screenshot_1.png) Add Constants

 After adding the Constants node you can add pre-defined constants or define your own constant.

 ![Consants2](/images/Screenshot_2.png) Add Constant

 After adding the Consant node you can define its value in the property grid on the left.

 ![Constant3](/images/Screenshot_3.png) Define value


Save & reload your configuration and use your defined constants. Consants values will be dispatched every 10 second.
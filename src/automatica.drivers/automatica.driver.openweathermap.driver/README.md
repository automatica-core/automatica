# Introduction 

[![Build Status](https://automatica-core.visualstudio.com/automatica/_apis/build/status/Plugins/Drivers/P3.Driver.OpenWeatherMap?branchName=develop)](https://automatica-core.visualstudio.com/automatica/_build/latest?definitionId=31&branchName=develop)

OpenWeatherMap provides data from the OpenWeatherMap api. 


# Nodes
* Wind speed
    * Unit: Meter per Second
* Sunset
    * Sunset time
* Wind direction
    * Unit: degrees
* Pressure
    * Unit: hPa
* Temperature
    * Unit: °C
* Humidity
    * Unit: %
* Sunrise
    * Sunrise time
* Temperature min
    * Unit: °C
* Temperature max
    * Unit: °C

 # How to use
OpenWeatherMap can be added in the Virtual node.

 ![OWM1](https://github.com/automatica-core/automatica.driver.openweathermap.driver/blob/master/images/Screenshot_1.png?raw=true)

 ![OWM2](https://github.com/automatica-core/automatica.driver.openweathermap.driver/blob/master/images/Screenshot_2.png?raw=true)

 After adding the OpenWeatherMap node, you can see all the nodes which the driver provides for you. 

 ![OWM3](https://github.com/automatica-core/automatica.driver.openweathermap.driver/blob/master/images/Screenshot_3.png?raw=true)

You need to set an OpenWeatherMap api key to use the driver ([see here...](https://openweathermap.org/appid)).

 ![OWM4](https://github.com/automatica-core/automatica.driver.openweathermap.driver/blob/master/images/Screenshot_4.png?raw=true)
 OpenWeatherMap uses the Latitude & Longitude settings provided from the settings. You can change your global settings if you click on the root node and change the value in the property grid to the left.


Save & reload your configuration and use weather data in your Automatica.Core Server.  
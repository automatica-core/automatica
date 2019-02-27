# Amazon Alexa
In this article we will describe how you can use Amazon Alexa to switch on/off your lights.


# Automatica.Core configuration
We will simulate a HueBridge to archive our goal. Therefore we add a new HueBridge Simulator in our configuration.

![Screenshot](~/images/alexa/Screenshot_1.png)

Create the Hue Bridge Simulator.


![Screenshot2](~/images/alexa/Screenshot_2.png)

Create a On/Off device (eg. a lamp). In this case it is the ceiling in the living room (Wohnzimmer).

Press save & reload.


# Alexa configuration
Open your Amazon Alexa app on your mobile phone. Go to "Add device"

![Alexa1](~/images/alexa/AlexaApp1.jpg)

Select "Lamp".

![Alexa2](~/images/alexa/AlexaApp2.jpg)

For the brand use "Philips Hue"
![Alexa3](~/images/alexa/AlexaApp3.jpg)

Gateway type is V1.
![Alexa4](~/images/alexa/AlexaApp4.jpg)

Now Alexa is searching for devices.
![Alexa5](~/images/alexa/AlexaApp5.jpg)

If everything works fine - Alexa will show you how many new devices was found.
![Alexa6](~/images/alexa/AlexaApp6.jpg)

Optional you can add the device to a group.
![Alexa7](~/images/alexa/AlexaApp7.jpg)

Just say "Alexy switch <device> on!"
![Alexa8](~/images/alexa/AlexaApp8.jpg)


## Test


You can test it directly in the Alexa app by selecting the new added device and turning it on.

![Alexa9](~/images/alexa/AlexaApp9.jpg)


You should see the new value in the config tree.
![Screenshot3](~/images/alexa/Screenshot_3.png)
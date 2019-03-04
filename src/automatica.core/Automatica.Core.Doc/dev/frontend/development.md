# Development
As already mentioned - the frontend is built using Angular!

## Setting up the environment
First you need to be able to built the frontend. Therefore you need to install [node](https://nodejs.org/). 

After you installed node, you need to install the angular-cli.

~~~
$ npm install -g @angular/cli
~~~

## Getting the code
First step is to fork the automatica.core repository and add the upstream remote. You can place the forked repository anywhere on your system.

~~~
$ git clone git@github.com:YOUR_GIT_USERNAME/automatica.git
$ cd automatica/src/automatica.core/Automatica.WebNew
$ git remote add upstream https://github.com/automatica-core/automatica.git
~~~

## Build
The first time you build the frontend, you need to install all the node_modules using

~~~
$ npm install
~~~

This operation may take some minutes.

After that you can build the frontend with

~~~
$ npm run start
or 
$ ng b --watch
~~~

The dist folder is placed in src/automatica.core/Automatica.Core/wwwroot
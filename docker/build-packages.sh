#!/bin/bash


build() {
	for i in $1/*;
	do 
		for f in $(find $i -name 'automatica-manifest.json'); do 	
			WORKING_DIR=$(dirname $f)
			
			automatica-cli Pack -V $2 -W $WORKING_DIR -o /tmp
			
		done
	done
	
	for i in /tmp/*.acpkg;
	do
		unzip -o $i -d /app/automatica/$3
		rm $i
	done
	ls /app/automatica/$3
}

echo "building in $1 - version $2"

build $1/src/automatica.drivers $2 "Drivers"
build $1/src/automatica.logics $2 "Rules"

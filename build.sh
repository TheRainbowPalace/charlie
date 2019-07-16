#!/bin/bash

OUTPUTDIR="bin/Debug/net471"

msbuild charlie.csproj -nologo -verbosity:quiet

# Update resources
rm -fr $OUTPUTDIR/resources
cp -r resources $OUTPUTDIR

# Update recent build
zip releases/Charlie-dev.zip -r -q bin/Debug/net471/
tar -czvf releases/Charlie-dev.tar.gz bin/Debug/net471/
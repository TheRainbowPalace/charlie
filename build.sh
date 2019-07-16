#!/bin/bash

OUTPUTDIR="bin/Debug/net471"

msbuild charlie.csproj -nologo -verbosity:quiet

rm -fr $OUTPUTDIR/resources
cp -r resources $OUTPUTDIR
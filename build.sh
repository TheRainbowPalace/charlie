#!/bin/bash

csc -target:library -out:bin/SineExample.dll \
    -r:packages/CairoSharp.3.22.24.36/lib/netstandard2.0/CairoSharp.dll \
    src/SineExample.cs src/ISimulation.cs

msbuild sensor-positioning.csproj /verbosity:quiet
clear

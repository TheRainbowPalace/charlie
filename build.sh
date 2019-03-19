#!/bin/bash

# csc -target:library -out:bin/Examples.dll \
#     -r:packages/CairoSharp.3.22.24.36/lib/netstandard2.0/CairoSharp.dll \
#     src/Examples.cs src/ISimulation.cs

msbuild runcharlie-examples.csproj -nologo -verbosity:quiet
msbuild sensor-positioning.csproj -nologo -verbosity:quiet
msbuild runcharlie.csproj -nologo -verbosity:quiet
cp -r resources bin/RunCharlie


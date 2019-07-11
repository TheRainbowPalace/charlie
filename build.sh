#!/bin/bash

# csc -target:library -out:bin/Examples.dll \
#     -r:packages/CairoSharp.3.22.24.36/lib/netstandard2.0/CairoSharp.dll \
#     src/Examples.cs src/ISimulation.cs

msbuild charlie-examples.csproj -nologo -verbosity:quiet
msbuild charlie.csproj -nologo -verbosity:quiet


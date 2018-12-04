#!/bin/bash

msbuild sensor-positioning.csproj /verbosity:quiet \
&& clear && \
mono bin/Debug/sensor_positioning.exe

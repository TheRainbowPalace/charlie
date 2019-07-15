#!/bin/bash

./build.sh
./build_resources.sh
./build_icons.sh
rm -fr bin/Charlie.app
cp -r build-resources/mac-os-base bin/Charlie.app
cp build-resources/Charlie.icns bin/Charlie.app/Contents/Resources/icon.icns
cp -r bin/Charlie/* bin/Charlie.app/Contents/Resources

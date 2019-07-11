#!/bin/bash

LOGO="resources/logo1024"
ICONSET="build-resources/Charlie.iconset"

mkdir $ICONSET
sips -z 16 16     "$LOGO.png" --out "$ICONSET/icon_16x16.png"
sips -z 32 32     "$LOGO.png" --out "$ICONSET/icon_16x16@2x.png"
sips -z 32 32     "$LOGO.png" --out "$ICONSET/icon_32x32.png"
sips -z 64 64     "$LOGO.png" --out "$ICONSET/icon_32x32@2x.png"
sips -z 128 128   "$LOGO.png" --out "$ICONSET/icon_128x128.png"
sips -z 256 256   "$LOGO.png" --out "$ICONSET/icon_128x128@2x.png"
sips -z 256 256   "$LOGO.png" --out "$ICONSET/icon_256x256.png"
sips -z 512 512   "$LOGO.png" --out "$ICONSET/icon_256x256@2x.png"
sips -z 512 512   "$LOGO.png" --out "$ICONSET/icon_512x512.png"

# cp "$LOGO.png" "$ICONSET/icon_512x512@2x.png"
iconutil -c icns $ICONSET
rm -R $ICONSET

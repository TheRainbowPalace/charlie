#!/bin/bash

./build_mac_os.sh
VERSIONLINE=$(grep -E 'Version = "(\d+\.?){3}"' src/Charlie.cs)
VERSION=$(echo $VERSIONLINE |grep -Eo "(\d+\.?){3}")
zip "release/charlie-v$VERSION.zip" -r -q bin/Charlie bin/Charlie.app
echo "Created release/charlie-v$VERSION.zip"

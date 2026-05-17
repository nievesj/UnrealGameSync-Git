#!/usr/bin/env bash

set -e
set -o
set -u
set pipefail

cd build

mkdir -p UGSGit.app/Contents/Resources
mv UGSGit UGSGit.app/Contents/MacOS
cp resources/app/App.icns UGSGit.app/Contents/Resources/App.icns
sed "s/SOURCE_GIT_VERSION/$VERSION/g" resources/app/App.plist > UGSGit.app/Contents/Info.plist
rm -rf UGSGit.app/Contents/MacOS/UGSGit.dsym
rm -f UGSGit.app/Contents/MacOS/*.pdb

zip "ugsgit_$VERSION.$RUNTIME.zip" -r UGSGit.app

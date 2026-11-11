#!/usr/bin/env bash
# Publishes Katey from Linux as one self-contained, compressed Windows exe, katey.exe, into ./bin, with
# the manual and the two notice files beside it. Nothing needs installing on
# the Windows machine it runs on. Debug symbols and XML documentation never ship; the documentation is
# still generated, because the analyzer gate needs it, it is simply not published. Run with: bash publish.sh
# KATEY_VERSION, such as 1.2.0, stamps the build's version, which Katey reports to Spansh; 1.0.0 when unset.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
version="${KATEY_VERSION:-1.0.0}"

dotnet publish "$here/src/Katey.Ui" \
    -c Release -f net10.0 -r win-x64 --self-contained \
    -o "$here/bin" \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=none \
    -p:DebugSymbols=false \
    -p:PublishDocumentationFile=false \
    -p:PublishReferencesDocumentationFiles=false \
    -p:SatelliteResourceLanguages=en%3Bde%3Bes%3Bfr%3Bpt-BR%3Bru \
    -p:Version="$version"

# SkiaSharp and HarfBuzzSharp ship their native symbol files; they are not Katey's and do not ship.
rm -f "$here"/bin/*.pdb

# The manual and the legal notices travel with the executable.
cp "$here"/MANUAL*.md "$here/NOTICE.md" "$here/THIRD-PARTY-NOTICES.txt" "$here/bin/"
echo
echo "Published to $here/bin"
ls -1 "$here/bin"

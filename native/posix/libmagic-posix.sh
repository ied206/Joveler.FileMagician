#!/bin/bash
# Compile libmagic on Linux/macOS

# Usage:
#   ./libmagic-posix.sh ~/build/native/file-5.40 

# Check script arguments
if [[ "$#" -ne 1 ]]; then
    echo "Usage: $0 <FILE_SRCDIR>" >&2
    exit 1
fi
if ! [[ -d "$1" ]]; then
    echo "[$1] is not a directory!" >&2
    exit 1
fi
SRC_DIR=$1

# Query environment info
OS=$(uname -s) # Linux, Darwin, MINGW64_NT-10.0-19042, MSYS_NT-10.0-18363, ...

# Set path and command vars
# BASE_ABS_PATH: Absolute path of this script, e.g. /home/user/bin/foo.sh
# BASE_DIR: Absolute path of the parent dir of this script, e.g. /home/user/bin
if [ "${OS}" = Linux ]; then
    BASE_ABS_PATH=$(readlink -f "$0")
    CORES=$(grep -c ^processor /proc/cpuinfo)
    DEST_LIB="libmagic.so"
    DEST_EXE="file"
    STRIP="strip"
    CHECKDEP="ldd"
elif [ "${OS}" = Darwin ]; then
    BASE_ABS_PATH="$(cd $(dirname "$0");pwd)/$(basename "$0")"
    CORES=$(sysctl -n hw.logicalcpu)
    DEST_LIB="libmagic.dylib"
    DEST_EXE="file"
    STRIP="strip -x"
    CHECKDEP="otool -L"
else
    echo "${OS} is not a supported platform!" >&2
    exit 1
fi
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR="${BASE_DIR}/build"

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"

# Compile libmagic
# Adapted from https://wimlib.net/git/?p=wimlib;a=tree;f=tools/make-windows-release;
BUILD_MODES=( "lib" "exe" )
pushd "${SRC_DIR}" > /dev/null
for BUILD_MODE in "${BUILD_MODES[@]}"; do
    CONFIGURE_ARGS=""
    if [ "$BUILD_MODE" = "lib" ]; then
        CONFIGURE_ARGS="--enable-static=no --enable-shared=yes"
    elif [ "$BUILD_MODE" = "exe" ]; then
        CONFIGURE_ARGS="--enable-static=yes --enable-shared=no"
    fi

    make clean
    ./configure \
        --disable-zlib \
        --disable-bzlib \
        --disable-xzlib \
        --disable-zstdlib \
        --disable-lzlib \
        ${CONFIGURE_ARGS}
    make "-j${CORES}"

    if [ "$BUILD_MODE" = "lib" ]; then
        cp "src/.libs/${DEST_LIB}" "${DEST_DIR}"
        cat magic/Magdir/* > "${DEST_DIR}/magic.src"
        cp COPYING "${DEST_DIR}"
    elif [ "$BUILD_MODE" = "exe" ]; then
        cp "src/${DEST_EXE}" "${DEST_DIR}"
        cp magic/magic.mgc "${DEST_DIR}"
    fi
done
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh "${DEST_LIB}" "${DEST_EXE}"
${STRIP} "${DEST_LIB}" "${DEST_EXE}"
ls -lh "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null


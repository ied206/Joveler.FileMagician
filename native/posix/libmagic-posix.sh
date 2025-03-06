#!/bin/bash
# Compile libmagic on Linux/macOS

# Usage:
#   ./libmagic-posix.sh ~/build/native/file-5.46 

function print_help() {
    echo "Usage: $0 [-a x86_64|armhf|aarch64] <SRC_DIR>" >&2
    echo "" >&2
    echo "-a: Specify architecture for cross-compiling (Optional)" >&2
}

# Check script arguments
CROSS_ARCH=""
CROSS_TRIPLE=""
while getopts "a:h" opt; do
    case $opt in
        a) # pre-defined Architecture for cross-compile
            CROSS_ARCH=$OPTARG
            ;;
        h)
            print_help
            exit 1
            ;;
        :)
            print_help
            exit 1
            ;;
    esac
done
# Parse <SRC_DIR>
shift $(( OPTIND - 1 ))
SRC_DIR="$@"
if ! [[ -d "${SRC_DIR}" ]]; then
    print_help
    echo "Source [${SRC_DIR}] is not a directory!" >&2
    exit 1
fi

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
HOST_ARCH=$(uname -m)
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
DEST_DIR="${BASE_DIR}/build"

# Check command
function check_cmd() {
    local TOCHECK=$1
    local APT_INSTALL_PKG=$2
    local BREW_INSTALL_PKG=$3
    which $TOCHECK > /dev/null
    if [[ $? -ne 0 ]]; then
        echo "Please install ${TOCHECK}!" >&2
        if [[ "${OS}" == Linux && "${APT_INSTALL_PKG}" != "" ]]; then 
            echo "Run \"sudo apt install ${APT_INSTALL_PKG}\"." >&2
        elif [[ "${OS}" == Darwin && "${BREW_INSTALL_PKG}" != "" ]]; then
            echo "Run \"brew install ${BREW_INSTALL_PKG}\"." >&2
        fi
        exit 1
    fi
}

# Set target triple (for Linux) or mac_arch (for macOS)
TARGET_TRIPLE=""
TARGET_MAC_ARCH=""
if [[ "${CROSS_ARCH}" != "" ]]; then
    if [[ "${OS}" == Linux ]]; then
        if [[ "${CROSS_ARCH}" == i686 ]]; then
            TARGET_TRIPLE="i686-linux-gnu"
        elif [[ "${CROSS_ARCH}" == x86_64 ]]; then
            TARGET_TRIPLE="x86_64-linux-gnu"
        elif [[ "${CROSS_ARCH}" == armhf ]]; then
            TARGET_TRIPLE="arm-linux-gnueabihf"
        elif [[ "${CROSS_ARCH}" == aarch64 || "${CROSS_ARCH}" == arm64 ]]; then
            TARGET_TRIPLE="aarch64-linux-gnu"
        elif [[ "${CROSS_ARCH}" != "" ]]; then
            echo "[${ARCH}] is not a pre-defined architecture" >&2
            exit 1
        fi

        if [ "${TARGET_TRIPLE}" != "" ]; then
            echo "(Cross compile) Target triple set to [${TARGET_TRIPLE}]"
        fi 

        check_cmd "${TARGET_TRIPLE}-gcc" "gcc-${TARGET_TRIPLE}"
        check_cmd "${TARGET_TRIPLE}-ld" "binutils-${TARGET_TRIPLE}"
        check_cmd "${TARGET_TRIPLE}-strip" "binutils-${TARGET_TRIPLE}"

        STRIP="${TARGET_TRIPLE}-strip"
        DEST_DIR="${DEST_DIR}-${CROSS_ARCH}"
    elif [[ "${OS}" == Darwin ]]; then
        # https://developer.apple.com/documentation/apple-silicon/building-a-universal-macos-binary
        # https://gist.github.com/andrewgrant/477c7037b1fc0dd7275109d3f2254ea9
        if [[ "${CROSS_ARCH}" == x86_64 ]]; then
            TARGET_MAC_ARCH="x86_64"
        elif [[ "${CROSS_ARCH}" == aarch64 || "${CROSS_ARCH}" == arm64 ]]; then
            TARGET_MAC_ARCH="arm64"
        elif [[ "${CROSS_ARCH}" != "" ]]; then
            echo "[${ARCH}] is not a pre-defined architecture" >&2
            exit 1
        fi

        if [[ "${TARGET_MAC_ARCH}" != "" ]]; then
            DEST_DIR="${DEST_DIR}-${TARGET_MAC_ARCH}"
            echo "(Cross compile) Target architecture set to [${TARGET_MAC_ARCH}]"
        fi 
    fi
fi

# Cross compile
if [[ "${TARGET_TRIPLE}" != "" ]]; then
    EXTRA_ARGS="${EXTRA_ARGS} --host=${TARGET_TRIPLE}"
fi 
if [[ "${TARGET_MAC_ARCH}" != "" ]]; then
    CPPFLAGS="${CPPFLAGS} -arch ${TARGET_MAC_ARCH}"
    CFLAGS="${CFLAGS} -arch ${TARGET_MAC_ARCH}"
    LDFLAGS="${LDFLAGS} -arch ${TARGET_MAC_ARCH}"
    #CPPFLAGS="${CPPFLAGS} --target=${TARGET_ARCH}"
    #CFLAGS="${CFLAGS} --target=${TARGET_ARCH}"
    #LDFLAGS="${LDFLAGS} --target=${TARGET_ARCHi}"
fi

# Create dest directory
rm -rf "${DEST_DIR}"
mkdir -p "${DEST_DIR}"

# [*] Let custom toolchain can be called first in PATH
if ! [[ -z "${TOOLCHAIN_DIR}" ]]; then
    export PATH=${TOOLCHAIN_DIR}/bin:${PATH}
fi

# Compile libmagic
# Adapted from https://wimlib.net/git/?p=wimlib;a=tree;f=tools/make-windows-release;
BUILD_MODES=( "exe" "lib" )
pushd "${SRC_DIR}" > /dev/null
for BUILD_MODE in "${BUILD_MODES[@]}"; do
    CONFIGURE_ARGS=""
    if [ "$BUILD_MODE" = "lib" ]; then
        CONFIGURE_ARGS="--disable-static --enable-shared"
    elif [ "$BUILD_MODE" = "exe" ]; then
        CONFIGURE_ARGS="--enable-static --disable-shared"
    fi

    make clean
    ./configure --host=${TARGET_TRIPLE} \
        --disable-zlib \
        --disable-bzlib \
        --disable-xzlib \
        --disable-zstdlib \
        --disable-lzlib \
        --disable-lrziplib \
        CFALGS="-Os" \
        ${CONFIGURE_ARGS}
    make "-j${CORES}"

    if [ "$BUILD_MODE" = "lib" ]; then
        cp "src/.libs/${DEST_LIB}" "${DEST_DIR}"
        cat magic/Magdir/* > "${DEST_DIR}/magic.src"
        cp COPYING "${DEST_DIR}"
    elif [ "$BUILD_MODE" = "exe" ]; then
        cp "src/${DEST_EXE}" "${DEST_DIR}"
        if [[ "${CROSS_ARCH}" == ""  || "${CROSS_ARCH}" == "${HOST_ARCH}" ]]; then
            cp magic/magic.mgc "${DEST_DIR}"
        fi
    fi
done
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh "${DEST_LIB}" "${DEST_EXE}"
${STRIP} "${DEST_LIB}" "${DEST_EXE}"
file "${DEST_LIB}" "${DEST_EXE}"
ls -lh "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null

# Check dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null

#!/bin/bash
# Compile libmagic for Windows on MSYS2
#   libgnurx part adopted from https://github.com/nscaife/file-windows

# Usage:
#   ./libmagic-msys2.sh -a i686 /d/build/native/file-5.40 
#   ./libmagic-msys2.sh -a x86_64 /d/build/native/file-5.40 
#   ./libmagic-msys2.sh -a aarch64 -t /c/llvm-mingw /d/build/native/file-5.40 

# Check script arguments
while getopts "a:t:" opt; do
  case $opt in
    a) # architecture
      ARCH=$OPTARG
      ;;
    t) # toolchain, required for aarch64
      TOOLCHAIN_DIR=$OPTARG
      ;;
    :)
      echo "Usage: $0 <-a i686|x86_64|aarch64> [-t TOOLCHAIN_DIR] <FILE_SRCDIR>" >&2
      exit 1
      ;;
  esac
done
# Parse <FILE_SRCDIR>
shift $(( OPTIND - 1 ))
SRC_DIR="$@"
if ! [[ -d "${SRC_DIR}" ]]; then
    echo "[${SRC_DIR}] is not a directory!" >&2
    exit 1
fi

# Set path and command vars
# BASE_ABS_PATH: Absolute path of this script, e.g. /home/user/bin/foo.sh
# BASE_DIR: Absolute path of the parent dir of this script, e.g. /home/user/bin
BASE_ABS_PATH=$(readlink -f "$0")
BASE_DIR=$(dirname "${BASE_ABS_PATH}")
GNURX_DIR="${BASE_DIR}/libgnurx-2.5"
DEST_DIR=${BASE_DIR}/build-${ARCH}
CORES=$(grep -c ^processor /proc/cpuinfo)

# Set library paths
GNURX_LIB="libgnurx-0.dll"
DEST_LIB="libmagic-1.dll"
DEST_EXE="file.exe"
STRIP="strip"
CHECKDEP="ldd"

# Set target triple
if [ "${ARCH}" = i686 ]; then
    TARGET_TRIPLE="i686-w64-mingw32"
elif [ "${ARCH}" = x86_64 ]; then
    TARGET_TRIPLE="x86_64-w64-mingw32"
elif [ "${ARCH}" = aarch64 ]; then
    TARGET_TRIPLE="aarch64-w64-mingw32"
    # Let custom toolchain is called first in PATH
    if [[ -z "${TOOLCHAIN_DIR}" ]]; then
        echo "Please provide llvm-mingw as [TOOLCHAIN_DIR] for aarch64 build." >&2
        exit 1
    fi
else
    echo "${ARCH} is not a supported architecture" >&2
    exit 1
fi

# Create dest directory
mkdir -p "${DEST_DIR}"

# Let custom toolchain is called first in PATH
if ! [[ -z "${TOOLCHAIN_DIR}" ]]; then
    export PATH=${TOOLCHAIN_DIR}/bin:${PATH}
fi

# Compile libgnurx
pushd "${GNURX_DIR}" > /dev/null
make clean
make "-j${CORES}" TARGET_TRIPLE=${TARGET_TRIPLE}-
cp "${GNURX_LIB}" "${DEST_DIR}"
cp COPYING.gnurx "${DEST_DIR}/COPYING.gnurx"
export CFLAGS="-I${GNURX_DIR}"
export LDFLAGS="-L${GNURX_DIR}"
# Use .dll.a instead for static compile, but beware of LGPL.
# export LDFLAGS="libgnurx.dll.a"
popd > /dev/null

# Compile libmagic
# If a target arch is not compatible with host arch, magic.mgc creation will fail, but it is ignorable.
# If you want to really prevent this, run make with 'FILE_COMPILE={PATH_TO_RUNNABLE_FILE_SAME_VER}'.
BUILD_MODES=( "lib" "exe" )
pushd "${SRC_DIR}" > /dev/null
for BUILD_MODE in "${BUILD_MODES[@]}"; do
    CONFIGURE_ARGS=""
    if [ "$BUILD_MODE" = "lib" ]; then
        CONFIGURE_ARGS="--enable-shared"
    elif [ "$BUILD_MODE" = "exe" ]; then
        CONFIGURE_ARGS="--disable-shared"
    fi

    make clean
    autoreconf -f -i # Required to use own libgnurx
    ./configure --host=${TARGET_TRIPLE} \
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

# Clean libgnurx
pushd "${GNURX_DIR}" > /dev/null
make clean
popd > /dev/null

# Strip a binary
pushd "${DEST_DIR}" > /dev/null
ls -lh *.dll *.exe
${STRIP} "${GNURX_LIB}" "${DEST_LIB}" "${DEST_EXE}" 
ls -lh *.dll *.exe
popd > /dev/null

# print dependency of a binary
pushd "${DEST_DIR}" > /dev/null
${CHECKDEP} "${GNURX_LIB}" "${DEST_LIB}" "${DEST_EXE}"
popd > /dev/null

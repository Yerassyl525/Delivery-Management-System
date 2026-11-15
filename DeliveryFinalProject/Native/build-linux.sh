#!/usr/bin/env sh
set -eu
cd "$(dirname "$0")"
cc --version | head -n 1
cc -std=c11 -O2 -fPIC -shared delivery_native.c -o libdelivery_native.so -lm
printf 'Built %s\n' "$(pwd)/libdelivery_native.so"

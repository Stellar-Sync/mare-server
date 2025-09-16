#!/bin/sh
cd ../../../
docker build -t kasuaberra/stellar-sync-server:latest . -f ../Dockerfile-StellarSyncServer --no-cache --pull --force-rm
cd Docker/build/linux-local
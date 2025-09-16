#!/bin/sh
cd ../../../
docker build -t kasuaberra/stellar-sync-staticfilesserver:latest . -f ../Dockerfile-StellarSyncStaticFilesServer --no-cache --pull --force-rm
cd Docker/build/linux-local
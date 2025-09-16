#!/bin/sh
cd ../../../
docker build -t kasuaberra/stellar-sync-services:latest . -f ../Dockerfile-StellarSyncServices --no-cache --pull --force-rm
cd Docker/build/linux-local
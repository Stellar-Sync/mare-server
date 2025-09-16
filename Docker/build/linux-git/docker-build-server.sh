#!/bin/sh
docker build -t kasuaberra/stellar-sync-server:latest . -f ../Dockerfile-StellarSyncServer-git --no-cache --pull --force-rm
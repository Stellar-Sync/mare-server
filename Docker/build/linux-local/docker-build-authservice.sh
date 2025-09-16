#!/bin/sh
docker build -t kasuaberra/stellar-sync-authservice:latest . -f ../Dockerfile-StellarSyncAuthService --no-cache --pull --force-rm
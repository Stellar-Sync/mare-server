#!/bin/sh
docker build -t kasuaberra/stellar-sync-authservice:latest . -f ../Dockerfile-StellarSyncAuthService-git --no-cache --pull --force-rm
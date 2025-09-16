#!/bin/sh
docker build -t kasuaberra/stellar-sync-services:latest . -f ../Dockerfile-StellarSyncServices-git --no-cache --pull --force-rm
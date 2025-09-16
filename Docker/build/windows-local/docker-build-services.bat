@echo off
cd ..\..\..\
docker build -t kasuaberra/stellar-sync-services:latest . -f Docker\build\Dockerfile-StellarSyncServices --no-cache --pull --force-rm
cd Docker\build\windows-local
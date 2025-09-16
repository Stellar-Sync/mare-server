@echo off
cd ..\..\..\
docker build -t kasuaberra/stellar-sync-server:latest . -f Docker\build\Dockerfile-StellarSyncServer --no-cache --pull --force-rm
cd Docker\build\windows-local
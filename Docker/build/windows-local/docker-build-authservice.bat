@echo off
cd ..\..\..\
docker build -t kasuaberra/stellar-sync-authservice:latest . -f Docker\build\Dockerfile-StellarSyncAuthService --no-cache --pull --force-rm
cd Docker\build\windows-local
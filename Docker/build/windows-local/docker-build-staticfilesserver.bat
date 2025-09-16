@echo off
cd ..\..\..\
docker build -t kasuaberra/stellar-sync-staticfilesserver:latest . -f Docker\build\Dockerfile-StellarSyncStaticFilesServer --no-cache --pull --force-rm
cd Docker\build\windows-local
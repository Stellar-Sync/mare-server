@echo off

docker build -t kasuaberra/stellar-sync-staticfilesserver:latest . -f ..\Dockerfile-StellarSyncStaticFilesServer-git --no-cache --pull --force-rm
@echo off
cd ..\..\..\
docker build -t kasuaberra/mare-synchronos-staticfilesserver:latest . -f Docker\build\Dockerfile-MareSynchronosStaticFilesServer --no-cache --pull --force-rm
cd Docker\build\windows-local
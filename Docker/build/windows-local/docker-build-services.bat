@echo off
cd ..\..\..\
docker build -t kasuaberra/mare-synchronos-services:latest . -f Docker\build\Dockerfile-MareSynchronosServices --no-cache --pull --force-rm
cd Docker\build\windows-local
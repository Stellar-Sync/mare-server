@echo off
cd ..\..\..\
docker build -t kasuaberra/mare-synchronos-authservice:latest . -f Docker\build\Dockerfile-MareSynchronosAuthService --no-cache --pull --force-rm
cd Docker\build\windows-local
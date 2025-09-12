@echo off

docker build -t kasuaberra/mare-synchronos-staticfilesserver:latest . -f ..\Dockerfile-MareSynchronosStaticFilesServer-git --no-cache --pull --force-rm
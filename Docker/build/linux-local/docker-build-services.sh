#!/bin/sh
cd ../../../
docker build -t kasuaberra/mare-synchronos-services:latest . -f ../Dockerfile-MareSynchronosServices --no-cache --pull --force-rm
cd Docker/build/linux-local
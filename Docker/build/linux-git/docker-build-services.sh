#!/bin/sh
docker build -t kasuaberra/mare-synchronos-services:latest . -f ../Dockerfile-MareSynchronosServices-git --no-cache --pull --force-rm
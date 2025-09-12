#!/bin/sh
docker build -t kasuaberra/mare-synchronos-server:latest . -f ../Dockerfile-MareSynchronosServer-git --no-cache --pull --force-rm
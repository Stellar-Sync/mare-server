#!/bin/sh
docker build -t kasuaberra/mare-synchronos-authservice:latest . -f ../Dockerfile-MareSynchronosAuthService-git --no-cache --pull --force-rm
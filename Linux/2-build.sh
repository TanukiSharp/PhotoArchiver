#!/usr/bin/env bash

CURRENT_USER=${1:-${USER}}

pushd ../PhotoArchiver
sudo dotnet publish -c Release -o /opt/PhotoArchiver
sudo chown -R ${CURRENT_USER}: /opt/PhotoArchiver
popd

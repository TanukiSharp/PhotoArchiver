#!/usr/bin/env bash

STATUS=`systemctl status PhotoArchiver | grep 'Active:' | awk '{print $2}'`

CURRENT_USER=${1:-${USER}}

if [ "${STATUS}" == "active" ]; then
    echo Stopping PhotoArchiver service...
    sudo systemctl stop PhotoArchiver
fi

pushd ../PhotoArchiver
sudo dotnet publish -c Release -o /opt/PhotoArchiver
sudo chown -R ${CURRENT_USER}: /opt/PhotoArchiver
popd

if [ "${STATUS}" == "active" ]; then
    echo Restarting PhotoArchiver service...
    sudo systemctl start PhotoArchiver
fi

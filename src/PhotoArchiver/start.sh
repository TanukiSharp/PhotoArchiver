#!/usr/bin/env bash

pid_file="process.pid"

if [ -f "$pid_file" ] ; then
    echo "process already running"
    exit 1
fi

nohup dotnet bin/Release/netcoreapp1.1/PhotoArchiver.dll >> ../../log.txt 2>&1 &
echo $! > $pid_file
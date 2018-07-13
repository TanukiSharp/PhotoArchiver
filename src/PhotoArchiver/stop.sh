#!/usr/bin/env bash

pid_file="process.pid"

if [ -f "$pid_file" ] ; then
    kill -15 `cat "$pid_file"`
    rm -f "$pid_file"
else
    echo "process not running"
    exit 1
fi

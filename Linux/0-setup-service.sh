#!/usr/bin/env bash

CURRENT_USER=${1:-${USER}}

cat PhotoArchiver.service | sed -e s/__USER_VARIABLE__/${CURRENT_USER}/ | sudo tee 1> /dev/null /etc/systemd/system/PhotoArchiver.service
sudo systemctl daemon-reload

#!/bin/bash

function wait_for_raven() {
    while true
    do
        if $(curl -s -i http://ravendb:8080 | grep -q "302 Found")
        then
            break
        fi
    done
}

wait_for_raven

cd test_data
npm install

node setup.js

echo "HERE!!!"

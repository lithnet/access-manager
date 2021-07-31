#!/bin/sh

echo -n "Enter the fully qualified DNS name of the Access Manager Server (eg ams.lithnet.io): "
read servername

echo -n "Enter the registration key: "
read regkey

/opt/LithnetAccessManagerAgent/Lithnet.AccessManager.Agent --server $servername --registration-key $regkey


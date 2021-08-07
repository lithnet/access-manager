#!/bin/sh
echo "This will uninstall the Lithnet Access Manager Agent, and remove all configuration for this machine"
read -p "Are you sure? " -n 1 -r
echo 
if [ $REPLY =~ ^[Yy]$ ]
then
	/bin/launchctl unload "/Library/LaunchDaemons/LithnetAccessManagerAgent.plist"
	rm /Library/LaunchDaemons/LithnetAccessManagerAgent.plist
	rm -rf /Applications/LithnetAccessManagerAgent
	rm -rf /Library/Application Support/LithnetAccessManagerAgent
fi


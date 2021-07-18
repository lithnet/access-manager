# Importing authorization rules
Access Manager provides several different mechanisms for creating authorization rules by discovering and importing permissions from external sources.

## Microsoft LAPS directory permissions
If you have an existing Microsoft LAPS deployment, you can search your directory for those users and groups you've assigned permission to read the Microsoft LAPS attribute, and convert those permissions to Access Manager authorization rules.

[[Importing Microsoft LAPS permissions]]

## BitLocker recovery directory permissions
If you have delegated permissions to read BitLocker recovery passwords in your environment, you can search your directory for users and groups with existing permissions to read the BitLocker attributes, and convert those permissions to Access Manager authorization rules.

[[Importing BitLocker permissions]]

## Import members of the local administrators group from computers
Access Manager can remotely connect to computers and obtain the list of members of the local administrators group of each computer, and consolidate that into a set of authorization rules.

[[Importing local administrators group membership]]

## Import user-to-computer mapping from a CSV file
You can prepare a CSV file of mappings between users and computer and Access Manager can consolidate that list into a set of authorization rules.

[[Importing mappings from a CSV file]]

## Import authorization targets from the Lithnet LAPS web app
If you had the Lithnet LAPS Web App installed (the predecessor to Lithnet Access Manager), you can import your `target` rules and notifications from the `web.config` file.

[[Importing rules from Lithnet Laps web app]]

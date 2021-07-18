# About
Lithnet Access Manager is a web-based service that allows users to request access to local admin passwords, bitlocker recovery keys, and request just-in-time access to computers. 

The service itself consists of two components.

The first is the Lithnet Access Manager Service (AMS). AMS is a web-based interface where users can request various types of access to computers. It's the core service of Access Manager.

The second component is an optional agent, called the Access Manager Agent (AMA). Depending on how you want to use Access Manager, you may need to deploy the Access Manager agent to your workstations. 

This document will guide you through the process for configuring Lithnet Access Manager, and the features you want to use in your environment.

# Installation
You'll need to first install and configure the Access Manager Service application. 

[[Installing the Access Manager Service]]

# Choosing the services to use
Once you've installed AMS, you need to decide if you need to deploy the Microsoft LAPS agent, or the Lithnet Access Manager agent. Use the following table below to map the AMS features you want to use with the agent you need to deploy.

| Feature | Requires Microsoft LAPS Agent | Requires Lithnet Access Manager Agent |
| --- | --- | --- |
| Access Microsoft LAPS passwords | ✔ | ❌ |
| Encrypt local admin passwords in the directory | ❌ | ✔ |
| Store a history of previous local admin passwords in the directory | ❌ | ✔ |
| Grant just-in-time admin access to computers | ❌ | ❌ |
| Access BitLocker recovery passwords | ❌ | ❌ |

AMS is fully compatible with Microsoft LAPS, but the Access Manager Agent provides advanced functionality not available with the Microsoft LAPS agent. To learn more, read the guide on [[Choosing a local admin password strategy]]. 

Note, that if the Microsoft LAPS agent is installed and enabled on a machine, the Lithnet LAPS agent will not take over password management. Either the Microsoft LAPS agent needs to be disabled by group policy, or uninstalled.

Once you've chosen the features to enable, follow the instructions in the relevant getting started guides.
- [[Setting up Microsoft LAPS]]
- [[Setting up password encryption and history]]
- [[Setting up JIT access]]
- [[Setting up BitLocker access]]

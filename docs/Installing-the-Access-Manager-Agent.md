# Prerequisites
In order to install the Access Manager Agent, the following prerequisites must be met
1. Windows 8.1 or Windows Server 2012 R2 or later 
2. [.NET Framework Runtime](https://dotnet.microsoft.com/download) 4.7.2 or later installed

We recommend using a configuration management tool such as SCCM to deploy the agent to your fleet. 

## Download and install the Access Manager Agent
1. Download the latest version of the agent from the [releases](https://github.com/lithnet/access-manager/releases/latest) page. Take note that you must install the x64 version on 64-bit machines, and the x86 version on 32-bit machines.

2. Run the AMA installation package. Follow the prompts to install the application.

## Configure the agent via Group Policy
The Access Manager agent doesn't do anything until it's configured via a group policy. Follow the [[[password encryption and history setup guide|Setting up password encryption and history]] for the correct process of setting up the relevant group policy settings.
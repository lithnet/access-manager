![](images/access-manager-logo.png)
# Lithnet Access Manager
Lithnet Access Manager is a tool that allows you to safely delegate sensitive administrative access to computers in your Active Directory environment in a modern and user-friendly way.

It provides a web-based interface that allows users to request local admin passwords, bitlocker recovery keys, and grant just-in-time administrative access to their own accounts. 

It is fully compatible and works out-of-the-box with Microsoft LAPS, or you can replace the Microsoft LAPS agent with the Lithnet Access Manager Agent, which encrypts local admin passwords in the directory, and can optionally save a history of local admin passwords. 

Access Manager provides a granular permission model, coupled with a detailed auditing system, both of which are extensible using PowerShell.

Modern authentication is a key feature of Access Manager, with support for OpenID Connect, allowing strong authentication and MFA with cloud-based identity providers such as Azure AD and Okta. On-premises providers have not been forgotten, with full support for WS-Federation (ADFS), smart cards, and if you need it, integrated windows authentication.

## Defend against ransomware and other lateral movement-based attacks
Access Manager has one simple goal. To reduce the likelihood and impact of a wide-spread compromise in your environment by removing permanent administrative access to your workstations and servers. By making sure every computer has a unique local admin password (through the use of Microsoft LAPS or the Lithnet Access Manager Agent), and removing all other members of the built-in local `Administrators` group, you can limit the ability for credential-stealing ransomware to move laterally across your environment. Access Manager makes it as seamless as possible for admins to access LAPS passwords, or grant themselves temporary just-in-time admin access. Access Manager isn't a silver bullet guaranteed to protect you from these sort of attacks, but it forms a fundamental part of a defence-in-depth strategy against them. 

We're a big believer in not having to pay for security 'minimums'. These days, attackers have a treasure-trove of commodity credential-stealing tools at their disposal. They don't have to pay for them, and we believe you shouldn't have to pay to defend against them. Therefore, this software is completely free and as always our code is open source and open to scrutiny. 

We recommend you have a look at our other product [Lithnet Password Protection for Active Directory](https://github.com/lithnet/ad-password-protection), for a tool to help strengthen your environment against commodity password-based attacks. 

## Features
### Web-based access to local admin passwords
Access Manager provides a simple web-based and mobile-friendly interface for accessing local admin passwords. There's no need for admins to install custom software, or have access to AD administrative tools to access LAPS passwords. 

<img src="images/web-request-laps.gif" Width="400"/>

Administrators also have the option of forcing an expiry time when a password is accessed. This ensures that the password is rotated after use.

We use LAPS passwords ourselves, so we know they can be painful at times. We try to take away as much of that pain as possible. From using fonts where you can actually see the difference between an lower-case L and a capital I, to showing a breakdown of the password using the NATO phonetic alphabet to make it easy to read it out to someone. Ever find yourself needing to type a LAPS password into a Windows logon screen? Have access manager read the password to you while you type!
 
Whether you use Microsoft LAPS, or the Lithnet Access Manager agent, the user experience is the same.

### Access historical local admin passwords
Deploying the Lithnet Access Manager Agent to your fleet allows you to upgrade to encrypted local admin passwords and gain the benefit of having previous local admin passwords stored in the directory as well. This means no more issues getting locked out of computers when they are restored from backup or reverted from a snapshot.

<img src="images/web-request-laps-history.gif" Width="400"/>

### Just-in-time administrative access to computers
Using the same web interface, users can request that their account be added to a group that is a member of the local administrators group of the computer. This access is temporary and automatically removed after the allowed time period. Access Manager makes use of the Active Directory time-based membership feature in Windows Server 2016 and later domain functional levels, or time-based (dynamic) objects in earlier versions of AD.

<img src="images/web-request-jit.gif" Width="400"/>

### Easy access to BitLocker recovery passwords
Authorized users can also request access to the BitLocker recovery passwords for a computer through the same easy-to-use web interface.

<img src="images/web-request-bitlocker.gif" Width="400"/>

### Audit success and failure event logs
All success and failure events are logged to the Windows event log and a file. Optionally, you can send audit events via email, webhooks, and even PowerShell.

The webhook functionality makes it really easy to get alerts via Slack or Microsoft teams, and there are even built-in templates for these systems.

![](images/auditing-example-slack.png)

### Modern authentication options
The web app supports traditional integrated windows authentication, as well as external authentication providers such as [ADFS](https://github.com/lithnet/access-manager/wiki/Setting-up-authentication-with-ADFS) or 3rd party OpenID Connect providers such as [Azure AD](https://github.com/lithnet/access-manager/wiki/Setting-up-authentication-with-Azure-AD) and [Okta](https://github.com/lithnet/access-manager/wiki/Setting-up-authentication-with-Okta). Using an external authentication provider allows you the option of providing additional protections for the application such as multi-factor authentication.

## Guides
*   [Getting started](Getting-started.md)

## Download the app
Download the [current release](https://github.com/lithnet/access-manager/releases/latest)

## Help and support
Please read our [[getting support guide|Getting-support]] if you need help or encounter a problem


## Keep up to date
*   [Visit my blog](http://blog.lithnet.io)
*   [Follow me on twitter](https://twitter.com/RyanLNewington)![](https://twitter.com/favicon.ico)
*   [Follow us on twitter](https://twitter.com/lithnet_io)![](https://twitter.com/favicon.ico)
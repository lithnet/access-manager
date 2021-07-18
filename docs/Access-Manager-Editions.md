Access Manager comes in two editions - Standard and Enterprise.

# Standard Edition
Access Manager Standard edition is our core offering, that contains all the features that an organization need to help defend themselves from lateral movement-based attacks. You can provide your users full access to Microsoft LAPS passwords and request just-in-time admin access to computers all from the convenience of their browser.

Standard edition is completely free for any organization of any size to use.

# Enterprise Edition
 Enterprise edition adds additional functionality, such as support for Microsoft Failover Clusters for high availability, as well as additional features like just-in-time access to any service you can manage with AD, or with a PowerShell script.

See the [[licensing]] page for information on how to trial or purchase an Enterprise Edition license.

# Feature comparison

## Web Interface features
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Access to local admin passwords | x | x |
| Access to BitLocker recovery passwords | x | x |
| Just-in-time access requests for local admin access on computers | x | x |
| 'Read aloud' function for passwords (where supported by the browser) | x | x |
| Phonetic display of passwords | x | x |
| Access to local admin password history |  | x |
| Just-in-time access requests to admin-defined roles^ |  | x |

## Local admin password features 
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Read passwords set by the Microsoft LAPS client | x | x |
| Read passwords set by the Lithnet Access Manager agent | x | x |
| Encrypt local admin passwords using the Access Manager agent | x | x |
| Retain historical local admin passwords using the Access Manager agent | x | x |
| Trigger LAPS password change when the password has been accessed | x | x |
| PowerShell access to encrypted local admin passwords (from the AMS server only) | x | x |
| PowerShell access to encrypted local admin password history (from the AMS server only) | x | x |

## Just-in-time access features
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Just-in-time access to Windows computers | x | x |
| Just-in-time access to Active Directory groups^ |  | x |
| Just-in-time access to 3rd party services using custom PowerShell scripts^ |  | x |

## BitLocker features
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Read BitLocker recovery passwords from AD | x | x |


## Authentication features
Access Manager supports many different authentication mechanisms. Use a modern authentication provider like Azure AD or Okta to add MFA support to your Access Manager instance.
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Support for Integrated Windows Authentication | x | x |
| Support for OpenID Connect | x | x |
| Support for WS-Federation | x | x | 
| Support for smart-card authentication | x | x |

## Auditing and analytics features
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Log events to the Windows event log | x | x |
| Send audit notifications via webhooks | x | x |
| Send audit notifications via email | x | x | 
| Send audit notifications via custom PowerShell scripts | x | x | 
| Query access history from within the app^ |  | x |
| Email summary reports^ | | x |

## Infrastructure 
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Multi-domain support | x | x |
| Cross-forest trust support | x | x |
| Single-server deployments | x | x |
| Windows Failover Cluster deployments |  | x |
| Database support | SQL LocalDB  | SQL LocalDB<br>SQL Server Standard or Enterprise | 

## Authorization features
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| ACL-based authorization | x | x |
| Custom PowerShell script-based authorization | | x |
| Global rate-limiting on requests | x | x |
| Granular rate-limiting policies for users and groups^ |  | x |
| Import Microsoft LAPS permissions from Active Directory | x | x |
| Import BitLocker recovery password permissions from Active Directory | x | x |
| Import local admin permissions from computers | x | x | 
| Import permissions from CSV file | x | x |
| Import LAPS permissions from the Lithnet LAPS Web App | x | x |

## Support
| Feature | Standard Edition | Enterprise Edition |
| --- | :---: | :---: |
| Community support via GitHub | x | x |
| Priority support via email |  | x |



***^ Denotes features that are planned and coming soon***
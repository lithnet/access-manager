> ![](images/badge-enterprise-edition-rocket.svg)  High availability is an [[Enterprise edition feature|Access Manager Editions]]

![](images/ui-page-highavailability.png)

## Database configuration
By default, Access Manager uses an internal database instance, based on Microsoft SQL LocalDB. For most use cases, this database type is fine. 

If you have specific requirements around management, performance or availability, you may wish to use an external SQL server to host the Access Manager. You can select to create a database directly using the `Create database` function, or they can generate a database creation script that can be run on the SQL server directly to create the necessary database and permissions for the AMS service account.

## Data protection
> Data protection functionality requires at least one domain controller in the domain running Windows Server 2012 R2, and a [KDS root key](https://docs.microsoft.com/en-us/windows-server/security/group-managed-service-accounts/create-the-key-distribution-services-kds-root-key) must have been generated in the domain.

### Cluster-compatible secret encryption
In order to run AMS in a high availability configuration, such as a Windows failover cluster, cluster-compatible secret encryption must be enabled. 

### Encryption certificate synchronization
To ensure that each server in the cluster has access to the necessary decryption certificates you can enable certificate synchronization. This will encrypt the service certificates and store the in the configuration file. 

Do not use this option if you are using certificates stored on a 3rd party device such as a HSM. Configure the device to ensure that all nodes of the cluster have access to decrypt data using the certificate's private key.
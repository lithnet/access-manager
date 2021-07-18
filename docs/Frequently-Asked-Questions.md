## Do I need to deploy the Lithnet Access Manager Agent?
The Lithnet Access Manager Agent is only required if you have one of the following requirements
1. You want your local admin passwords encrypted in the directory
2. You want to store previous local admin passwords in the directory

If you want to use the Access Manager web app to access Microsoft LAPS passwords you do not need to use the Access Manager Agent.

Read our guide on [[Choosing a local admin password strategy]] to learn more about the differences between using the Microsoft LAPS agent and the Lithnet Access Manager Agent.

## How are directory passwords encrypted?
Lithnet Access Manager uses public-key cryptography to protect directory passwords. The Access Manager Service (AMS) creates a public and private key pair (RSA-PSS 4096-bit) and publishes the public key to the directory, and stores the private key in the service certificate store.

The encryption certificate is located in the Configuration naming context, in the `caCertificate` attribute of an object with the name `CN=AccessManagerConfig,CN=Lithnet,CN=Services,CN=Configuration,DC=X`

The Access Manager agent (AMA) obtains the encryption certificate from this location in the directory. It then generates a unique encryption key and uses AES-CBC-256 to encrypt the password with this key. The AES key is encrypted with the public key of the encryption certificate and the resulting blob converted to base-64 text and stored in the directory, along with the thumbprint of the certificate used to encrypt the key material.

## Can I use a HSM or similar to store the private key to the encryption certificate?
Yes you can, but it requires a bit of manual work.

Using the [certificate publishing template](https://github.com/lithnet/access-manager/blob/master/src/Lithnet.AccessManager/Lithnet.AccessManager.Server.UI/ScriptTemplates/Publish-LithnetAccessManagerCertificate.ps1) provided by access manager, manually replace the `$forest` variable to the dns name of the forest, and replace the `{certificateData}` placeholder with the base-64 encoded .cer file content.

Publish the certificate to the directory.

Then, simply ensure that the AMS server has access to the private key of that certificate from the system's personal certificate store.

Note, that the Access Manager Service Configuration app will not recognize this configuration and will show that the certificate is not published or missing. This can be ignored. The agent always obtains the certificate to encrypt with from the directory, and AMS simply looks for a certificate with a thumbprint matching the one used to encrypt the certificate in its local store. The configuration app simply provides an easy way to create and publish certificates.

## Can I rotate the encryption certificate?
Yes, you can create and publish a new encryption certificate at any time. Agents will encrypt their passwords using this new certificate at the next scheduled password rotation. Do note that the agents will not re-encrypt their previous passwords using the new certificates. Agents do not have access to the private key needed to decrypt the passwords.

As such, you need to ensure that the private key remains on the server, as long as there are passwords in the directory that were encrypted with the old certificate.

## Can computers decrypt their own admin passwords?
No. Passwords are encrypted using asymmetrical encryption. They encrypt the passwords using the public key of the encryption certificate and only the private key can decrypt this data. The private key remains only on the AMS server at all times.


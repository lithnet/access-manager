# Backing up AMS
You should regularly backup and restore your AMS config, and always backup before version updates.

## Backing up the configuration
To backup the AMS configuration, simply backup the `config` folder, located in the program files directory. 

`%ProgramFiles%\Lithnet\Access Manager Service\config`

If you have custom scripts or templates located outside of this folder, then back those up as well. We recommend keeping them in this folder to keep transport simple.

## Backing up the encryption certificate
If you have deployed the Access Manager Agent, and are using the encrypted password functionality, you'll need to ensure you have a backup of your password encryption certificate and it's private key. There is one encryption certificate per forest.

The easiest way to backup the encryption certificates for each forest is to use the AMS configuration tool. From the `Local admin passwords` page, select the forest that contains the certificate you want to backup, and click `View Certificate`. From the `Details` tab, click `Copy to file`, making sure to select the option to export the private key when prompted. Choose a very strong password, and store the resulting PFX somewhere very safe. Preferably in offline storage. Remember that access to this key will allow someone to decrypt all the local admin passwords in your domain.

Repeat the process for any other keys listed, in this forest, or other forests you have in your environment.

Alternatively, you can use `mmc.exe` to backup the certificates. Run `mmc.exe` and select the `File` menu and `Add/Remove snap in...`. Add the `certificates` snap in, and select `service account` followed by `local computer`. When prompted to select a service, choose the `Lithnet Access Manager Service`. All the encryption certificates are located in the `Personal` store of the service. Right click on each certificate and select `All tasks -> Export` to run the export wizard.

# Restoring AMS from a backup

## Restoring the configuration
To restore from backup, stop the Access Manager Service using the Windows services MMC. Close down the configuration editor if you have it open. Copy the contents of your config backup into the AMS configuration folder.

`%ProgramFiles%\Lithnet\Access Manager Service\config`

Open the Access Manager Configuration tool, and if you configuration is correct, you can start the AMS service.

## Restoring the encryption certificate
Run `mmc.exe` and select the `File` menu and `Add/Remove snap in...`. Add the `certificates` snap in, and select `service account` followed by `local computer`. When prompted to select a service, choose the `Lithnet Access Manager Service`. Once the console has loaded, right click on `lithnetams\Personal` and select `All tasks -> Import certificate`. Select your exported PFX files, provide the password, and import the certificate into the services personal store.

# Recovering from a lost encryption certificate private key
If you loose access to the encryption certificate's private key, any current and historical passwords encrypted with that key are not recoverable. This is why backups are so important. 

However, you can publish a new key, and force the agents to generate a new password and encrypt it with that key. See the guide on [[Recovering from a lost encryption certificate]] for more details.

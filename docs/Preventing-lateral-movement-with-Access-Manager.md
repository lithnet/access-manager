Lateral movement is a technique used by attackers, where after gaining initial access to one system, they obtain credentials that allow them to move into other hosts on the network.

Windows systems are particularly prone to lateral-movement based attacks. It is not uncommon for desktop support staff to administrators of all workstations, and server admin staff to be an administrator of every server. In some environments, the local admin password is the same on every machine. These configurations make it trivial to exploit a Windows environment.

Ransomware in particular loves this kind of setup. It seems every other week there is another organization in the news reported as falling victim to a ransomware attack. In most cases, large-scale ransomware attacks are successful because they manage to steal credentials of accounts that are admins on large numbers of computers.

So, how do you defend against these types of attacks? 

## 1. Secure the built-in administrator account
While every computer has its own local administrator account, if the password is the same it is effectively a single account. One that has access to every machine, and is not tied to AD. It is not audited in any AD logs, and there are no controls on its usage. It makes for a very attractive target for an attacker. If they obtain access to it, it is very easy for them to move through the environment undetected. All that is needed is a compromise of a single machine with this password.

This is why Microsoft LAPS is so important. It randomizes the password on each individual machine and changes it regularly, ensuring that if one machine is compromised and its local admin password exposed, that it cannot be used to access other machines on the network. 

Accessing a Microsoft LAPS agent is not the most user-friendly experience. You need to install a thick client, or use the advanced attribute editor of the Active Directory Users and Computers tool. For technicians out in the field this can be problematic, as they may be at a customer site without their own computer.

Lithnet Access Manager Service (AMS) provides mobile-friendly web-based access to LAPS passwords, so they can be accessed from any device with a browser. Combined with the ability to support modern authentication protocols like OpenID Connect, you can protect access to these passwords with multi-factor authentication.

The Lithnet Access Manager Agent (AMA) can also rotate the local admin password where Microsoft LAPS isn't used. AMA offers two features that the Microsoft LAPS agent does not. First, it encrypts passwords in the directory. I've heard some people who are unable or unwilling to deploy Microsoft LAPS because it stores passwords in plain-text in the directory. I don't personally believe this is a problem, but AMA can be used to eliminate this barrier of adoption.

The second feature helps in scenarios where machines are rolled back from a snapshot or restored from a backup. Using Microsoft LAPS can be problematic in this case. If the machine account password has been changed since the snapshot was taken, no one can log onto the machine with a domain account. If the LAPS password was rotated since the snapshot was taken, then not even the local administrator can log in. AMA can store the previous passwords in the directory, and record when they were in use, so you can easily get back into the computer in this scenario.

For added protection, it is highly recommended to [Deny local accounts access to the computer over the network](https://support.microsoft.com/en-au/help/4488256/how-to-block-remote-use-of-local-accounts-in-windows). This will ensure that the local admin password cannot be used over the network at all. It will only work for local logins.

## 2. Removing everyone from the local administrators group
So if we have secured the local admin password, what other lateral movement risks remain? If you think back to the attack scenario, where an attacker is looking for credentials on a machine they have compromised, what happens if an admin is logged on to the machine at the time? If that user has admin rights on other machines, then the attacker may be able to steal hashes, kerberos tickets, and in some cases the plain-text password. They can then move into other systems with those stolen credentials.

There are lots of things you can do to help mitigate these attacks, not all of them easy or 100% foolproof. We do encourage you to research and implement as many defences as you can to prevent lateral movement attacks. You need to choose the ones that make sense for your environment.

One of the simplest to achieve technically, that has a high degree of effectiveness, is to simply not have any permanent members of the local administrators group. 

While this sounds scary, Access Manager takes the technical complexity away from making such a change. Your administrators of course, will need to adapt to a new way of working, so getting appropriate buy-in from senior management is important. Like the other mitigations, it's not foolproof, and it's not perfect. It is however a relatively low complexity solution, that offers a high value.

The theory behind this approach, quite simply, is that if a computer is compromised, and the user is a local admin on that computer but no where else, then lateral movement with that account becomes very difficult. So instead of having all your admins as members of the local administrators group, you assign them 'just-in-time access' (JIT) rights in Access Manager. When they need to access a computer, they visit the Access Manager web site, request JIT access to the computer, and Access Manager will grant them the appropriate rights for the length of time you allow. They can then logon to the server, and perform whatever work they need to, just as they did before. You can read more about exactly [[how Access Manager performs just-in-time access|Setting up JIT access]].

The difference here is that they are entitled to be an administrator of the computers you specify in the Access Manager configuration tool, but they only get promoted to administrator when they need and request it.

This means that at any single point in time, you have relatively few users with admin rights across your fleet of computers, drastically reducing the ability for an attacker to spread across the fleet.

It is important to note at this point, that Access Manager is not a fully-fledged Privileged Access Management (PAM) tool. There are no approval workflows. You determine in advance who can access which computer, and they can self-grant those privileges at any time. There are plenty of commercial PAM solutions out there if you want to control who and when people can access certain resources. We're not trying to fix that problem. We just want our admins to be admins only when they need to do work.
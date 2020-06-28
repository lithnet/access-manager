using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    public enum AdsFormat
    {
        /// <summary>
        /// Returns the full path in Windows format, for example, "LDAP://servername/o=internet/…/cn=bar".
        /// </summary>
        Windows = 1,

        /// <summary>
        /// Returns Windows format without server, for example, "LDAP://o=internet/…/cn=bar".
        /// </summary>
        WindowsNoServer = 2,

        /// <summary>
        /// Returns Windows format of the distinguished name only, for example, "o=internet/…/cn=bar".
        /// </summary>
        WindowsDn = 3,

        /// <summary>
        /// Returns Windows format of Parent only, for example, "o=internet/…".
        /// </summary>
        WindowsParent = 4,

        /// <summary>
        /// Returns the full path in X.500 format, for example, "LDAP://servername/cn=bar,…,o=internet".
        /// </summary>
        X500 = 5,

        /// <summary>
        /// Returns the path without server in X.500 format, for example, "LDAP://cn=bar,…,o=internet".
        /// </summary>
        X500NoServer = 6,

        /// <summary>
        /// Returns only the distinguished name in X.500 format. For example, "cn=bar,…,o=internet".
        /// </summary>
        X500Dn = 7,

        /// <summary>
        /// Returns only the parent in X.500 format, for example, "…,o=internet".
        /// </summary>
        X500Parent = 8,

        /// <summary>
        /// Returns the server name, for example, "servername".
        /// </summary>
        Server = 9,

        /// <summary>
        /// Returns the name of the provider, for example, "LDAP".
        /// </summary>
        Provider = 10,

        /// <summary>
        /// Returns the name of the leaf, for example, "cn=bar".
        /// </summary>
        Leaf = 11
    }
}
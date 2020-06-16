namespace Lithnet.AccessManager.Interop
{
    public enum DsNameFormat
    {
        /// <summary>
        ///  The server looks up the name by using the algorithm specified in the LookupUnknownName procedure.
        /// </summary>
        DS_UNKNOWN_NAME = 0,

        /// <summary>
        /// A distinguished name.
        /// </summary>
        DS_FQDN_1779_NAME = 1,

        /// <summary>
        /// Windows NT 4.0 operating system (and prior) name format. The account name is in the format domain\user and the domain-only name is in the format domain\.
        /// </summary>
        DS_NT4_ACCOUNT_NAME = 2,

        /// <summary>
        /// A user-friendly display name.
        /// </summary>
        DS_DISPLAY_NAME = 3,

        /// <summary>
        /// Curly braced string representation of an objectGUID. The format of the string representation is specified in [MS-DTYP] section 2.3.4.3.
        /// </summary>
        DS_UNIQUE_ID_NAME = 6,

        /// <summary>
        /// A canonical name.
        /// </summary>
        DS_CANONICAL_NAME = 7,

        /// <summary>
        /// User principal name.
        /// </summary>
        DS_USER_PRINCIPAL_NAME = 8,

        /// <summary>
        /// Same as DS_CANONICAL_NAME except that the rightmost forward slash (/) is replaced with a newline character (\n).
        /// </summary>
        DS_CANONICAL_NAME_EX = 9,

        /// <summary>
        /// Service principal name (SPN).
        /// </summary>
        DS_SERVICE_PRINCIPAL_NAME = 10,

        /// <summary>
        ///  String representation of a SID (as specified in [MS-DTYP] section 2.4.2).
        /// </summary>
        DS_SID_OR_SID_HISTORY_NAME = 11,

        /// <summary>
        /// Not supported.
        /// </summary>
        DS_DNS_DOMAIN_NAME = 12
    }
}
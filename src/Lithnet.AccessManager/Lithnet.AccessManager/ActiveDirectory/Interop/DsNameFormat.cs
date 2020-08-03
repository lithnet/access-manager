namespace Lithnet.AccessManager.Interop
{
    public enum DsNameFormat
    {
        /// <summary>
        ///  The server looks up the name by using the algorithm specified in the LookupUnknownName procedure.
        /// </summary>
        Unknown = 0, //DS_UNKNOWN_NAME

        /// <summary>
        /// A distinguished name.
        /// </summary>
        DistinguishedName = 1, //DS_FQDN_1779_NAME

        /// <summary>
        /// Windows NT 4.0 operating system (and prior) name format. The account name is in the format domain\user and the domain-only name is in the format domain\
        /// </summary>
        Nt4Name = 2, //DS_NT4_ACCOUNT_NAME

        /// <summary>
        /// A user-friendly display name
        /// </summary>
        DisplayName = 3, //DS_DISPLAY_NAME

        /// <summary>
        /// Curly braced string representation of an objectGUID
        /// </summary>
        ObjectGuid = 6, //DS_UNIQUE_ID_NAME

        /// <summary>
        /// A canonical name
        /// </summary>
        CanonicalName = 7, //DS_CANONICAL_NAME

        /// <summary>
        /// User principal name
        /// </summary>
        UserPrincipalName = 8, //DS_USER_PRINCIPAL_NAME

        /// <summary>
        /// Same as DS_CANONICAL_NAME except that the rightmost forward slash (/) is replaced with a newline character (\n)
        /// </summary>
        CanonicalNameEx = 9, //DS_CANONICAL_NAME_EX

        /// <summary>
        /// Service principal name (SPN).
        /// </summary>
        ServicePrincipalName = 10, //DS_SERVICE_PRINCIPAL_NAME

        /// <summary>
        ///  String representation of a SID
        /// </summary>
        SecurityIdentifier = 11, //DS_SID_OR_SID_HISTORY_NAME
    }
}
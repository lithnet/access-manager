namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    public enum DsNameError
    {
        None = 0,

        // Generic processing error.
        Resolving = 1,

        // Couldn't find the name at all - or perhaps caller doesn't have
        // rights to see it.
        NotFound = 2,

        // Input name mapped to more than one output name.
        NotUnique = 3,

        // Input name found, but not the associated output format.
        // Can happen if object doesn't have all the required attributes.
        NoMapping = 4,

        // Unable to resolve entire name, but was able to determine which
        // domain object resides in.  Thus DS_NAME_RESULT_ITEM?.pDomain
        // is valid on return.
        DomainOnly = 5,

        // Unable to perform a purely syntactical mapping at the client
        // without going out on the wire.
        NoSyntacticalMapping = 6,

        // The name is from an external trusted forest.
        TrustReferral = 7
    }
}
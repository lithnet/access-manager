namespace Lithnet.AccessManager.Interop
{
    /// <summary>
    /// The DS_NAME_FLAGS enumeration is used to define how the name syntax will be cracked. These flags are used by the DsCrackNames function.
    /// </summary>
    public enum DsNameFlags
    {
        /// <summary>
        /// Indicates that there are no associated flags.
        /// </summary>
        DS_NAME_NO_FLAGS = 0,

        /// <summary>
        /// Performs a syntactical mapping at the client without transferring over the network. The only syntactic mapping supported is from DistinguishedName to DS_CANONICAL_NAME or DS_CANONICAL_NAME_EX. DsCrackNames returns the DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING flag if a syntactical mapping is not possible.
        /// </summary>
        DS_NAME_FLAG_SYNTACTICAL_ONLY = 1,

        /// <summary>
        /// Forces a trip to the domain controller for evaluation, even if the syntax could be cracked locally.
        /// </summary>
        DS_NAME_FLAG_EVAL_AT_DC = 2,

        /// <summary>
        /// The call fails if the domain controller is not a global catalog server.
        /// </summary>
        DS_NAME_FLAG_GCVERIFY = 4,

        /// <summary>
        /// Enables cross forest trust referral.
        /// </summary>
        DS_NAME_FLAG_TRUST_REFERRAL = 8
    }
}
using System;

namespace Lithnet.AccessManager.Interop
{
    [Flags]
    internal enum AuthzAccessCheckFlags : uint
    {
        /// <summary>
        /// If phAccessCheckResults is not NULL, a deep copy of the security descriptor is copied to the handle referenced by phAccessCheckResults. 
        /// </summary>
        None = 0,

        /// <summary>
        /// A deep copy of the security descriptor is not performed. The calling application must pass the address of an AUTHZ_ACCESS_CHECK_RESULTS_HANDLE handle in phAccessCheckResults. The AuthzAccessCheck function sets this handle to a security descriptor that must remain valid during subsequent calls to AuthzCachedAccessCheck. 
        /// </summary>
        NoDeepCopySd = 0x00000001
    }
}

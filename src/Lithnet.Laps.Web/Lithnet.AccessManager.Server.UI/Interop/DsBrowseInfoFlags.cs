using System;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum DsBrowseInfoFlags
    {
        /// <summary>
        /// The + and - buttons are not displayed in the dialog box.
        /// </summary>
        NoButtons = 0x1,

        /// <summary>
        /// The lines that connect the objects in the dialog box are not displayed.
        /// </summary>
        NoLines = 0x2,

        /// <summary>
        /// The lines and buttons above the root objects are not displayed.
        /// </summary>
        NoLinesAtRoot = 0x4,

        /// <summary>
        /// Causes a check box to be placed next to each item in the tree. The user can use the mouse to select and clear this check box. This currently has limited usage because there is no way to set or get the check state of an item.
        /// </summary>
        CheckBoxes = 0x100,

        /// <summary>
        /// The root object, specified by pszRoot, is not displayed and the immediate child objects of the root are displayed at the root of the tree. This flag has no effect if pszRoot is NULL or if this member contains DSBI_ENTIREDIRECTORY.
        /// </summary>
        NoRoot = 0x10000,

        /// <summary>
        /// Include hidden objects in the dialog box.
        /// </summary>
        IncludeHidden = 0x20000,

        /// <summary>
        /// When the dialog box opens, the container specified in pszPath will be visible and selected.
        /// </summary>
        ExpandOnOpen = 0x40000,

        /// <summary>
        /// Includes all the trusted domains to the server specified in pszRoot or, by default, the domain that the user is logged in to.
        /// </summary>
        EntireDirectory = 0x90000,

        /// <summary>
        /// Indicates that the dwReturnFormat member is valid. If this flag is not set, the path format defaults to X.500.
        /// </summary>
        ReturnFormat = 0x100000,

        /// <summary>
        /// pUserName and pPassword are used for the access credentials. Otherwise, if this member does not contain DSBI_SIMPLEAUTHENTICATE, the dialog uses the security context of the calling thread.
        /// </summary>
        HasCredentials = 0x200000,

        /// <summary>
        /// When determining if the object is displayed in the dialog box, the treatAsLeaf display specifier is ignored.
        /// </summary>
        IgnoreTreatAsLeaf = 0x400000,

        /// <summary>
        /// Indicates that secure authentication is not required when calling ADsOpenObject.
        /// </summary>
        SimpleAuthenticate = 0x800000,

        /// <summary>
        /// Indicates that the pszObjectClass and cchObjectClass are valid and should be filled.
        /// </summary>
        ReturnObjectClass = 0x1000000,

        /// <summary>
        /// Indicates that signing and sealing will not be used when communicating with the directory service.
        /// </summary>
        DontSignSeal = 0x2000000

    }
}
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public static class FileExtensions
    {
        public static void AddFileSecurity(this FileInfo info, IdentityReference account, FileSystemRights rights, AccessControlType controlType)
        {
            FileSecurity fSecurity = info.GetAccessControl();

            fSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, controlType));

            info.SetAccessControl(fSecurity);
        }

        public static void AddDirectorySecurity(this DirectoryInfo info, IdentityReference account, FileSystemRights rights, AccessControlType controlType)
        {
            DirectorySecurity fSecurity = info.GetAccessControl();

            fSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, controlType));

            info.SetAccessControl(fSecurity);
        }
    }
}

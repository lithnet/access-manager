using System;

namespace Lithnet.Laps.Web.Models
{
    public interface IDirectory
    {
        IComputer GetComputer(string computerName);
        Password GetPassword(IComputer computer);
        void SetPasswordExpiryTime(IComputer computer, DateTime time);
        bool IsComputerInOu(IComputer computer, string ou);
		IGroup GetGroup(string groupName);
        bool IsComputerInGroup(IComputer computer, IGroup group);
        bool IsUserInGroup(IUser user, IGroup group);
        IUser GetUser(string userName);
    }
}

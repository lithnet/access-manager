using System;

namespace Lithnet.Laps.Web.Models
{
    public interface IDirectory
    {
        IComputer GetComputer(string computerName);
        Password GetPassword(IComputer computer);
        void SetPasswordExpiryTime(IComputer computer, DateTime time);
    }
}

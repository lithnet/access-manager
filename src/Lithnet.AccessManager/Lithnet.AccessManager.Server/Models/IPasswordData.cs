using System;

namespace Lithnet.AccessManager.Server
{
    public interface IPasswordData
    {
        long Id { get; set; }
        string PasswordData { get; set; }
        DateTime EffectiveDate { get; set; }
        DateTime RetiredDate { get; set; }
        DateTime ExpiryDate { get; set; }
        string RequestId { get; set; }
        string AccountName { get; set; }
    }
}
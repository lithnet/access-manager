namespace Lithnet.AccessManager.Server
{
    public interface IRegistrationKey
    {
        long Id { get; set; }

        string Key { get; set; }

        int ActivationCount { get; set; }

        int ActivationLimit { get; set; }

        bool Enabled { get; set; }

        string Name { get; set; }
    }
}
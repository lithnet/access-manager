namespace Lithnet.AccessManager.Server.UI
{
    public interface IExternalDialogAware
    {
        bool CancelButtonVisible { get; }

        bool SaveButtonVisible { get; }

        bool CancelButtonIsDefault { get; }

        bool SaveButtonIsDefault { get; }

        string SaveButtonName { get; }

        string CancelButtonName { get; }
    }
}
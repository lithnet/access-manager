namespace Lithnet.AccessManager.Server.UI
{
    public interface IRegistrationKeyViewModelFactory
    {
        RegistrationKeyViewModel CreateViewModel(IRegistrationKey model);
    }
}
namespace Lithnet.AccessManager
{
    public interface IAppDataProvider
    {
        void DeleteAppData(IAppData settings);

        IAppData GetAppData(IComputer computer);

        IAppData GetOrCreateAppData(IComputer computer);

        bool TryGetAppData(IComputer computer, out IAppData appData);

        IAppData Create(IComputer computer);
    }
}
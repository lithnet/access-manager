using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IShellExecuteProvider
    {
        Task OpenWithShellExecute(string path);
    }
}
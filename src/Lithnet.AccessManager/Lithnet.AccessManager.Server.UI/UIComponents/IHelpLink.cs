using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IHelpLink
    {
        string HelpLink { get; }

        Task Help();
    }
}
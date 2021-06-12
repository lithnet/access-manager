using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public interface ILapsAgent
    {
         Task DoCheck();
    }
}
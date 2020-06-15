using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.Authorization;

namespace Lithnet.Laps.Web.ActionProviders
{
    public interface IJitAccessGroupResolver
    {
        IGroup GetJitAccessGroup(IComputer computer, IJsonTarget target);
    }
}
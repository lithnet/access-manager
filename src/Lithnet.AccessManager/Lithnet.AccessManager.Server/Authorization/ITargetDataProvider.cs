using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface ITargetDataProvider
    {
        TargetData GetTargetData(SecurityDescriptorTarget target);

        int GetSortOrder(SecurityDescriptorTarget target);
    }
}
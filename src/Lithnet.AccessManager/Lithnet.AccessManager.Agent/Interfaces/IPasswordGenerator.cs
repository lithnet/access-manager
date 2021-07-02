using Lithnet.AccessManager.Agent;

namespace Lithnet.AccessManager
{
    public interface IPasswordGenerator
    {
        string Generate(IPasswordPolicy policy);
    }
}
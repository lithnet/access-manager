using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent
{
    public interface IMetadataProvider
    {
        Task<MetadataResponse> GetMetadata();
    }
}
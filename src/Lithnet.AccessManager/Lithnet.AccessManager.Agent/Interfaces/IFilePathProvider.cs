using System;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IFilePathProvider
    {
        string ConfFilePath { get; }

        string StateFilePath { get; }
    }
}
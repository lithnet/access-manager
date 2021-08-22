﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IRegistrationKeyProvider
    {
        Task<IRegistrationKey> ValidateRegistrationKey(string key);

        IAsyncEnumerable<IRegistrationKey> GetRegistrationKeys();

        Task DeleteRegistrationKey(IRegistrationKey key);

        Task<IRegistrationKey> CreateRegistrationKey();

        Task<IRegistrationKey> UpdateRegistrationKey(IRegistrationKey key);

        Task<IRegistrationKey> CloneRegistrationKey(IRegistrationKey key);
        IAsyncEnumerable<IAmsGroup> GetRegistrationKeyGroups(IRegistrationKey key);
        Task AddGroupToKey(IRegistrationKey key, IAmsGroup group);
        Task RemoveGroupFromKey(IRegistrationKey key, IAmsGroup group);
    }
}
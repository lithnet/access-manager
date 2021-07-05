using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class DbRegistrationKeyProvider : IRegistrationKeyProvider
    {
        private readonly IDbProvider dbProvider;
        private readonly ILogger<DbRegistrationKeyProvider> logger;
        private readonly IRandomValueGenerator rvg;

        public DbRegistrationKeyProvider(IDbProvider dbProvider, ILogger<DbRegistrationKeyProvider> logger, IRandomValueGenerator rvg)
        {
            this.dbProvider = dbProvider;
            this.logger = logger;
            this.rvg = rvg;
        }

        public async Task<IRegistrationKey> ValidateRegistrationKey(string key)
        {
            try
            {
                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("spConsumeRegistrationKey", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@RegistrationKey", key);

                await using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    return new DbRegistrationKey(reader);
                }

                throw new InvalidOperationException("The database did not return the record as expected");
            }
            catch (SqlException ex)
            {
                if (ex.Number == DbConstants.ErrorRegistrationKeyDisabled)
                {
                    throw new RegistrationKeyValidationException($"The registration key provided has been disabled", ex);
                }

                if (ex.Number == DbConstants.ErrorRegistrationKeyActivationLimitExceeded)
                {
                    throw new RegistrationKeyValidationException("The registration key has exceeded the maximum allowed activations", ex);
                }

                if (ex.Number == DbConstants.ErrorRegistrationKeyNotFound)
                {
                    throw new RegistrationKeyValidationException("The registration key provided was not found", ex);
                }

                throw;
            }
        }

        public async Task DeleteRegistrationKey(IRegistrationKey key)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spDeleteRegistrationKey", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ID", key.Id);

            await command.ExecuteNonQueryAsync();
        }

        public Task<IRegistrationKey> CloneRegistrationKey(IRegistrationKey key)
        {
            return Task.FromResult((IRegistrationKey)new DbRegistrationKey()
            {
                ActivationCount = key.ActivationCount,
                ActivationLimit = key.ActivationLimit,
                Enabled = key.Enabled,
                Id = key.Id,
                Key = key.Key,
                Name = key.Name,
                ApprovalRequired = key.ApprovalRequired
            });
        }

        public Task<IRegistrationKey> CreateRegistrationKey()
        {
            return Task.FromResult((IRegistrationKey)new DbRegistrationKey
            {
                Key = this.rvg.GenerateRandomString(32, true, false, false, false),
                ActivationLimit = 0,
                ActivationCount = 0,
                Enabled = true,
                ApprovalRequired = false
            });
        }

        public async Task<IRegistrationKey> UpdateRegistrationKey(IRegistrationKey key)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();
            string sp = key.Id == 0 ? "spCreateRegistrationKey" : "spUpdateRegistrationKey";
            SqlCommand command = new SqlCommand(sp, con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            key.ToUpdateCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new DbRegistrationKey(reader);
            }

            throw new InvalidOperationException("The database did not return the new record as expected");
        }

        public async IAsyncEnumerable<IRegistrationKey> GetRegistrationKeys()
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetRegistrationKeys", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                yield return new DbRegistrationKey(reader);
            }
        }
    }
}
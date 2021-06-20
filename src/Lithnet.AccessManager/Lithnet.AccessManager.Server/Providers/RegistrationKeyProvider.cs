using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Providers
{
    public class RegistrationKeyProvider : IRegistrationKeyProvider
    {
        private readonly IDbProvider dbProvider;
        private readonly ILogger<RegistrationKeyProvider> logger;

        public RegistrationKeyProvider(IDbProvider dbProvider, ILogger<RegistrationKeyProvider> logger)
        {
            this.dbProvider = dbProvider;
            this.logger = logger;
        }

        public async Task<bool> ValidateRegistrationKey(string key)
        {
            try
            {
                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("spConsumeRegistrationKey", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@RegistrationKey", key);

                return (int)(await command.ExecuteScalarAsync()) == 1;
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
    }
}

/*
 * SELECT TOP (1000) [ID]
      ,[RegistrationKey]
      ,[ActivationLimit]
      ,[ActivationCount]
      ,[Enabled]
      ,[RegistrationKeyName]
  FROM [AccessManager].[dbo].[RegistrationKeys]
 */
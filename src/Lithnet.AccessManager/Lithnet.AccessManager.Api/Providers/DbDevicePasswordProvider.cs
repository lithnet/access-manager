using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Api.Providers
{
    public class DbDevicePasswordProvider : IDbDevicePasswordProvider
    {
        private readonly IDbProvider dbProvider;
        private readonly ILogger<DbDeviceProvider> logger;
        private IOptions<PasswordPolicyOptions> passwordPolicy;

        public DbDevicePasswordProvider(IDbProvider dbProvider, ILogger<DbDeviceProvider> logger, IOptions<PasswordPolicyOptions> passwordPolicy)
        {
            this.dbProvider = dbProvider;
            this.logger = logger;
            this.passwordPolicy = passwordPolicy;
        }

        public async Task<bool> HasPasswordExpired(string deviceId)
        {
            try
            {
                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("spGetCurrentPassword", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ObjectID", deviceId);

                await using SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return true;
                }

                await reader.ReadAsync();
                DbPasswordData data = new DbPasswordData(reader);
                return data.EffectiveDate.AddDays(this.passwordPolicy.Value.MaximumPasswordAgeDays) < DateTime.UtcNow;

            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                {
                    throw new ObjectNotFoundException($"The device {deviceId} was not found");
                }

                throw;
            }
        }

        public async Task<string> UpdateDevicePassword(string deviceId, PasswordUpdateRequest request)
        {
            try
            {
                await using SqlConnection con = this.dbProvider.GetConnection();

                string requestId = Guid.NewGuid().ToString();

                SqlCommand command = new SqlCommand("spUpdateCurrentPassword", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ObjectID", deviceId);
                command.Parameters.AddWithValue("@PasswordData", request.PasswordData);
                command.Parameters.AddWithValue("@RequestId", requestId);
                command.Parameters.AddWithValue("@AccountName", request.AccountName);
                command.Parameters.AddWithValue("@EffectiveDate", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();

                await this.PurgeOldPasswords(deviceId);

                return requestId;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                {
                    throw new ObjectNotFoundException($"The device {deviceId} was not found");
                }

                throw;
            }
        }

        public async Task RevertLastPasswordChange(string deviceId, string requestId)
        {
            try
            {
                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("spGetCurrentPassword", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ObjectID", deviceId);

                await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {
                        throw new PasswordRollbackDeniedException($"The password change cannot be reverted for device {deviceId} as no current password was found");
                    }

                    await reader.ReadAsync();
                    DbPasswordData data = new DbPasswordData(reader);

                    if (!string.Equals(data.RequestId, requestId))
                    {
                        throw new PasswordRollbackDeniedException($"The password change cannot be reverted for device {deviceId} because the supplied request ID did not match the most recent password for the device");
                    }

                    if (data.EffectiveDate.AddMinutes(this.passwordPolicy.Value.RollbackWindowMinutes) < DateTime.UtcNow)
                    {
                        throw new PasswordRollbackDeniedException($"The password change cannot be reverted for device {deviceId} because the rollback window has expired");
                    }
                }

                command = new SqlCommand("spRollbackPasswordUpdate", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ObjectID", deviceId);
                command.Parameters.AddWithValue("@RequestID", requestId);
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                {
                    throw new ObjectNotFoundException($"The device {deviceId} was not found");
                }

                throw;
            }
        }

        private async Task PurgeOldPasswords(string deviceId)
        {
            if (this.passwordPolicy.Value.MaxNumberOfPasswords <= 0 ||
                this.passwordPolicy.Value.MaximumPasswordHistoryAgeDays <= 0)
            {
                return;
            }

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spPurgePasswordHistory", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectID", deviceId);

            if (this.passwordPolicy.Value.MaxNumberOfPasswords >= 1)
            {
                command.Parameters.AddWithValue("@MaxEntries", this.passwordPolicy.Value.MaxNumberOfPasswords);
            }
            else
            {
                command.Parameters.AddWithValue("@OldestDate", DateTime.UtcNow.AddDays(-this.passwordPolicy.Value.MaximumPasswordHistoryAgeDays));
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}
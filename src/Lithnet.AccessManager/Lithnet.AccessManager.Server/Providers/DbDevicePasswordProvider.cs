using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public class DbDevicePasswordProvider : IDevicePasswordProvider
    {
        private readonly IDbProvider dbProvider;
        private readonly ILogger<DbDeviceProvider> logger;
        private readonly IOptionsMonitor<PasswordPolicyOptions> policyOptions;

        public DbDevicePasswordProvider(IDbProvider dbProvider, ILogger<DbDeviceProvider> logger, IOptionsMonitor<PasswordPolicyOptions> policyOptions)
        {
            this.dbProvider = dbProvider;
            this.logger = logger;
            this.policyOptions = policyOptions;
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

                return DateTime.UtcNow > data.ExpiryDate;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                {
                    throw new DeviceNotFoundException($"The device {deviceId} was not found");
                }

                throw;
            }
        }

        public async Task<IPasswordData> GetCurrentPassword(string deviceId)
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
                    throw new NoPasswordException();
                }

                await reader.ReadAsync();
                return new DbPasswordData(reader);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                {
                    throw new DeviceNotFoundException($"The device {deviceId} was not found");
                }

                throw;
            }
        }

        public async Task<IList<IPasswordData>> GetPasswordHistory(string deviceId)
        {
            try
            {
                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("spGetPasswordHistory", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ObjectID", deviceId);

                await using SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    throw new NoPasswordException();
                }

                List<IPasswordData> passwords = new List<IPasswordData>();

                while (await reader.ReadAsync())
                {
                    passwords.Add(new DbPasswordData(reader));
                }

                return passwords;
            }
            catch (SqlException ex)
            {
                if (ex.Number == DbConstants.ErrorDeviceNotFound)
                {
                    throw new DeviceNotFoundException($"The device {deviceId} was not found");
                }

                throw;
            }
        }

        public async Task<IPasswordData> GetCurrentPassword(string deviceId, DateTime newExpiry)
        {
            try
            {
                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("spGetCurrentPasswordAndUpdateExpiry", con);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ObjectID", deviceId);
                command.Parameters.AddWithValue("@ExpiryDate", newExpiry);

                await using SqlDataReader reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    throw new NoPasswordException();
                }

                await reader.ReadAsync();
                return new DbPasswordData(reader);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000)
                {
                    throw new DeviceNotFoundException($"The device {deviceId} was not found");
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
                command.Parameters.AddWithValue("@ExpiryDate", request.ExpiryDate);

                await command.ExecuteNonQueryAsync();

                return requestId;
            }
            catch (SqlException ex)
            {
                if (ex.Number == DbConstants.ErrorDeviceNotFound)
                {
                    throw new DeviceNotFoundException($"The device {deviceId} was not found");
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

                    if (data.EffectiveDate.AddMinutes(this.policyOptions.CurrentValue.RollbackWindowMinutes) < DateTime.UtcNow)
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
                if (ex.Number == DbConstants.ErrorDeviceNotFound)
                {
                    throw new DeviceNotFoundException($"The device {deviceId} was not found");
                }

                throw;
            }
        }

        public async Task PurgeOldPasswords(string deviceId, int minimumNumberOfPasswords, int minimumPasswordHistoryAgeDays)
        {
            if (minimumNumberOfPasswords <= 0 &&
                minimumPasswordHistoryAgeDays <= 0)
            {
                return;
            }

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spPurgePasswordHistory", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectID", deviceId);
            command.Parameters.AddWithValue("@MinEntries", minimumNumberOfPasswords);
            command.Parameters.AddWithValue("@PurgeBefore", DateTime.UtcNow.AddDays(-minimumPasswordHistoryAgeDays));

            await command.ExecuteNonQueryAsync();
        }
    }
}
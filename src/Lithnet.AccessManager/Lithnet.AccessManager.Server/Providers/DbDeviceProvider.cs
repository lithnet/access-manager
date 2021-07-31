using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Server
{
    public class DbDeviceProvider : IDeviceProvider
    {
        private readonly IDbProvider dbProvider;
        private readonly ILogger<DbDeviceProvider> logger;

        public DbDeviceProvider(IDbProvider dbProvider, ILogger<DbDeviceProvider> logger)
        {
            this.dbProvider = dbProvider;
            this.logger = logger;
        }

        public async Task<IList<IDevice>> FindDevices(string name)
        {
            name.ThrowIfNull(nameof(name));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDevicesByNames", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ComputerNameOrDnsName", name);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<IDevice> devices = new List<IDevice>();

            while (await reader.ReadAsync())
            {
                devices.Add(new DbDevice(reader));
            }

            return devices;
        }
        public async IAsyncEnumerable<IDevice> GetDevices()
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDevices", con);
            command.CommandType = CommandType.StoredProcedure;

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                yield return new DbDevice(reader);
            }
        }

        public async IAsyncEnumerable<IDevice> GetDevices(int startIndex, int count)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDevicesByPage", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@startIndex", startIndex);
            command.Parameters.AddWithValue("@rows", count);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                yield return new DbDevice(reader);
            }
        }

        public async Task DisableDevice(string deviceId)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spDisableDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectId", deviceId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task EnableDevice(string deviceId)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spEnableDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectId", deviceId);

            await command.ExecuteNonQueryAsync();
        }

    
        public async Task ApproveDevice(string deviceId)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spApproveDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectId", deviceId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteDevice(string deviceId)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spDeleteDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectId", deviceId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task RejectDevice(string deviceId)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spRejectDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectId", deviceId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<IDevice> GetOrCreateDeviceAsync(Microsoft.Graph.Device aadDevice, string authorityId)
        {
            authorityId.ThrowIfNull(nameof(authorityId));
            aadDevice.ThrowIfNull(nameof(aadDevice));

            string deviceId = aadDevice.Id;

            try
            {
                return await this.GetDeviceAsync(AuthorityType.AzureActiveDirectory, authorityId, deviceId);
            }
            catch (DeviceNotFoundException)
            {
                this.logger.LogTrace($"The AAD-joined computer {aadDevice.DeviceId} was not found in the AMS database and will be created");
            }

            return await this.CreateDeviceAsync(aadDevice, authorityId);
        }

        public async Task<IDevice> GetOrCreateDeviceAsync(IActiveDirectoryComputer principal, string authorityId)
        {
            authorityId.ThrowIfNull(nameof(authorityId));
            principal.ThrowIfNull(nameof(principal));

            string deviceId = principal.Sid.ToString();

            try
            {
                return await this.GetDeviceAsync(AuthorityType.ActiveDirectory, authorityId, deviceId);
            }
            catch (DeviceNotFoundException)
            {
                this.logger.LogTrace($"The AD-joined computer {principal.MsDsPrincipalName} was not found in the AMS database and will be created");
            }

            return await this.CreateDeviceAsync(principal, authorityId, deviceId);
        }

        public async Task<IDevice> GetDeviceAsync(AuthorityType authorityType, string authorityId, string authorityDeviceId)
        {
            authorityId.ThrowIfNull(nameof(authorityId));
            authorityDeviceId.ThrowIfNull(nameof(authorityDeviceId));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDeviceByAuthority", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@AuthorityType", (int)authorityType);
            command.Parameters.AddWithValue("@AuthorityId", authorityId);
            command.Parameters.AddWithValue("@AuthorityDeviceId", authorityDeviceId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new DbDevice(reader);
            }

            throw new DeviceNotFoundException($"Could not find a device with ID {authorityDeviceId} from authority {authorityId} ({authorityType})");
        }

        public async Task<IDevice> GetDeviceAsync(string deviceId)
        {
            deviceId.ThrowIfNull(nameof(deviceId));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectID", deviceId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new DbDevice(reader);
            }

            throw new DeviceNotFoundException($"Could not find a device with ID {deviceId}");
        }

        private async Task<long> GetOrCreateAuthorityKey(string authorityId, AuthorityType type)
        {
            authorityId.ThrowIfNull(nameof(authorityId));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetOrCreateAuthority", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@AuthorityId", authorityId);
            command.Parameters.AddWithValue("@AuthorityType", (int)type);

            return (long)await command.ExecuteScalarAsync();
        }

        public async Task<IDevice> GetDeviceAsync(X509Certificate2 certificate)
        {
            certificate.ThrowIfNull(nameof(certificate));

            await using SqlConnection con = this.dbProvider.GetConnection();

            string thumbprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);

            SqlCommand command = new SqlCommand("spGetDeviceByX509Thumbprint", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Thumbprint", thumbprint);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new DbDevice(reader);
            }

            throw new DeviceCredentialsNotFoundException($"Could not find a device with credentials for the certificate issued to '{certificate.Subject}' with thumbprint {certificate.Thumbprint}");
        }

        public async Task<IDevice> CreateDeviceAsync(IDevice device, X509Certificate2 certificate)
        {
            try
            {
                device.ThrowIfNull(nameof(device));
                certificate.ThrowIfNull(nameof(certificate));

                device.ObjectID ??= Guid.NewGuid().ToString();
                device.AuthorityDeviceId = device.ObjectID;
                device.SecurityIdentifier = new System.Security.Principal.SecurityIdentifier($"{SidUtils.AmsSidPrefix}{SidUtils.GuidStringToSidString(device.ObjectID)}");

                long authorityKey = await this.GetOrCreateAuthorityKey(Constants.AmsAuthorityId, AuthorityType.Ams);

                await using SqlConnection con = this.dbProvider.GetConnection();

                SqlCommand command = new SqlCommand("spCreateDeviceWithCredentials", con);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@X509Certificate", certificate.Export(X509ContentType.Cert));
                command.Parameters.AddWithValue("@X509CertificateThumbprint", certificate.GetCertHashString(HashAlgorithmName.SHA256));
                command.Parameters.AddWithValue("@AuthorityKey", authorityKey);
                device.ToCreateCommandParameters(command);

                await using SqlDataReader reader = await command.ExecuteReaderAsync();
                await reader.ReadAsync();
                return new DbDevice(reader);
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

        public async Task AddDeviceCredentialsAsync(IDevice device, X509Certificate2 certificate)
        {
            device.ThrowIfNull(nameof(device));
            certificate.ThrowIfNull(nameof(certificate));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spAddDeviceCredentials", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@X509Certificate", certificate.Export(X509ContentType.Cert));
            command.Parameters.AddWithValue("@X509CertificateThumbprint", certificate.GetCertHashString(HashAlgorithmName.SHA256));
            command.Parameters.AddWithValue("@ID", device.Id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<IDevice> CreateDeviceAsync(Microsoft.Graph.Device aadDevice, string authorityId)
        {
            aadDevice.ThrowIfNull(nameof(aadDevice));
            authorityId.ThrowIfNull(nameof(authorityId));

            DbDevice device = new DbDevice
            {
                AuthorityId = authorityId,
                AuthorityDeviceId = aadDevice.Id,
                AuthorityType = AuthorityType.AzureActiveDirectory,
                ApprovalState = ApprovalState.Approved,
                ComputerName = aadDevice.DisplayName,
                OperatingSystemFamily = aadDevice.OperatingSystem,
                OperatingSystemVersion = aadDevice.OperatingSystemVersion,
                SecurityIdentifier = new System.Security.Principal.SecurityIdentifier($"{SidUtils.AadSidPrefix}{SidUtils.GuidStringToSidString(aadDevice.Id)}")
            };

            return await this.CreateDeviceAsync(device);
        }

        public async Task<IDevice> CreateDeviceAsync(IActiveDirectoryComputer computer, string authorityId, string deviceId)
        {
            computer.ThrowIfNull(nameof(computer));
            authorityId.ThrowIfNull(nameof(authorityId));
            deviceId.ThrowIfNull(nameof(deviceId));

            DbDevice device = new DbDevice
            {
                ApprovalState = ApprovalState.Approved,
                AuthorityId = authorityId,
                AuthorityDeviceId = deviceId,
                AuthorityType = AuthorityType.ActiveDirectory,
                ComputerName = computer.SamAccountName.TrimEnd('$'),
                DnsName = computer.DnsHostName,
                SecurityIdentifier = computer.Sid
            };

            return await this.CreateDeviceAsync(device);
        }

        public async Task<IDevice> CreateDeviceAsync(IDevice device)
        {
            device.ThrowIfNull(nameof(device));

            long authorityKey = await this.GetOrCreateAuthorityKey(device.AuthorityId, device.AuthorityType);

            device.ObjectID ??= Guid.NewGuid().ToString();

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spCreateDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@AuthorityKey", authorityKey);

            device.ToCreateCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new DbDevice(reader);
        }

        public async Task<IDevice> UpdateDeviceAsync(IDevice device)
        {
            device.ThrowIfNull(nameof(device));

            if (device.ObjectID == null)
            {
                throw new InvalidOperationException("Could not update the device because the device ID was not found");
            }

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spUpdateDevice", con);
            command.CommandType = CommandType.StoredProcedure;
            device.ToUpdateCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new DbDevice(reader);
        }
    }
}

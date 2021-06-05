using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Providers
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

        public async Task<Device> GetOrCreateDeviceAsync(Microsoft.Graph.Device aadDevice, string authority)
        {
            string deviceId = aadDevice.Id;

            try
            {
                return await this.GetDeviceAsync(AuthorityType.AzureActiveDirectory, authority, deviceId);
            }
            catch (DeviceNotFoundException)
            {
                this.logger.LogTrace($"The AAD-joined computer {aadDevice.DeviceId} was not found in the AMS database and will be created");
            }

            return await this.CreateDeviceAsync(aadDevice, authority);
        }

        public async Task<Device> GetOrCreateDeviceAsync(IComputer principal, string authority)
        {
            string deviceId = principal.Sid.ToString();

            try
            {
                return await this.GetDeviceAsync(AuthorityType.ActiveDirectory, authority, deviceId);
            }
            catch (DeviceNotFoundException)
            {
                this.logger.LogTrace($"The AD-joined computer {principal.MsDsPrincipalName} was not found in the AMS database and will be created");
            }

            return await this.CreateDeviceAsync(principal, authority, deviceId);
        }

        public async Task<Device> GetDeviceAsync(AuthorityType authorityType, string authority, string deviceId)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDeviceByAuthority", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@AuthorityType", (int)authorityType);
            command.Parameters.AddWithValue("@Authority", authority);
            command.Parameters.AddWithValue("@AuthorityDeviceId", deviceId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Device(reader);
            }

            throw new DeviceNotFoundException($"Could not find a device with ID {deviceId} from authority {authority} ({authorityType})");
        }

        public async Task<Device> GetDeviceAsync(X509Certificate2 certificate)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDeviceByX509Thumbprint", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Thumbprint", certificate.Thumbprint);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Device(reader);
            }

            throw new DeviceNotFoundException($"Could not find a device with credentials for the certificate issued to '{certificate.Subject}' with thumbprint {certificate.Thumbprint}");
        }

        public async Task<Device> CreateDeviceAsync(Device device, X509Certificate2 certificate)
        {
            device.ObjectId ??= Guid.NewGuid().ToString();
            device.Authority = "ams";
            device.AuthorityDeviceId = device.ObjectId;
            device.AuthorityType = AuthorityType.SelfAsserted;

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spCreateDeviceWithCredentials", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@X509Certificate", certificate.Export(X509ContentType.Cert));
            command.Parameters.AddWithValue("@X509CertificateThumbprint", certificate.Thumbprint);
            device.ToCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new Device(reader);
        }

        public async Task<Device> CreateDeviceAsync(Microsoft.Graph.Device aadDevice, string authority)
        {
            Device device = new Device
            {
                Authority = authority,
                AuthorityDeviceId = aadDevice.Id,
                AuthorityType = AuthorityType.AzureActiveDirectory,
                ApprovalState = ApprovalState.Approved,
                ComputerName = aadDevice.DisplayName,
                OperatingSystemFamily = aadDevice.OperatingSystem,
                OperatingSystemVersion = aadDevice.OperatingSystemVersion,
            };

            return await this.CreateDeviceAsync(device);
        }

        public async Task<Device> CreateDeviceAsync(IComputer computer, string authority, string deviceId)
        {
            Device device = new Device
            {
                ApprovalState = ApprovalState.Approved,
                Authority = authority,
                AuthorityDeviceId = deviceId,
                AuthorityType = AuthorityType.ActiveDirectory,
                ComputerName = computer.SamAccountName.TrimEnd('$'),
                DnsName = computer.DnsHostName
            };

            return await this.CreateDeviceAsync(device);
        }

        public async Task<Device> CreateDeviceAsync(Device device)
        {
            device.ObjectId ??= Guid.NewGuid().ToString();

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spCreateDevice", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            device.ToCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new Device(reader);
        }
    }
}

using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

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

        public async Task<IList<Device>> FindDevices(string name)
        {
            name.ThrowIfNull(nameof(name));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDevicesByNames", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ComputerNameOrDnsName", name);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            List<Device> devices = new List<Device>();

            while (await reader.ReadAsync())
            {
                devices.Add(new Device(reader));
            }

            return devices;
        }

        public async Task<Device> GetOrCreateDeviceAsync(Microsoft.Graph.Device aadDevice, string authority)
        {
            authority.ThrowIfNull(nameof(authority));
            aadDevice.ThrowIfNull(nameof(aadDevice));

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

        public async Task<Device> GetOrCreateDeviceAsync(IActiveDirectoryComputer principal, string authority)
        {
            authority.ThrowIfNull(nameof(authority));
            principal.ThrowIfNull(nameof(principal));

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

        public async Task<Device> GetDeviceAsync(AuthorityType authorityType, string authority, string authorityDeviceId)
        {
            authority.ThrowIfNull(nameof(authority));
            authorityDeviceId.ThrowIfNull(nameof(authorityDeviceId));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDeviceByAuthority", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@AuthorityType", (int)authorityType);
            command.Parameters.AddWithValue("@Authority", authority);
            command.Parameters.AddWithValue("@AuthorityDeviceId", authorityDeviceId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Device(reader);
            }

            throw new DeviceNotFoundException($"Could not find a device with ID {authorityDeviceId} from authority {authority} ({authorityType})");

        }

        public async Task<Device> GetDeviceAsync(string deviceId)
        {
            deviceId.ThrowIfNull(nameof(deviceId));

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDevice", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectID", deviceId);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new Device(reader);
            }

            throw new DeviceNotFoundException($"Could not find a device with ID {deviceId}");
        }

        public async Task<Device> GetDeviceAsync(X509Certificate2 certificate)
        {
            certificate.ThrowIfNull(nameof(certificate));

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
            device.ThrowIfNull(nameof(device));
            certificate.ThrowIfNull(nameof(certificate));

            device.ObjectID ??= Guid.NewGuid().ToString();
            device.Authority = "ams";
            device.AuthorityDeviceId = device.ObjectID;
            device.AuthorityType = AuthorityType.Ams;
            device.SecurityIdentifier = new System.Security.Principal.SecurityIdentifier($"{SidUtils.AmsSidPrefix}{SidUtils.GuidStringToSidString(device.ObjectID)}");

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spCreateDeviceWithCredentials", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@X509Certificate", certificate.Export(X509ContentType.Cert));
            command.Parameters.AddWithValue("@X509CertificateThumbprint", certificate.Thumbprint);
            device.ToCreateCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new Device(reader);
        }

        public async Task<Device> CreateDeviceAsync(Microsoft.Graph.Device aadDevice, string authority)
        {
            aadDevice.ThrowIfNull(nameof(aadDevice));
            authority.ThrowIfNull(nameof(authority));

            Device device = new Device
            {
                Authority = authority,
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

        public async Task<Device> CreateDeviceAsync(IActiveDirectoryComputer computer, string authority, string deviceId)
        {
            computer.ThrowIfNull(nameof(computer));
            authority.ThrowIfNull(nameof(authority));
            deviceId.ThrowIfNull(nameof(deviceId));

            Device device = new Device
            {
                ApprovalState = ApprovalState.Approved,
                Authority = authority,
                AuthorityDeviceId = deviceId,
                AuthorityType = AuthorityType.ActiveDirectory,
                ComputerName = computer.SamAccountName.TrimEnd('$'),
                DnsName = computer.DnsHostName,
                SecurityIdentifier = computer.Sid
            };

            return await this.CreateDeviceAsync(device);
        }

        public async Task<Device> CreateDeviceAsync(Device device)
        {
            device.ThrowIfNull(nameof(device));

            device.ObjectID ??= Guid.NewGuid().ToString();

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spCreateDevice", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            device.ToCreateCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new Device(reader);
        }

        public async Task<Device> UpdateDeviceAsync(Device device)
        {
            device.ThrowIfNull(nameof(device));

            if (device.ObjectID == null)
            {
                throw new InvalidOperationException("Could not update the device because the device ID was not found");
            }

            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spUpdateDevice", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            device.ToUpdateCommandParameters(command);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new Device(reader);
        }
    }
}

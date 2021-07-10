using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class DbAmsGroupProvider : IAmsGroupProvider
    {
        private readonly IDbProvider dbProvider;
        private readonly ILogger<DbAmsGroupProvider> logger;

        public DbAmsGroupProvider(IDbProvider dbProvider, ILogger<DbAmsGroupProvider> logger)
        {
            this.dbProvider = dbProvider;
            this.logger = logger;
        }

        public async IAsyncEnumerable<SecurityIdentifier> GetGroupSidsForDevice(IDevice device)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetDeviceGroupMembership", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ObjectID", device.ObjectID);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                yield return new DbAmsGroup(reader).SecurityIdentifier;
            }
        }

        public async Task DeleteGroup(IAmsGroup group)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spDeleteGroup", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@ID", group.Id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task RemoveFromGroup(IAmsGroup group, IDevice device)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spRemoveDeviceFromGroup", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@GroupID", group.Id);
            command.Parameters.AddWithValue("@DeviceID", device.Id);

            await command.ExecuteNonQueryAsync();
        }

        public async IAsyncEnumerable<IDevice> GetMemberDevices(IAmsGroup group)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetGroupDeviceMembers", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@GroupID", group.Id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                yield return new DbDevice(reader);
            }
        }

        public async Task AddToGroup(IAmsGroup group, IDevice device)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spAddDeviceToGroup", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@GroupID", group.Id);
            command.Parameters.AddWithValue("@DeviceID", device.Id);

            await command.ExecuteNonQueryAsync();
        }
        
        public Task<IAmsGroup> CloneGroup(IAmsGroup group)
        {
            return Task.FromResult((IAmsGroup)new DbAmsGroup()
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                Sid = group.Sid
            });
        }

        public Task<IAmsGroup> CreateGroup()
        {
            return Task.FromResult((IAmsGroup)new DbAmsGroup
            {
                Sid = Guid.NewGuid().ToAmsSidString()
            });
        }

        public async Task<IAmsGroup> UpdateGroup(IAmsGroup group)
        {
            await using SqlConnection con = this.dbProvider.GetConnection();
            string sp = group.Id == 0 ? "spCreateGroup" : "spUpdateGroup";
            SqlCommand command = new SqlCommand(sp, con);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            if (group.Id == 0)
            {
                group.ToCreateCommandParameters(command);
            }
            else
            {
                group.ToUpdateCommandParameters(command);
            }

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                return new DbAmsGroup(reader);
            }

            throw new InvalidOperationException("The database did not return the new record as expected");
        }

        public async IAsyncEnumerable<IAmsGroup> GetGroups()
        {
            await using SqlConnection con = this.dbProvider.GetConnection();

            SqlCommand command = new SqlCommand("spGetGroups", con);
            command.CommandType = System.Data.CommandType.StoredProcedure;

            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                yield return new DbAmsGroup(reader);
            }
        }
    }
}
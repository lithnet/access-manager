using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Text;
using Lithnet.AccessManager.Server.Providers;

namespace Lithnet.AccessManager.Server
{
    public static class AmsGroupExtensions
    {
        public static void ToUpdateCommandParameters(this IAmsGroup group, SqlCommand command)
        {
            if (group.Id != 0)
            {
                command.Parameters.AddWithValue("@ID", group.Id);
            }

            command.Parameters.AddWithValue("@Description", group.Description);
            command.Parameters.AddWithValue("@Name", group.Name);
        }

        public static void ToCreateCommandParameters(this IAmsGroup group, SqlCommand command)
        {
            command.Parameters.AddWithValue("@Sid", group.Sid);
            command.Parameters.AddWithValue("@Description", group.Description);
            command.Parameters.AddWithValue("@Name", group.Name);
        }
    }
}

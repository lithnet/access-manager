using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Text;

namespace Lithnet.AccessManager.Server
{
    public static class RegistrationKeyExtensions
    {
        public static void ToUpdateCommandParameters(this IRegistrationKey key, SqlCommand command)
        {
            if (key.Id != 0)
            {
                command.Parameters.AddWithValue("@ID", key.Id);
            }

            command.Parameters.AddWithValue("@RegistrationKey", key.Key);
            command.Parameters.AddWithValue("@ActivationLimit", key.ActivationLimit);
            command.Parameters.AddWithValue("@ActivationCount", key.ActivationCount);
            command.Parameters.AddWithValue("@Enabled", key.Enabled);
            command.Parameters.AddWithValue("@RegistrationKeyName", key.Name);
        }

        public static void ToCreateCommandParameters(this IRegistrationKey key, SqlCommand command)
        {
            command.Parameters.AddWithValue("@RegistrationKey", key.Key);
            command.Parameters.AddWithValue("@ActivationLimit", key.ActivationLimit);
            command.Parameters.AddWithValue("@ActivationCount", key.ActivationCount);
            command.Parameters.AddWithValue("@Enabled", key.Enabled);
            command.Parameters.AddWithValue("@RegistrationKeyName", key.Name);
        }
    }
}

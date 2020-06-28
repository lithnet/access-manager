using System.Collections.Generic;
using Lithnet.AccessManager.Configuration;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    public class TestSerialization
    {
        [Test]
        public void SerializeAuditNotificationChannels()
        {
            AuditNotificationChannels channels = new AuditNotificationChannels();
            channels.OnFailure = new List<string>() { "1" };
            channels.OnSuccess = new List<string>() { "2" };

            AuditNotificationChannels newChannels = JsonConvert.DeserializeObject<AuditNotificationChannels>(JsonConvert.SerializeObject(channels));

            CollectionAssert.AreEqual(channels.OnFailure, newChannels.OnFailure);
        }
    }
}

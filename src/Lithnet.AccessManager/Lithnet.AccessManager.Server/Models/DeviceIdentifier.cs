namespace Lithnet.AccessManager.Server
{
    public class DeviceIdentifier
    {
        public string AuthorityDeviceId { get; set; }

        public string AuthorityId { get; set; }

        public AuthorityType AuthorityType { get; set; }

        public string Sid { get; set; }
    }
}

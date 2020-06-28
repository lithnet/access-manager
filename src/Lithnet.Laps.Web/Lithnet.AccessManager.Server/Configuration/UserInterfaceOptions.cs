namespace Lithnet.AccessManager.Configuration
{
    public class UserInterfaceOptions
    {
        public string Title { get; set; } = "Lithnet Access Manager";

        public AuditReasonFieldState UserSuppliedReason { get; set; } = AuditReasonFieldState.Optional;

        public bool AllowLaps { get; set; } = true;

        public bool AllowJit { get; set; } = true;

        public bool AllowLapsHistory { get; set; } = true;
    }
}
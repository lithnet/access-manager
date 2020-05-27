namespace Lithnet.Laps.Web.Config
{
    public interface IReaderElement
    {
        AuditElement Audit { get; }

        string Principal { get; }
    }
}

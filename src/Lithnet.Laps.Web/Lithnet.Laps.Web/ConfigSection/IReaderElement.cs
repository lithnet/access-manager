namespace Lithnet.Laps.Web
{
    public interface IReaderElement
    {
        AuditElement Audit { get; }
        string Principal { get; }
    }
}

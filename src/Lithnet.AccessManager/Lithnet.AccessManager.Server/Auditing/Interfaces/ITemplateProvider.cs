namespace Lithnet.AccessManager.Server.Auditing
{
    public interface ITemplateProvider
    {
        string GetTemplate(string templateNameOrPath);
    }
}

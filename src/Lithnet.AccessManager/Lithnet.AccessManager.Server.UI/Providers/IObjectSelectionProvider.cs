using System.Security.Principal;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IObjectSelectionProvider
    {
        bool GetUser(IViewAware owner, out SecurityIdentifier sid);

        bool GetGroup(IViewAware owner, out SecurityIdentifier sid);

        bool GetComputer(IViewAware owner, out SecurityIdentifier sid);

        bool GetUserOrGroup(IViewAware owner, out SecurityIdentifier sid);

        bool GetUser(IViewAware owner, string targetServer, out SecurityIdentifier sid);

        bool GetGroup(IViewAware owner, string targetServer, out SecurityIdentifier sid);

        bool GetComputer(IViewAware owner, string targetServer, out SecurityIdentifier sid);

        bool GetUserOrGroup(IViewAware owner, string targetServer, out SecurityIdentifier sid);

        bool SelectContainer(IViewAware owner, string dialogTitle, string treeViewTitle,string baseContainer, string selectedContainer, out string container);
    }
}
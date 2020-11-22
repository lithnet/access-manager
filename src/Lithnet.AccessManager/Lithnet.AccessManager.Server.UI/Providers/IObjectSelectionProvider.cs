using System.Collections.Generic;
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

        bool GetUsers(IViewAware owner, out List<SecurityIdentifier> sid);

        bool GetGroups(IViewAware owner, out List<SecurityIdentifier> sid);

        bool GetComputers(IViewAware owner, out List<SecurityIdentifier> sid);

        bool GetUserOrGroups(IViewAware owner, out List<SecurityIdentifier> sid);

        bool GetUsers(IViewAware owner, string targetServer, out List<SecurityIdentifier> sid);

        bool GetGroups(IViewAware owner, string targetServer, out List<SecurityIdentifier> sid);

        bool GetComputers(IViewAware owner, string targetServer, out List<SecurityIdentifier> sid);

        bool GetUserOrGroups(IViewAware owner, string targetServer, out List<SecurityIdentifier> sid);

        bool SelectContainer(IViewAware owner, string dialogTitle, string treeViewTitle, string baseContainer, string selectedContainer, out string container);
        bool GetUserOrServiceAccount(IViewAware owner, string targetServer, out SecurityIdentifier sid);
        bool GetUserOrServiceAccount(IViewAware owner, out SecurityIdentifier sid);
    }
}
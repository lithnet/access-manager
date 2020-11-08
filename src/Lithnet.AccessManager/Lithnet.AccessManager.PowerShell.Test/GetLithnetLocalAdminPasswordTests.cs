using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Management.Automation;
using System.Linq;

namespace Lithnet.AccessManager.PowerShell.Test
{
    [TestClass]
    public class GetLithnetLocalAdminPasswordTests
    {
        [TestMethod]
        public void GetLocalAdminPassword()
        {
            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();
            ps.AddCommand(new CmdletInfo("Get-LithnetLocalAdminPassword", typeof(GetLocalAdminPassword)));
            ps.AddParameter("ComputerName", "IDMDEV1\\PC1");
            var output = ps.Invoke();

            Assert.AreEqual(1, output.Count);
            var result = output[0];

            Assert.AreEqual("Password", result.Properties["Password"].Value);
        }

        [TestMethod]
        public void GetLocalAdminPasswordHistory()
        {
            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();
            ps.AddCommand(new CmdletInfo("Get-LithnetLocalAdminPasswordHistory", typeof(GetLocalAdminPasswordHistory)));
            ps.AddParameter("ComputerName", "IDMDEV1\\PC1");
            var output = ps.Invoke();

            Assert.AreEqual(3, output.Count);

            var passwords = output.Select(t => t.Properties["Password"].Value as string).ToList();

            CollectionAssert.AreEquivalent(new[] { "History-1", "History-2", "History-3" }, passwords);
        }
    }
}

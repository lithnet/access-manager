using System.Linq;
using Lithnet.Laps.Web.Audit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lithnet.Laps.WebTests.Audit
{
    [TestClass()]
    public class UsersToNotifyTests
    {
        [TestMethod()]
        public void UnionTest()
        {
            var usersToNotify1 = new UsersToNotify(
                "frits@example.com,freddy@example.com",
                "bert@example.com"
                );

            var usersToNotify2 = new UsersToNotify(
                "frits@example.com,franky@example.com",
                "ernie@example.com"
                );

            var combined = usersToNotify1.Union(usersToNotify2);

            var expected = new UsersToNotify(
                "franky@example.com,freddy@example.com,frits@example.com",
                "bert@example.com,ernie@example.com"
                );

            // There should be a better way to test this...
            CollectionAssert.AreEqual(expected.OnFailure.ToArray(), combined.OnFailure.ToArray());
            CollectionAssert.AreEqual(expected.OnSuccess.ToArray(), combined.OnSuccess.ToArray());
        }
    }
}
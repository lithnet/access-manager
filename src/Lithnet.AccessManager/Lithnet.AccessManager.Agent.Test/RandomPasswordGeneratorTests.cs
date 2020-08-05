using System.Linq;
using System.Security.Cryptography;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Agent.Test
{
    public class RandomPasswordGeneratorTests
    {
        private RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();

        private Mock<ILapsSettings> settings;

        [SetUp()]
        public void TestInitialize()
        {
            settings = new Mock<ILapsSettings>();
        }

        [Test]
        public void TestPasswordLength()
        {
            settings.SetupGet(t => t.PasswordLength).Returns(12);

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            Assert.AreEqual(12, g.Generate().Length);
        }

        [Test]
        public void TestPasswordUseLower()
        {
            settings.SetupGet(t => t.UseLower).Returns(true);

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            string password = g.Generate();

            foreach(char c in password)
            {
                Assert.IsTrue(char.IsLower(c));
            }
        }

        [Test]
        public void TestPasswordUseUpper()
        {
            settings.SetupGet(t => t.UseUpper).Returns(true);

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            string password = g.Generate();

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsUpper(c));
            }
        }


        [Test]
        public void TestPasswordUseNumeric()
        {
            settings.SetupGet(t => t.UseNumeric).Returns(true);

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            string password = g.Generate();

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsNumber(c));
            }
        }


        [Test]
        public void TestPasswordUseSymbol()
        {
            settings.SetupGet(t => t.UseSymbol).Returns(true);

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            string password = g.Generate();

            foreach (char c in password)
            {
                Assert.IsFalse(char.IsLetterOrDigit(c));
            }
        }

        [Test]
        public void TestPasswordUseUpperAndLower()
        {
            settings.SetupGet(t => t.UseUpper).Returns(true);
            settings.SetupGet(t => t.UseLower).Returns(true);

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            string password = g.Generate();

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsUpper(c) || char.IsLower(c));
            }
        }

        [Test]
        public void TestPasswordUseCustomCharSet()
        {
            settings.SetupGet(t => t.PasswordCharacters).Returns("a");

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            string password = g.Generate();

            foreach (char c in password)
            {
                Assert.AreEqual('a', c);
            }
        }

        [Test]
        public void TestPasswordUseReadibilitySeparator()
        {
            settings.SetupGet(t => t.UseReadabilitySeparator).Returns(true);
            settings.SetupGet(t => t.ReadabilitySeparatorInterval).Returns(3);
            settings.SetupGet(t => t.ReadabilitySeparator).Returns("_");
            settings.SetupGet(t => t.PasswordLength).Returns(12);

            RandomPasswordGenerator g = new RandomPasswordGenerator(settings.Object, this.csp);

            string password = g.Generate();

            Assert.AreEqual(15, password.Length);
            Assert.AreEqual(3, password.Where(t => t == '_').Count());
            Assert.AreEqual('_', password[3]);
            Assert.AreEqual('_', password[7]);
            Assert.AreEqual('_', password[11]);
        }
    }
}
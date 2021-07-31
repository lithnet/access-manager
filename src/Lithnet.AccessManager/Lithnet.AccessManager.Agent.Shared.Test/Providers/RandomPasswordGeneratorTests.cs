using Moq;
using NUnit.Framework;
using System.Security.Cryptography;
using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Cryptography;

namespace Lithnet.AccessManager.Agent.Providers.Test
{
    public class RandomPasswordGeneratorTests
    {
        private Mock<IPasswordPolicy> settings;
        private RandomPasswordGenerator passwordGenerator = new RandomPasswordGenerator(new RandomValueGenerator(RandomNumberGenerator.Create()));

        [SetUp()]
        public void TestInitialize()
        {
            this.settings = new Mock<IPasswordPolicy>();
        }

        [Test]
        public void TestPasswordLength()
        {
            this.settings.SetupGet(t => t.PasswordLength).Returns(12);

            Assert.AreEqual(12, this.passwordGenerator.Generate(this.settings.Object).Length);
        }

        [Test]
        public void TestPasswordUseLower()
        {
            this.settings.SetupGet(t => t.UseLower).Returns(true);

            string password = this.passwordGenerator.Generate(this.settings.Object);

            foreach(char c in password)
            {
                Assert.IsTrue(char.IsLower(c));
            }
        }

        [Test]
        public void TestPasswordUseUpper()
        {
            this.settings.SetupGet(t => t.UseUpper).Returns(true);

            string password = this.passwordGenerator.Generate(this.settings.Object);

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsUpper(c));
            }
        }


        [Test]
        public void TestPasswordUseNumeric()
        {
            this.settings.SetupGet(t => t.UseNumeric).Returns(true);

            string password = this.passwordGenerator.Generate(this.settings.Object);

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsNumber(c));
            }
        }


        [Test]
        public void TestPasswordUseSymbol()
        {
            this.settings.SetupGet(t => t.UseSymbol).Returns(true);

            string password = this.passwordGenerator.Generate(this.settings.Object);

            foreach (char c in password)
            {
                Assert.IsFalse(char.IsLetterOrDigit(c));
            }
        }

        [Test]
        public void TestPasswordUseUpperAndLower()
        {
            this.settings.SetupGet(t => t.UseUpper).Returns(true);
            this.settings.SetupGet(t => t.UseLower).Returns(true);

            string password = this.passwordGenerator.Generate(this.settings.Object);

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsUpper(c) || char.IsLower(c));
            }
        }

        [Test]
        public void TestPasswordUseCustomCharSet()
        {
            this.settings.SetupGet(t => t.PasswordCharacters).Returns("a");

            string password = this.passwordGenerator.Generate(this.settings.Object);

            foreach (char c in password)
            {
                Assert.AreEqual('a', c);
            }
        }
    }
}
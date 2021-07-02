using Moq;
using NUnit.Framework;
using System.Security.Cryptography;

namespace Lithnet.AccessManager.Agent.Test
{
    public class RandomPasswordGeneratorTests
    {
        private Mock<IPasswordPolicy> settings;
        private RandomPasswordGenerator passwordGenerator = new RandomPasswordGenerator(new RandomValueGenerator(RandomNumberGenerator.Create()));

        [SetUp()]
        public void TestInitialize()
        {
            settings = new Mock<IPasswordPolicy>();
        }

        [Test]
        public void TestPasswordLength()
        {
            settings.SetupGet(t => t.PasswordLength).Returns(12);

            Assert.AreEqual(12, passwordGenerator.Generate(settings.Object).Length);
        }

        [Test]
        public void TestPasswordUseLower()
        {
            settings.SetupGet(t => t.UseLower).Returns(true);

            string password = passwordGenerator.Generate(settings.Object);

            foreach(char c in password)
            {
                Assert.IsTrue(char.IsLower(c));
            }
        }

        [Test]
        public void TestPasswordUseUpper()
        {
            settings.SetupGet(t => t.UseUpper).Returns(true);

            string password = passwordGenerator.Generate(settings.Object);

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsUpper(c));
            }
        }


        [Test]
        public void TestPasswordUseNumeric()
        {
            settings.SetupGet(t => t.UseNumeric).Returns(true);

            string password = passwordGenerator.Generate(settings.Object);

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsNumber(c));
            }
        }


        [Test]
        public void TestPasswordUseSymbol()
        {
            settings.SetupGet(t => t.UseSymbol).Returns(true);

            string password = passwordGenerator.Generate(settings.Object);

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

            string password = passwordGenerator.Generate(settings.Object);

            foreach (char c in password)
            {
                Assert.IsTrue(char.IsUpper(c) || char.IsLower(c));
            }
        }

        [Test]
        public void TestPasswordUseCustomCharSet()
        {
            settings.SetupGet(t => t.PasswordCharacters).Returns("a");

            string password = passwordGenerator.Generate(settings.Object);

            foreach (char c in password)
            {
                Assert.AreEqual('a', c);
            }
        }
    }
}
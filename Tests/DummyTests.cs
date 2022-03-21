using System;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class DummyTests
    {

        [Test]
        public void Success()
        {
            Assert.That(true, Is.True);
        }

        [Test]
        public void Failure()
        {
            Assert.That(3, Is.LessThan(2));
        }

        [Test]
        public void Skipped()
        {
            Assert.Ignore("Something is not setup");
        }

        [Test, Explicit]
        public void Explicit()
        {
            Assert.Fail();
        }

        [Test]
        public void Mixed([Values(true, false)] bool value)
        {
            Assert.That(value, Is.True, $"Expected: {value}");
        }
    }
}

using System;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Utilities
{
    public class ConfiguredObjectTest
    {
        private class TestObject : ConfiguredObject
        {
            public new bool ConfigurationLocked => base.ConfigurationLocked;

            public new void LockConfiguration() => base.LockConfiguration();

            public new void EnsureConfigurationNotLocked() => base.EnsureConfigurationNotLocked();

            public bool OnBeforeLockConfigurationCalled { get; private set; }

            protected override void OnBeforeLockConfiguration() => OnBeforeLockConfigurationCalled = true;
        }

        [Test]
        public void BeforeLock_IsNotLocked()
        {
            var obj = new TestObject();
            Assert.IsFalse(obj.ConfigurationLocked);
        }

        [Test]
        public void AfterLock_IsLocked()
        {
            var obj = new TestObject();
            obj.LockConfiguration();
            Assert.IsTrue(obj.ConfigurationLocked);
        }

        [Test]
        public void OnBeforeLockConfiguration_BeforeLock_HasNotBeenCalled()
        {
            var obj = new TestObject();
            Assert.IsFalse(obj.OnBeforeLockConfigurationCalled);
        }

        [Test]
        public void OnBeforeLockConfiguration_AfterLock_HasBeenCalled()
        {
            var obj = new TestObject();
            obj.LockConfiguration();
            Assert.IsTrue(obj.OnBeforeLockConfigurationCalled);
        }

        [Test]
        public void LockConfiguration_AfterLock_Succeeds()
        {
            var obj = new TestObject();
            obj.LockConfiguration();
            Assert.DoesNotThrow(() => obj.LockConfiguration());
        }

        [Test]
        public void EnsureConfigurationNotLocked_BeforeLock_Succeeds()
        {
            var obj = new TestObject();
            Assert.DoesNotThrow(() => obj.EnsureConfigurationNotLocked());
        }

        [Test]
        public void EnsureConfigurationNotLocked_AfterLock_ThrowsInvalidOperationException()
        {
            var obj = new TestObject();
            obj.LockConfiguration();
            Assert.Throws<InvalidOperationException>(() => obj.EnsureConfigurationNotLocked());
        }
    }
}
using System;
using EmbedIO.Tests.TestObjects;
using EmbedIO.Utilities;
using NUnit.Framework;

namespace EmbedIO.Tests.Utilities
{
    public class DisposableComponentCollectionTest
    {
        private class Item : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose() => Disposed = true;
        }

        [Test]
        public void Dispose_DisposesComponents()
        {
            var item = new Item();
            using (new DisposableComponentCollection<Item> { item })
            {
            }

            Assert.IsTrue(item.Disposed);
        }

        [Test]
        public void Finalizer_DoesNotDisposeComponents()
        {
            var item = new Item();

            // We need this to make sure that:
            // 1. collection is actually created (not optimized out);
            // 2. collection goes out of scope before we call GC.Collect.
            void CreateAndForgetCollection()
            {
                var collection = new DisposableComponentCollection<Item> { item };
                collection.Count.Void();
            }

            CreateAndForgetCollection();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.IsFalse(item.Disposed);
        }
    }
}
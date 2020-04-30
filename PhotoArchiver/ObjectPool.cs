using System;
using System.Collections.Concurrent;

namespace PhotoArchiver
{
    public class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> objects = new ConcurrentBag<T>();
        private readonly Func<T> objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null)
                throw new ArgumentNullException(nameof(objectGenerator));

            this.objectGenerator = objectGenerator;
        }

        public T GetObject()
        {
            if (objects.TryTake(out T item))
                return item;

            return objectGenerator();
        }

        public void PutObject(T item)
        {
            objects.Add(item);
        }
    }
}

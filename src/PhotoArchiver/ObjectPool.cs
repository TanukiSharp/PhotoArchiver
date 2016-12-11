using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoArchiver
{
    public class ObjectPool<T>
    {
        private ConcurrentBag<T> objects = new ConcurrentBag<T>();
        private Func<T> objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null)
                throw new ArgumentNullException(nameof(objectGenerator));

            this.objectGenerator = objectGenerator;
        }

        public T GetObject()
        {
            T item;

            if (objects.TryTake(out item))
                return item;

            return objectGenerator();
        }

        public void PutObject(T item)
        {
            objects.Add(item);
        }
    }
}

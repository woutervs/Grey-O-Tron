using System.Collections.Generic;

namespace GreyOTron.Library.Helpers
{
    public class CarrouselHelper<T>
    {
        private readonly Queue<T> queue;
        public CarrouselHelper(IEnumerable<T> messages)
        {
            queue = new Queue<T>(messages);
        }

        public T Next()
        {
            var m = queue.Dequeue();
            queue.Enqueue(m);
            return m;
        }
    }
}

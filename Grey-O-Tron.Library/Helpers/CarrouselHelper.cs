using System.Collections.Generic;

namespace GreyOTron.Library.Helpers
{
    public class CarrouselHelper
    {
        private readonly Queue<string> queue;
        public CarrouselHelper(IEnumerable<string> messages)
        {
            queue = new Queue<string>(messages);
        }

        public string Next()
        {
            var m = queue.Dequeue();
            queue.Enqueue(m);
            return m;
        }
    }
}

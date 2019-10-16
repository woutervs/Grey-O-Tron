using System.Collections.Generic;

namespace GreyOTron.Library.Helpers
{
    public class Carrousel
    {
        private readonly Queue<string> queue;
        public Carrousel(IEnumerable<string> messages)
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

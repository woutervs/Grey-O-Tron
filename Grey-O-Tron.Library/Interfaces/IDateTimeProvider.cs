using System;

namespace GreyOTron.Library.Interfaces
{
    public interface IDateTimeNowProvider
    {
        public DateTime UtcNow { get; }
        public DateTime Now { get; }
    }
}

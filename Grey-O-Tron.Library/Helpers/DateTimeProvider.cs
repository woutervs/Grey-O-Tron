using System;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Helpers
{
    public class DateTimeProvider : IDateTimeNowProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime Now => DateTime.Now;
    }
}

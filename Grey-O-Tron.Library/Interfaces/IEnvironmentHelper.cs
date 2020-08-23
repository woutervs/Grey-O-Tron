using GreyOTron.Library.Helpers;

namespace GreyOTron.Library.Interfaces
{
    public interface IEnvironmentHelper
    {
        Environments Current { get; }
        bool Is(Environments environment);
    }
}
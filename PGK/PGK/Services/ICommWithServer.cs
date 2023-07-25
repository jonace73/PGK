using System.Threading.Tasks;

namespace PGK.Services
{
    public interface ICommWithServer
    {
        Task<bool> SendByDependency(string msg, string serverMsg);
        Task<bool> CloseConnectionsByDependency();
    }
}

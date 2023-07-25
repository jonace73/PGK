using PGK.Services;
using System.Threading.Tasks;

namespace PGK.iOS.Services
{
    internal class IOSCommWithServer : ICommWithServer
    {
        public Task<bool> SendByDependency(string msg, string serverMsg)
        {
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
        }
        public Task<bool> CloseConnectionsByDependency()
        {
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
        }
    }
}

using Xamarin.Forms;
using PGK.iOS.Services;
using PGK.Services;

[assembly: Dependency(typeof(iOSCrash))]
namespace PGK.iOS.Services
{
    internal class iOSCrash : ICrash
    {
        public void MakeToast(string txt)
        {

        }
    }
}
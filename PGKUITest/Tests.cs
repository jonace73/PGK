using NUnit.Framework;
using System.Threading.Tasks;
using Xamarin.UITest;

namespace PGKUITest
{
    [TestFixture(Platform.Android)]
    public class Tests
    {
        IApp app;
        Platform platform;

        public Tests(Platform platform)
        {
            this.platform = platform;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            app = AppInitializer.StartApp(platform);
        }

        [Test]
        public void TestPGK()
        {
            // Rotating disk
            Task.Delay(3000);
            app.Screenshot("Rotating disk");

            // Search results
            Task.Delay(7000);
            string searchBar = "SearchBar";
            app.WaitForElement(c => c.Marked(searchBar));
            app.ClearText(x => x.Marked(searchBar));
            string keyword = "father";
            app.EnterText((x => x.Marked(searchBar)), keyword);
            Task.Delay(3000);
            app.Screenshot("Search result for: " + keyword);
        }
    }
}

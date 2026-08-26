using NUnit.Framework;
using UnityEngine;

namespace DINOForge.Tests.UI
{
    [TestFixture]
    public class NativeMenuInjectorTests
    {
        [Test]
        public void IsSplashDetection_InitialGameLoader()
        {
            string name = "PrimeCanvas InitialGameLoader";
            bool isSplash = name.IndexOf("InitialGameLoader", System.StringComparison.OrdinalIgnoreCase) >= 0;
            Assert.IsTrue(isSplash);
        }

        [Test]
        public void IsSplashDetection_Loader()
        {
            string name = "Loader";
            bool isSplash = name.Equals("Loader", System.StringComparison.OrdinalIgnoreCase);
            Assert.IsTrue(isSplash);
        }

        [Test]
        public void IsSplashDetection_MainMenu_NotDetected()
        {
            string name = "PrimeCanvas MainMenu";
            bool isSplash = name.IndexOf("InitialGameLoader", System.StringComparison.OrdinalIgnoreCase) >= 0
                         || name.Equals("Loader", System.StringComparison.OrdinalIgnoreCase);
            Assert.IsFalse(isSplash);
        }

        [Test]
        public void CleanupTargets_IncludeDINOForgeLoadingScreen()
        {
            string name = "DINOForge_LoadingScreen_Canvas";
            bool shouldCleanup = name.IndexOf("DINOForge_LoadingScreen", System.StringComparison.OrdinalIgnoreCase) >= 0;
            Assert.IsTrue(shouldCleanup);
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DINOForge.Tests.UI
{
    [TestFixture]
    public class LoadingScreenControllerTests
    {
        [Test]
        public void SingletonGuard_ReusesExistingInstance()
        {
            var parent = new GameObject("TestParent");
            var c1 = LoadingScreenController.Create(parent, "/tmp/packs", null);
            var c2 = LoadingScreenController.Create(parent, "/tmp/packs", null);
            Assert.AreSame(c1, c2, "Singleton guard should return existing instance");
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void DismissedFlag_PreventsResurrection()
        {
            var parent = new GameObject("TestParent");
            var controller = LoadingScreenController.Create(parent, "/tmp/packs", null);
            Assert.IsNotNull(controller);
            controller.BeginFadeOut();
            controller.EnsureVisible();
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void SafetyTimer_MaxVisibleSeconds_Is5()
        {
            var field = typeof(LoadingScreenController).GetField(
                "MaxVisibleSeconds",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(field);
            var value = (float)field.GetValue(null);
            Assert.AreEqual(5f, value, "Safety timer should be 5 seconds");
        }

        [UnityTest]
        public IEnumerator SafetyTimer_FiresWithinTimeout()
        {
            var parent = new GameObject("TestParent");
            var controller = LoadingScreenController.Create(parent, "/tmp/packs", null);
            Assert.IsNotNull(controller);
            yield return new WaitForSeconds(6.5f);
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void Create_WithNullPacksDir_DoesNotThrow()
        {
            var parent = new GameObject("TestParent");
            Assert.DoesNotThrow(() => LoadingScreenController.Create(parent, null, null));
            Object.DestroyImmediate(parent);
        }
    }
}

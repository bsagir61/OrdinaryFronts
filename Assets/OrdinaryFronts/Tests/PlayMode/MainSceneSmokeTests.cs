using System;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Collections;

namespace OrdinaryFronts.Tests.PlayMode
{
    public sealed class MainSceneSmokeTests
    {
        private string testDirectory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            testDirectory = Path.Combine(Application.temporaryCachePath, "OrdinaryFrontsPlayMode", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
            AppController.TestSaveDirectoryOverride = testDirectory;
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AppController.TestSaveDirectoryOverride = null;
            yield return null;
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }

        [UnityTest]
        public IEnumerator MainScene_NewGameTwoChoicesAndAutosaveWork()
        {
            AppController app = UnityEngine.Object.FindObjectOfType<AppController>();
            Assert.That(app, Is.Not.Null, "Main sahnesinde AppController yok.");
            Assert.That(app.CurrentScreen, Is.EqualTo(AppScreen.MainMenu));
            GameObject menu = GameObject.Find("Main Menu");
            Assert.That(menu, Is.Not.Null, "Ana menü görünmüyor.");

            app.StartNewGameForTests();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(app.CurrentScreen, Is.EqualTo(AppScreen.Gameplay));
            string firstNode = app.CurrentNodeId;
            Assert.That(firstNode, Is.Not.Null.And.Not.Empty);
            Assert.That(File.Exists(app.ActiveSavePath), Is.True, "Yeni oyunda otomatik kayıt oluşmadı.");

            GameObject leftObject = GameObject.Find("Left Choice");
            Assert.That(leftObject, Is.Not.Null);
            Button left = leftObject.GetComponent<Button>();
            Assert.That(left, Is.Not.Null);
            Assert.That(left.interactable, Is.True);
            left.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.55f);
            string secondNode = app.CurrentNodeId;
            Assert.That(secondNode, Is.Not.EqualTo(firstNode));
            Assert.That(File.Exists(app.ActiveSavePath), Is.True);

            GameObject rightObject = GameObject.Find("Right Choice");
            Assert.That(rightObject, Is.Not.Null);
            Button right = rightObject.GetComponent<Button>();
            Assert.That(right, Is.Not.Null);
            Assert.That(right.interactable, Is.True);
            right.onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(app.CurrentNodeId, Is.Not.EqualTo(secondNode));

            GameObject gameplay = GameObject.Find("Gameplay");
            TMP_Text[] text = gameplay.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < text.Length; i++) Assert.That(text[i].text, Does.Not.Contain("Ordinary Fronts"), "Ürün adı oynanış HUD'ında görünmemeli.");
        }
    }
}

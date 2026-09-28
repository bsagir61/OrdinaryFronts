using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OrdinaryFronts.Tests.EditMode
{
    /// <summary>
    /// Tuş ipuçları: hiçbir yerelleştirme metni ham yer tutucu ("{ACT}") ya da cihaza bağlı
    /// bir tuş adı bırakmamalı; oyun koluna geçildiğinde klavye adı görünmemeli. Input
    /// Manager'da oyun kolu eksenleri tanımlı olmalı; yoksa kol yalnız düğmelerle çalışır.
    /// </summary>
    public sealed class InputGlyphsTests
    {
        private static string StreamingRoot { get { return Path.Combine(Application.dataPath, "StreamingAssets"); } }

        [Test]
        public void EveryLocalizedString_ResolvesAllTokens_OnBothDevices()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                LocalizationService localization = new LocalizationService(StreamingRoot);
                localization.Load(locale);
                foreach (string key in UiKey.All())
                {
                    string raw = localization.Get(key);
                    foreach (InputDevice device in new[] { InputDevice.KeyboardMouse, InputDevice.Gamepad })
                    {
                        string resolved = InputGlyphs.Apply(raw, device, localization.Get);
                        Assert.That(resolved, Does.Not.Contain("{"), locale + "/" + key + "/" + device);
                    }
                }
            }
        }

        [Test]
        public void GamepadHints_NameNoKeyboardKeys()
        {
            foreach (string locale in LocalizationService.SupportedLocales)
            {
                LocalizationService localization = new LocalizationService(StreamingRoot);
                localization.Load(locale);
                string[] keys = { UiKey.IlWalkHow, UiKey.IlWireHint, UiKey.IlGuideHint, UiKey.IlPlankHow, UiKey.IlBalanceHint, UiKey.IlHoldHint, UiKey.InterludeSkipHint, UiKey.GameplayPauseHint };
                foreach (string key in keys)
                {
                    string pad = InputGlyphs.Apply(localization.Get(key), InputDevice.Gamepad, localization.Get);
                    Assert.That(pad, Does.Not.Contain("Esc").And.Not.Contain("ESC").And.Not.Contain("SPACE").And.Not.Contain("BOŞLUK").And.Not.Contain("D / →"),
                        locale + "/" + key + ": " + pad);
                    string keyboard = InputGlyphs.Apply(localization.Get(key), InputDevice.KeyboardMouse, localization.Get);
                    Assert.That(keyboard, Does.Not.Contain("LB").And.Not.Contain("RB"), locale + "/" + key + ": " + keyboard);
                }
            }
        }

        [Test]
        public void InputManager_DefinesGamepadAxes()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ProjectSettings", "InputManager.asset");
            string text = File.ReadAllText(path);
            Assert.That(text, Does.Contain("m_Name: OF Pad Stick X"));
            Assert.That(text, Does.Contain("m_Name: OF Pad DPad X"));
        }
    }
}

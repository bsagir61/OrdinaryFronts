using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OrdinaryFronts
{
    /// <summary>
    /// Oyun kolu ve Steam Deck desteği. Eski Input Manager ile çalışır; <c>BuildAll</c> üç
    /// eksen ekler (sol çubuk X, yön tuşları X/Y). Tuş eşlemesi oynanışla aynı mantıktadır:
    /// <list type="bullet">
    /// <item>LB / yön tuşu sol / sol çubuk sol — sol seçenek (A/← karşılığı)</item>
    /// <item>RB / yön tuşu sağ / sol çubuk sağ — sağ seçenek (D/→ karşılığı)</item>
    /// <item>A — onay ve ara sahnelerde "elinle yap" (BOŞLUK / sol tık karşılığı)</item>
    /// <item>B — geri, ara sahneyi geç (Esc karşılığı); Menü — duraklat</item>
    /// </list>
    /// Son kullanılan cihaz izlenir: kola dokunulduğunda ipuçları kol adlarına döner ve fare
    /// imleci gizlenir; klavyeye ya da fareye dokunulduğunda geri gelir.
    /// </summary>
    public sealed partial class AppController
    {
        private const string PadStickXAxis = "OF Pad Stick X";
        private const string PadDpadXAxis = "OF Pad DPad X";
        private const float PadThreshold = 0.55f;

        private InputDevice inputDevice = InputDevice.KeyboardMouse;
        private int glyphVersion;
        private bool padAxesAvailable = true;
        private float padX;
        private float padXPrevious;
        private Vector3 lastMousePosition;

        /// <summary>Şu an geçerli cihaz; ipuçları buna göre yazılır.</summary>
        public InputDevice CurrentInputDevice { get { return inputDevice; } }

        private bool PadLeftHeld { get { return padX <= -PadThreshold || Input.GetKey(KeyCode.JoystickButton4); } }
        private bool PadRightHeld { get { return padX >= PadThreshold || Input.GetKey(KeyCode.JoystickButton5); } }
        private bool PadLeftDown { get { return (padX <= -PadThreshold && padXPrevious > -PadThreshold) || Input.GetKeyDown(KeyCode.JoystickButton4); } }
        private bool PadRightDown { get { return (padX >= PadThreshold && padXPrevious < PadThreshold) || Input.GetKeyDown(KeyCode.JoystickButton5); } }
        private static bool PadActionHeld { get { return Input.GetKey(KeyCode.JoystickButton0); } }
        private static bool PadActionDown { get { return Input.GetKeyDown(KeyCode.JoystickButton0); } }
        private static bool PadBackDown { get { return Input.GetKeyDown(KeyCode.JoystickButton1); } }
        private static bool PadPauseDown { get { return Input.GetKeyDown(KeyCode.JoystickButton7); } }

        /// <summary>Geri / duraklat: Esc, B ya da Menü.</summary>
        private static bool BackPressed { get { return Input.GetKeyDown(KeyCode.Escape) || PadBackDown || PadPauseDown; } }

        /// <summary>Yerelleştirme metnindeki tuş yer tutucularını cihaza göre doldurur.</summary>
        private string Glyphify(string text)
        {
            return InputGlyphs.Apply(text, inputDevice, key => localization == null ? null : localization.Get(key));
        }

        /// <summary>Anahtarı çevirir ve tuş yer tutucularını doldurur.</summary>
        private string TG(string key)
        {
            return Glyphify(T(key));
        }

        /// <summary>
        /// Her karenin başında: kol eksenlerini okur ve son kullanılan cihazı günceller.
        /// Eksenler tanımlı değilse (proje ayarları eski) bir kez uyarır ve kol yalnız
        /// düğmelerle çalışır.
        /// </summary>
        private void UpdateInputDevice()
        {
            padXPrevious = padX;
            padX = 0f;
            if (padAxesAvailable)
            {
                try
                {
                    float stick = Input.GetAxisRaw(PadStickXAxis);
                    float dpad = Input.GetAxisRaw(PadDpadXAxis);
                    padX = Mathf.Abs(dpad) > Mathf.Abs(stick) ? dpad : stick;
                }
                catch (ArgumentException)
                {
                    padAxesAvailable = false;
                    Debug.LogWarning("Oyun kolu eksenleri Input Manager'da yok; BuildAll çalıştırılmalı. Kol yalnız düğmelerle çalışacak.");
                }
            }

            bool padActivity = Mathf.Abs(padX) >= PadThreshold;
            for (int i = 0; i < 12 && !padActivity; i++)
                if (Input.GetKeyDown(KeyCode.JoystickButton0 + i)) padActivity = true;

            Vector3 mouse = Input.mousePosition;
            bool mouseMoved = (mouse - lastMousePosition).sqrMagnitude > 16f;
            lastMousePosition = mouse;
            bool keyboardActivity = mouseMoved || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
                || (!string.IsNullOrEmpty(Input.inputString)) || AnyKeyboardNavigationDown();

            if (padActivity && inputDevice != InputDevice.Gamepad) SetInputDevice(InputDevice.Gamepad);
            else if (keyboardActivity && !padActivity && inputDevice != InputDevice.KeyboardMouse) SetInputDevice(InputDevice.KeyboardMouse);
        }

        private static bool AnyKeyboardNavigationDown()
        {
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.LeftArrow)
                || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
                || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Tab);
        }

        private void SetInputDevice(InputDevice device)
        {
            inputDevice = device;
            glyphVersion++;
            Cursor.visible = device == InputDevice.KeyboardMouse;
            RefreshGlyphs();
            // Kola geçildiğinde bir seçili öğe olmalı; yoksa çubuk ve yön tuşları menüde hiçbir şey yapmaz.
            if (device == InputDevice.Gamepad && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null
                && router != null && router.Current != AppScreen.Gameplay && router.Current != AppScreen.Interlude && router.Current != AppScreen.Intro)
                SelectFirstButton(router.Get(router.Current));
        }

        /// <summary>Ekranda kalıcı duran ipuçlarını cihaza göre yeniden yazar.</summary>
        private void RefreshGlyphs()
        {
            if (pauseHintText != null) pauseHintText.text = TG(UiKey.GameplayPauseHint);
            if (storyController != null && storyController.CurrentNode != null) RefreshChoiceKeyLabels(storyController.CurrentNode);
            RefreshTestimonyGlyphs();
            RefreshTimelineHint();
        }
    }
}

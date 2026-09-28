using System;

namespace OrdinaryFronts
{
    /// <summary>Oyuncunun o an kullandığı giriş cihazı; ipuçları buna göre yazılır.</summary>
    public enum InputDevice
    {
        KeyboardMouse,
        Gamepad
    }

    /// <summary>
    /// Tuş ipuçlarının tek kaynağı. Yerelleştirme metinleri tuş adı yazmaz, yer tutucu
    /// yazar: <c>{LEFT}</c>, <c>{RIGHT}</c>, <c>{ACT}</c>, <c>{BACK}</c>, <c>{PAUSE}</c>.
    /// Bu sınıf yer tutucuyu oyuncunun cihazına göre doldurur; oyun kolu alındığında bütün
    /// ipuçları "BOŞLUK / sol tık" yerine "A düğmesi" der, klavyeye dönüldüğünde geri döner.
    /// <para>
    /// Oyun kolu adları Xbox düzenindedir (A, B, LB, RB, Menü). Steam Input, Steam Deck ve
    /// PlayStation kollarını bu düzene çevirir; oyun ek bir eşleme istemez.
    /// </para>
    /// </summary>
    public static class InputGlyphs
    {
        public const string TokenLeft = "{LEFT}";
        public const string TokenRight = "{RIGHT}";
        public const string TokenAction = "{ACT}";
        public const string TokenBack = "{BACK}";
        public const string TokenPause = "{PAUSE}";

        /// <summary>Metindeki yer tutucuları cihaza göre doldurur; bilinmeyen metne dokunmaz.</summary>
        /// <param name="lookup">Yerelleştirilmiş cihaz adları için anahtar → metin.</param>
        public static string Apply(string text, InputDevice device, Func<string, string> lookup)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text ?? string.Empty;
            return text
                .Replace(TokenLeft, Left(device, lookup))
                .Replace(TokenRight, Right(device, lookup))
                .Replace(TokenAction, Action(device, lookup))
                .Replace(TokenBack, Back(device, lookup))
                .Replace(TokenPause, Pause(device, lookup));
        }

        public static string Left(InputDevice device, Func<string, string> lookup)
        {
            return device == InputDevice.Gamepad ? "LB / ←" : "A / ←";
        }

        public static string Right(InputDevice device, Func<string, string> lookup)
        {
            return device == InputDevice.Gamepad ? "RB / →" : "D / →";
        }

        public static string Action(InputDevice device, Func<string, string> lookup)
        {
            return device == InputDevice.Gamepad ? Look(lookup, UiKey.GlyphPadAction, "A button") : Look(lookup, UiKey.GlyphKeyAction, "SPACE / left click");
        }

        public static string Back(InputDevice device, Func<string, string> lookup)
        {
            return device == InputDevice.Gamepad ? "B" : "Esc";
        }

        public static string Pause(InputDevice device, Func<string, string> lookup)
        {
            return device == InputDevice.Gamepad ? Look(lookup, UiKey.GlyphPadPause, "MENU") : "ESC";
        }

        private static string Look(Func<string, string> lookup, string key, string fallback)
        {
            if (lookup == null) return fallback;
            string value = lookup(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }
    }
}

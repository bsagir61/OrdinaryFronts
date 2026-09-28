using UnityEditor;
using UnityEngine;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Oyun kolu eksenlerini eski Input Manager'a ekler. Idempotenttir: aynı ad, tür ve
    /// eksen numarasıyla bir giriş zaten varsa dokunmaz. Giriş sistemi yükseltilmez; yalnız
    /// eksen tanımı eklenir.
    /// <list type="bullet">
    /// <item><c>OF Pad Stick X</c> — sol çubuk yatay (eksen 1)</item>
    /// <item><c>OF Pad DPad X</c> — yön tuşları yatay (XInput'ta 6. eksen)</item>
    /// <item><c>Horizontal</c> / <c>Vertical</c> — yön tuşlarının 6. ve 7. eksenleri; arayüz
    /// gezinmesi (StandaloneInputModule) böylece yön tuşlarıyla da çalışır. Aynı adlı
    /// eksenler Unity'de birleşir; klavye ve sol çubuk tanımları aynen kalır.</item>
    /// </list>
    /// </summary>
    internal static class InputAxesInstaller
    {
        private const string InputManagerPath = "ProjectSettings/InputManager.asset";

        internal static void EnsureGamepadAxes()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(InputManagerPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("Input Manager bulunamadı; oyun kolu eksenleri eklenmedi.");
                return;
            }
            SerializedObject manager = new SerializedObject(assets[0]);
            SerializedProperty axes = manager.FindProperty("m_Axes");
            bool changed = false;
            changed |= Ensure(axes, "OF Pad Stick X", 0, 0.2f, false);
            changed |= Ensure(axes, "OF Pad DPad X", 5, 0.1f, false);
            changed |= Ensure(axes, "Horizontal", 5, 0.1f, false);
            changed |= Ensure(axes, "Vertical", 6, 0.1f, false);
            if (changed)
            {
                manager.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        private static bool Ensure(SerializedProperty axes, string name, int axis, float dead, bool invert)
        {
            for (int i = 0; i < axes.arraySize; i++)
            {
                SerializedProperty existing = axes.GetArrayElementAtIndex(i);
                if (existing.FindPropertyRelative("m_Name").stringValue == name
                    && existing.FindPropertyRelative("type").intValue == 2
                    && existing.FindPropertyRelative("axis").intValue == axis) return false;
            }
            axes.arraySize++;
            SerializedProperty entry = axes.GetArrayElementAtIndex(axes.arraySize - 1);
            entry.FindPropertyRelative("m_Name").stringValue = name;
            entry.FindPropertyRelative("descriptiveName").stringValue = string.Empty;
            entry.FindPropertyRelative("descriptiveNegativeName").stringValue = string.Empty;
            entry.FindPropertyRelative("negativeButton").stringValue = string.Empty;
            entry.FindPropertyRelative("positiveButton").stringValue = string.Empty;
            entry.FindPropertyRelative("altNegativeButton").stringValue = string.Empty;
            entry.FindPropertyRelative("altPositiveButton").stringValue = string.Empty;
            entry.FindPropertyRelative("gravity").floatValue = 0f;
            entry.FindPropertyRelative("dead").floatValue = dead;
            entry.FindPropertyRelative("sensitivity").floatValue = 1f;
            entry.FindPropertyRelative("snap").boolValue = false;
            entry.FindPropertyRelative("invert").boolValue = invert;
            entry.FindPropertyRelative("type").intValue = 2;
            entry.FindPropertyRelative("axis").intValue = axis;
            entry.FindPropertyRelative("joyNum").intValue = 0;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;

namespace ReentryParticleEffect
{
    public static class GuiUtils
    {
        public static void label(string text, object obj)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            GUILayout.Label(obj == null ? "null" : obj.ToString(), GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        /*public static float editFloat(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            bool maxVal = value == float.MaxValue;
            bool minVal = value == float.MinValue;
            string displayVal = maxVal || minVal ? "" : value.ToString();
            string v = GUILayout.TextField(displayVal, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            float f = value;
            if (v == string.Empty)
            {
                if (maxVal)
                    f = float.MaxValue;
                else if (minVal)
                    f = float.MinValue;
                else
                    f = 0;
            }
            else
                float.TryParse(v, out f);
            return f;
        }*/

        public static string editFloat(string label, string text, out float value, float defaultValue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            string newText = GUILayout.TextField(text, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            if (String.IsNullOrEmpty(text))
                value = defaultValue;
            else
            {
                Regex numericOnly = new Regex(@"[^\d.-]");
                newText = numericOnly.Replace(newText, "");

                float.TryParse(newText, out value);
            }
            //if (text != newText)
            //{
            //Debug.Log("####Updated " + label + " to " + value);
            //}
            return newText;
        }

        public static string editString(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            string v = GUILayout.TextField(value, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            return v;
        }

        public static T editEnum<T>(string label, T value) where T : struct, IConvertible
        {
            Type genericType = typeof(T);
            if (!genericType.IsEnum)
                throw new ArgumentException("Type 'T' must be an enum");

            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            //GUILayout.FlexibleSpace();
            GUILayout.Label(" ");
            foreach (var e in Enum.GetValues(typeof(T)))
            {
                string name = e.ToString();
                GUILayout.Toggle(false, ""); // Text parameter causes overlap.
                GUILayout.Label(name + " ");
            }
            GUILayout.EndHorizontal();

            return value;
        }

        public static void slider(string label, ref float variable, float from, float to)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ": " + variable.ToString());
            GUILayout.FlexibleSpace();
            variable = GUILayout.HorizontalSlider(variable, from, to, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        public static Color rgbaSlider(string label, ref float r, ref float g, ref float b, ref float a, float from, float to)
        {
            GUILayout.Label(label);
            slider("r", ref r, from, to);
            slider("g", ref g, from, to);
            slider("b", ref b, from, to);
            slider("a", ref a, from, to);
            return new Color(r, g, b, a);

        }

        static float x = 0;
        static float y = 0;
        public static KeyValuePair<float, float> GetSliderXY()
        {
            return new KeyValuePair<float, float>(x, y);
        }
    }

    /*
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TestGui : MonoBehaviour
    {
        private static Rect windowPos = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);

        /// <summary>
        /// GUI draw event. Called (at least once) each frame.
        /// </summary>
        public void OnGUI()
        {
            if (ReentryParticleEffect.DrawGui)
                windowPos = GUILayout.Window(GetInstanceID(), windowPos, MainGUI, "Reentry Particle Effect", GUILayout.Width(600), GUILayout.Height(50));
        }
        private static void MainGUI(int windowID)
        {
            GUILayout.BeginVertical();

            ReentryParticleEffect.Gui();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }*/
}

namespace Assets.Scripts.CustomDebug
{
using UnityEngine;
using System;
using System.Collections.Generic;
    public class DebugStatsDisplay : MonoBehaviour
    {
        public static DebugStatsDisplay Instance { get; private set; }

        private readonly List<DebugStatsRequest> debugStatsRequests = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private GUIStyle backgroundStyle;
        private GUIStyle labelStyle;
        private Vector2 scrollPosition;
        
        void OnGUI()
        {
            backgroundStyle = new GUIStyle();
            backgroundStyle.normal.background = MakeTexture(2, 2, new Color(0.5f, 0.5f, 0.5f, 0.5f)); // Gray with 50% opacity
            
            // Create label style with larger text
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 18; // Adjust size as needed
            labelStyle.normal.textColor = Color.white; // Optional: make text white for better contrast

            GUILayout.BeginArea(new Rect(10, 10, 300, 500));
            GUILayout.BeginVertical(backgroundStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (var request in debugStatsRequests)
            {
                GUILayout.Label(request.GetDisplayString(), labelStyle);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
        }

        public void RegisterDebugStatsRequest(DebugStatsRequest request)
        {
            debugStatsRequests.Add(request);
        }
        public void UnregisterDebugStatsRequest(DebugStatsRequest request)
        {
            debugStatsRequests.Remove(request);
        }
        public void ClearDebugStatsRequests()
        {
            debugStatsRequests.Clear();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            
            Texture2D texture = new(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }

    public class DebugStatsRequest
    {
        private string _name;
        private Func<object> _valueGetter;

        public DebugStatsRequest(string name, Func<object> valueGetter)
        {
            _name = name;
            _valueGetter = valueGetter;
        }

        public string GetDisplayString()
        {
            try
            {
                return $"{_name}: {_valueGetter()}";
            }
            catch (System.Exception x)
            {
                return $"{_name} is null, ({x})";
            }

        }
    }



}

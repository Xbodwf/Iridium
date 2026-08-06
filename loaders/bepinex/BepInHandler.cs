using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using BepInEx.Logging;
using UnityEngine;
using static Iridium.UI.IridiumLayout;

namespace Iridium
{
    public class BepInHandler : IHandler
    {
        private readonly ManualLogSource _logger;
        private readonly string _modPath;
        private bool _uiVisible;
        private Rect _rect;
        private Vector2 _scrollPos;
        private bool _isDragging;
        private Vector2 _dragOffset;
        private float _titleBarHeight = 40f;

        public BepInHandler(ManualLogSource logger)
        {
            _logger = logger;
            _modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        }

        public string ModId => BuildInfo.ModName;
        public string ModVersion => BuildInfo.ModVersion;
        public string ModPath => _modPath;

        public void Log(string message) => _logger.LogInfo(message);
        public void Warning(string message) => _logger.LogWarning(message);
        public void Error(string message) => _logger.LogError(message);

        public T LoadSettings<T>() where T : class, new()
        {
            string settingsPath = Path.Combine(_modPath, "Settings.xml");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(T));
                    using var reader = new StreamReader(settingsPath);
                    return (T)(serializer.Deserialize(reader) ?? new T());
                }
                catch
                {
                    return new T();
                }
            }
            return new T();
        }

        public void SaveSettings<T>(T settings) where T : class
        {
            string settingsPath = Path.Combine(_modPath, "Settings.xml");
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using var writer = new StreamWriter(settingsPath);
                serializer.Serialize(writer, settings);
            }
            catch (Exception ex)
            {
                Log($"Failed to save settings: {ex.Message}");
            }
        }

        public float UIScale => 1048576;

        public event Action<float>? OnUpdate;
        public event Action<bool>? OnToggle;
        public event Action? OnGUI;
        public event Action? OnSaveGUI;

        private static bool CheckHotkey(string hotkey)
        {
            if (string.IsNullOrEmpty(hotkey)) return false;

            var parts = hotkey.Split('+');
            if (parts.Length == 0) return false;

            KeyCode? targetKey = null;
            bool needCtrl = false, needAlt = false, needShift = false;

            foreach (var part in parts)
            {
                var p = part.Trim();
                if (string.IsNullOrEmpty(p)) continue;

                var lower = p.ToLowerInvariant();
                if (lower == "ctrl")
                    needCtrl = true;
                else if (lower == "alt")
                    needAlt = true;
                else if (lower == "shift")
                    needShift = true;
                else
                {
                    if (Enum.TryParse<KeyCode>(p, ignoreCase: true, out var kc))
                        targetKey = kc;
                }
            }

            if (targetKey == null) return false;

            if (needCtrl && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                return false;
            if (needAlt && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                return false;
            if (needShift && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                return false;

            return Input.GetKeyDown(targetKey.Value);
        }

        public void TriggerToggle(bool value) => OnToggle?.Invoke(value);

        public void TriggerUpdate(float dt)
        {
            if (Main.Settings != null && CheckHotkey(Main.Settings.panelToggleHotkey))
            {
                _uiVisible = !_uiVisible;
                float w = Mathf.Max(Screen.width * 0.8f, 960);
                float h = Mathf.Max(Screen.height * 0.8f, 720);
                _rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            }
            OnUpdate?.Invoke(dt);
        }

        public void TriggerGUI()
        {
            if (!_uiVisible) return;

            HandleWindowDrag();

            Render(
                Area(_rect,
                    VBox(
                        ContainerStyle.Background,
                        null,
                        WidthMax,
                        HBox(
                            ContainerStyle.None,
                            null,
                            WidthMax,
                            Text("Iridium", TextStyle.Title),
                            Fill(),
                            Button("\u00d7", ButtonStyle.Element, () => _uiVisible = false, GUILayout.Width(28), GUILayout.Height(28))
                        ),
                        Space(8),
                        ScrollView(_scrollPos, p => _scrollPos = p, WidthMax, () => OnGUI?.Invoke())
                    )
                )
            );

            _rect.x = (int)_rect.x;
            _rect.y = (int)_rect.y;
        }

        private void HandleWindowDrag()
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.mousePosition.y >= _rect.y && e.mousePosition.y <= _rect.y + _titleBarHeight
                        && e.mousePosition.x < _rect.xMax - 36)
                    {
                        _isDragging = true;
                        _dragOffset = e.mousePosition - _rect.position;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    _isDragging = false;
                    break;
                case EventType.MouseDrag:
                    if (_isDragging)
                    {
                        _rect.position = e.mousePosition - _dragOffset;
                        e.Use();
                    }
                    break;
            }
        }
    }
}

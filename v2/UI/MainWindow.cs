using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Iridium.UI.IridiumLayout;

namespace Iridium.UI
{
    public class IridiumWindow : MonoBehaviour
    {
        public class ButtonConfig
        {
            public string Text { get; set; } = string.Empty;
            public Action? OnClick { get; set; }
            public ButtonStyle Style { get; set; } = ButtonStyle.Primary;
            public bool CloseOnClick { get; set; } = true;
        }

        public class Config
        {
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public IconStyle? Icon { get; set; }
            public Vector2 Size { get; set; } = new(400, 200);
            public ButtonConfig[] Buttons { get; set; } = Array.Empty<ButtonConfig>();
            public Action? OnClose { get; set; }
        }

        private Config _config = null!;
        private Rect _windowRect;
        private SizesGroup.Holder _sizesHolder = new();
        private bool _isDragging;
        private Vector2 _dragOffset;

        private static readonly List<IridiumWindow> _activeWindows = new();

        private static GameObject? _root;

        private static GameObject EnsureRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("IridiumWindowRoot");
                DontDestroyOnLoad(_root);
            }
            return _root;
        }

        public static IridiumWindow Show(Config config)
        {
            var root = EnsureRoot();
            var go = new GameObject("IridiumWindow");
            go.transform.SetParent(root.transform);
            var window = go.AddComponent<IridiumWindow>();
            window._config = config;
            window._windowRect = new Rect(
                (Screen.width - config.Size.x) / 2f,
                (Screen.height - config.Size.y) / 2f,
                config.Size.x,
                config.Size.y
            );
            _activeWindows.Add(window);
            return window;
        }

        public static void Close(IridiumWindow window)
        {
            if (window == null) return;
            _activeWindows.Remove(window);
            window._config?.OnClose?.Invoke();
            Destroy(window.gameObject);

            if (_activeWindows.Count == 0 && _root != null)
            {
                Destroy(_root);
                _root = null;
            }
        }

        public static void CloseAll()
        {
            foreach (var w in _activeWindows.ToArray())
                Close(w);
        }

        public static bool HasActiveWindows => _activeWindows.Count > 0;

        private void OnGUI()
        {
            EnsureTexturesAlive();
            DrawWindow();
        }

        private void DrawWindow()
        {
            var sizes = _sizesHolder.Begin();

            var header = new List<Element>();
            if (_config.Icon.HasValue)
            {
                header.Add(Icon(_config.Icon.Value));
                header.Add(Space(8));
            }
            header.Add(Text(_config.Title, TextStyle.Title, WidthMax));

            var content = new List<Element>
            {
                HBox(ContainerStyle.None, sizes, header.ToArray()),
                Space(10),
                Text(_config.Message, TextStyle.Normal, WidthMax),
                FlexibleSpace()
            };

            if (_config.Buttons.Length > 0)
            {
                var buttons = new List<Element> { Fill() };
                foreach (var btn in _config.Buttons)
                {
                    buttons.Add(Button(btn.Text, btn.Style, () =>
                    {
                        btn.OnClick?.Invoke();
                        if (btn.CloseOnClick)
                            Close(this);
                    }, Width(120)));
                }
                content.Add(HBox(ContainerStyle.None, sizes, buttons.ToArray()));
            }

            IridiumLayout.Render(
                Area(_windowRect,
                    VBox(ContainerStyle.Background, sizes, WidthMax, content.ToArray())
                )
            );

            HandleWindowDrag();
        }

        private void HandleWindowDrag()
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (_windowRect.Contains(e.mousePosition))
                    {
                        _isDragging = true;
                        _dragOffset = e.mousePosition - _windowRect.position;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    _isDragging = false;
                    break;
                case EventType.MouseDrag:
                    if (_isDragging)
                    {
                        _windowRect.position = e.mousePosition - _dragOffset;
                        e.Use();
                    }
                    break;
            }
        }
    }

    public static class MainWindow
    {
        public static void ShowFirstRun()
        {
            IridiumWindow.Show(new IridiumWindow.Config
            {
                Title = Localization.Get("FirstRunTitle"),
                Message = Localization.Get("FirstRunMessage"),
                Icon = IconStyle.Information,
                Size = new Vector2(400, 200),
                Buttons = new[]
                {
                    new IridiumWindow.ButtonConfig
                    {
                        Text = Localization.Get("Understand"),
                        Style = ButtonStyle.Primary,
                        CloseOnClick = true,
                        OnClick = () =>
                        {
                            Main.Settings.firstRun = false;
                            Main.Settings.lastVersion = VersionManager.GetFullVersionString();
                            Main.Settings.lastUpgradeMessageSeen_106_beta5 = "1.0.6_beta5";
                            Main.Handler?.SaveSettings(Main.Settings);
                        }
                    }
                }
            });
        }

        public static void ShowUpgrade(string messageKey)
        {
            IridiumWindow.Show(new IridiumWindow.Config
            {
                Title = Localization.Get("UpgradeTitle"),
                Message = Localization.Get(messageKey),
                Icon = IconStyle.Warning,
                Size = new Vector2(400, 200),
                Buttons = new[]
                {
                    new IridiumWindow.ButtonConfig
                    {
                        Text = Localization.Get("Understand"),
                        Style = ButtonStyle.Primary,
                        CloseOnClick = true,
                        OnClick = () =>
                        {
                            Main.Settings.lastVersion = VersionManager.GetFullVersionString();
                            Main.Settings.lastUpgradeMessageSeen_106_beta5 = "1.0.6_beta5";
                            Main.Handler?.SaveSettings(Main.Settings);
                        }
                    }
                }
            });
        }
    }
}
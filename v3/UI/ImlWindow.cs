using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Iris.Iml;

namespace Iridium.UI
{
    /// <summary>
    /// UGUI/IML-backed modal dialog. Equivalent of IridiumWindow (IMGUI) for the
    /// v3 project; the v2 project keeps the IMGUI path per project hard
    /// constraints. IML is generated dynamically via <see cref="BuildIml"/> so
    /// any number of buttons can be rendered without per-instance IML files.
    /// </summary>
    public class ImlWindow : MonoBehaviour
    {
        public enum IconStyle { Information, Success, Warning, Error, Stop }
        public enum ButtonStyle { Element, Primary }

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

        private IrisGoRenderer _renderer;
        private Config _config = null!;
        private static readonly List<ImlWindow> _activeWindows = new();

        public static ImlWindow Show(Config config)
        {
            var go = new GameObject("ImlWindow");
            DontDestroyOnLoad(go);
            var win = go.AddComponent<ImlWindow>();
            win._config = config;
            win.SetupRenderer();
            _activeWindows.Add(win);
            Main.Logger?.Log("[ImlWindow] Show() complete");
            return win;
        }

        public void Close()
        {
            _activeWindows.Remove(this);
            _config?.OnClose?.Invoke();
            if (_renderer?.RootObject != null)
                Destroy(_renderer.RootObject);
            Destroy(gameObject);
        }

        public static void Close(ImlWindow window)
        {
            window?.Close();
        }

        public static void CloseAll()
        {
            foreach (var w in _activeWindows.ToArray())
                w.Close();
        }

        public static bool HasActiveWindows => _activeWindows.Count > 0;

        /// <summary>Show the first-run welcome dialog. Called once on initial mod enable.</summary>
        public static void ShowFirstRun()
        {
            Show(new Config
            {
                Title = Localization.Get("FirstRunTitle"),
                Message = Localization.Get("FirstRunMessage"),
                Icon = IconStyle.Information,
                Size = new Vector2(400, 200),
                Buttons = new[]
                {
                    new ButtonConfig
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

        /// <summary>Show a version-upgrade notice dialog with content keyed by <paramref name="messageKey"/>.</summary>
        public static void ShowUpgrade(string messageKey)
        {
            Show(new Config
            {
                Title = Localization.Get("UpgradeTitle"),
                Message = Localization.Get(messageKey),
                Icon = IconStyle.Warning,
                Size = new Vector2(400, 200),
                Buttons = new[]
                {
                    new ButtonConfig
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

        private void SetupRenderer()
        {
            _renderer = new IrisGoRenderer
            {
                ParentTransform = transform,
            };

            // Data context: dictionary so we can dynamically add hasBtn{i}/btn{i}Text
            // entries for an arbitrary number of buttons.
            var data = new Dictionary<string, object>
            {
                ["title"] = _config.Title ?? string.Empty,
                ["message"] = _config.Message ?? string.Empty,
                ["iconType"] = _config.Icon.HasValue
                    ? _config.Icon.Value.ToString().ToLowerInvariant()
                    : string.Empty,
                ["showIcon"] = _config.Icon.HasValue,
            };
            for (int i = 0; i < _config.Buttons.Length; i++)
            {
                var idx = i + 1;
                data[$"btn{idx}Text"] = _config.Buttons[i].Text ?? string.Empty;
                data[$"btn{idx}Style"] = _config.Buttons[i].Style.ToString().ToLowerInvariant();
                data[$"hasBtn{idx}"] = true;
            }
            _renderer.SetDataContext(data);

            // Handlers: one OnBtn{i} per button, in declaration order.
            for (int i = 0; i < _config.Buttons.Length; i++)
            {
                var btn = _config.Buttons[i];
                var idx = i + 1;
                _renderer.RegisterHandler($"OnBtn{idx}", (obj) =>
                {
                    try { btn.OnClick?.Invoke(); }
                    catch (Exception ex) { Main.Logger?.Log($"[ImlWindow] OnBtn{idx} handler error: {ex}"); }
                    if (btn.CloseOnClick) Close();
                });
            }

            var iml = BuildIml(_config);
            _renderer.LoadContent(iml, basePath: Main.Handler?.ModPath ?? string.Empty);
            _renderer.Rebuild();
            Main.Logger?.Log("[ImlWindow] Render complete");
        }

        /// <summary>
        /// Build the IML document for <paramref name="config"/>. Mirrors the style
        /// values from the legacy IridiumWindow.iml (dialog radius 16, primary/element
        /// radius 8, dialog padding 16, dialog minWidth 400) and emits one
        /// <c>&lt;Button&gt;</c> per <see cref="Config.Buttons"/> entry because
        /// <see cref="IrisGoRenderer"/> does not yet support <c>&lt;ForEach&gt;</c>.
        /// </summary>
        private static string BuildIml(Config config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<Iris>");
            sb.AppendLine("    <Resources>");
            sb.AppendLine("        <Reference path=\"@Colors.iml\" />");
            sb.AppendLine("        <Style name=\"dialog\" on=\".dialog\">");
            sb.AppendLine("            <Setter property=\"padding\" value=\"16,16,16,16\" />");
            sb.AppendLine("            <Setter property=\"radius\" value=\"16\" />");
            sb.AppendLine("        </Style>");
            sb.AppendLine("        <Style name=\"title\" on=\".title\">");
            sb.AppendLine("            <Setter property=\"fontSize\" value=\"24\" />");
            sb.AppendLine("        </Style>");
            sb.AppendLine("        <Style name=\"normal\" on=\".normal\">");
            sb.AppendLine("            <Setter property=\"fontSize\" value=\"12\" />");
            sb.AppendLine("        </Style>");
            sb.AppendLine("    </Resources>");
            sb.AppendLine();
            sb.AppendLine("    <VBox class=\"dialog bg-default\" minWidth=\"400\">");
            sb.AppendLine("        <HBox gap=\"8\">");
            sb.AppendLine("            <If condition=\"{showIcon}\">");
            sb.AppendLine("                <Icon type={iconType} />");
            sb.AppendLine("            </If>");
            sb.AppendLine("            <Text text={title} class=\"title text-title\" />");
            sb.AppendLine("        </HBox>");
            sb.AppendLine("        <Spacer height=\"10\" />");
            sb.AppendLine("        <Text text={message} class=\"normal text-normal\" />");
            sb.AppendLine("        <Fill />");
            sb.AppendLine("        <HBox gap=\"8\">");
            sb.AppendLine("            <Fill />");
            for (int i = 0; i < config.Buttons.Length; i++)
            {
                var idx = i + 1;
                sb.AppendLine($"            <Button text={{btn{idx}Text}} class={{btn{idx}Style}} visible={{hasBtn{idx}}} on-click=\"OnBtn{idx}\" width=\"120\" />");
            }
            sb.AppendLine("        </HBox>");
            sb.AppendLine("    </VBox>");
            sb.AppendLine("</Iris>");
            return sb.ToString();
        }
    }
}

using UnityEngine;
using static Iridium.UI.IridiumLayout;

namespace Iridium.UI
{
    public class MainWindow : MonoBehaviour
    {
        private static MainWindow? _instance;
        private static bool _showFirstRunTips;
        private static bool _showUpgradeTips;
        private static string _upgradeMessageKey = "";
        private Rect _windowRect = new(Screen.width / 2f - 200, Screen.height / 2f - 100, 400, 200);
        private SizesGroup.Holder _sizesHolder = new();

        public static void ShowFirstRun()
        {
            _showFirstRunTips = true;
            _showUpgradeTips = false;
            EnsureInstance();
        }

        public static void ShowUpgrade(string messageKey)
        {
            _showUpgradeTips = true;
            _showFirstRunTips = false;
            _upgradeMessageKey = messageKey;
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                var go = new GameObject("IridiumMainWindow");
                _instance = go.AddComponent<MainWindow>();
                DontDestroyOnLoad(go);
            }
        }

        private void OnGUI()
        {
            EnsureTexturesAlive();

            if (_showFirstRunTips)
            {
                _windowRect = GUI.Window(998, _windowRect, DrawFirstRunWindow, Localization.Get("FirstRunTitle"));
                return;
            }

            if (_showUpgradeTips)
            {
                _windowRect = GUI.Window(997, _windowRect, DrawUpgradeWindow, Localization.Get("UpgradeTitle"));
            }
        }

        private void DrawFirstRunWindow(int windowID)
        {
            var sizes = _sizesHolder.Begin();
            Begin(ContainerDirection.Vertical, ContainerStyle.Padding);
            {
                Space(10);
                Text(Localization.Get("FirstRunMessage"), TextStyle.Normal, WidthMax);
                FlexibleSpace();
                Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                {
                    Fill();
                    if (Button(Localization.Get("Understand"), ButtonStyle.Primary, Width(120)))
                    {
                        _showFirstRunTips = false;
                        Main.Settings.firstRun = false;
                        Main.Settings.lastVersion = VersionManager.GetFullVersionString();
                        Main.Settings.lastUpgradeMessageSeen_106_beta5 = "1.0.6_beta5";
                        if (Main.Mod != null) Main.Settings.Save(Main.Mod);
                        if (!_showUpgradeTips)
                        {
                            Destroy(gameObject);
                            _instance = null;
                        }
                    }
                }
                End();
            }
            End();
            GUI.DragWindow();
        }

        private void DrawUpgradeWindow(int windowID)
        {
            var sizes = _sizesHolder.Begin();
            Begin(ContainerDirection.Vertical, ContainerStyle.Padding);
            {
                Space(10);
                Text(Localization.Get(_upgradeMessageKey), TextStyle.Normal, WidthMax);
                FlexibleSpace();
                Begin(ContainerDirection.Horizontal, sizes: sizes, options: WidthMax);
                {
                    Fill();
                    if (Button(Localization.Get("Understand"), ButtonStyle.Primary, Width(120)))
                    {
                        _showUpgradeTips = false;
                        Main.Settings.lastVersion = VersionManager.GetFullVersionString();
                        Main.Settings.lastUpgradeMessageSeen_106_beta5 = "1.0.6_beta5";
                        if (Main.Mod != null) Main.Settings.Save(Main.Mod);
                        Destroy(gameObject);
                        _instance = null;
                    }
                }
                End();
            }
            End();
            GUI.DragWindow();
        }

        private static void FlexibleSpace()
        {
            GUILayout.FlexibleSpace();
        }
    }
}
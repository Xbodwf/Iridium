using UnityEngine;
using static Iridium.UI.IridiumLayout;

namespace Iridium.UI
{
    public class VRAMNotificationUI : MonoBehaviour
    {
        private static VRAMNotificationUI? _instance;
        private string _message = "";
        private float _timer = 0f;
        private const float FadeDuration = 0.5f;
        private const float DisplayDuration = 2.5f;
        private SizesGroup.Holder _sizesHolder = new();

        public static void Show(string message)
        {
            if (_instance == null)
            {
                var go = new GameObject("Iridium_VRAMNotification");
                _instance = go.AddComponent<VRAMNotificationUI>();
                DontDestroyOnLoad(go);
            }
            _instance._message = message;
            _instance._timer = FadeDuration + DisplayDuration + FadeDuration;
        }

        private void OnGUI()
        {
            if (_timer <= 0f) return;

            EnsureTexturesAlive();

            float alpha = 1f;
            if (_timer > DisplayDuration + FadeDuration)
                alpha = (FadeDuration + DisplayDuration + FadeDuration - _timer) / FadeDuration;
            else if (_timer < FadeDuration)
                alpha = _timer / FadeDuration;

            GUI.color = new Color(1f, 1f, 1f, alpha);

            var sizes = _sizesHolder.Begin();
            GUILayout.BeginArea(new Rect(20, 20, 280, 50));
            {
                Begin(ContainerDirection.Horizontal, ContainerStyle.Background, sizes: sizes, options: WidthMax);
                {
                    Icon(IconStyle.Success);
                    Text(_message, TextStyle.Normal, WidthMax);
                }
                End();
            }
            GUILayout.EndArea();

            GUI.color = Color.white;
        }

        private void Update()
        {
            if (_timer > 0f) _timer -= Time.deltaTime;
        }
    }
}
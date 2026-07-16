using BepInEx;
using UnityEngine;

namespace Iridium
{
    [BepInPlugin(BuildInfo.ModName, BuildInfo.ModName, BuildInfo.ModVersion)]
    [BepInProcess("A Dance of Fire and Ice.exe")]
    public class IridiumBepInPlugin : BaseUnityPlugin
    {
        private BepInHandler? _handler;

        private void Awake()
        {
            _handler = new BepInHandler(Logger);
            // This assembly targets the Mono BepInEx installation.
            Main.Initialize(_handler, new Iridium.Runtime.MonoRuntimeHost());
            _handler.TriggerToggle(true);
        }

        private void Update()
        {
            _handler?.TriggerUpdate(Time.deltaTime);
        }

        private void OnGUI()
        {
            _handler?.TriggerGUI();
        }
    }
}

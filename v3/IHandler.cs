using System;

namespace Iridium
{
    public interface IHandler
    {
        string ModId { get; }
        string ModVersion { get; }
        string ModPath { get; }

        void Log(string message);
        void Warning(string message);
        void Error(string message);

        T LoadSettings<T>() where T : class, new();
        void SaveSettings<T>(T settings) where T : class;

        float UIScale { get; }

        event Action<float> OnUpdate;
        event Action<bool> OnToggle;
        event Action OnGUI;
        event Action OnSaveGUI;
    }
}

namespace Iridium.UI
{
    /// <summary>
    /// Frontline entry points for first-run and upgrade dialogs. The real
    /// implementation lives in <see cref="ImlWindow"/>; this class is kept as
    /// a thin forwarding shell so call sites in <c>Main.cs</c> don't need to
    /// change. <c>main</c> (v2.9.8) keeps its own IMGUI-based MainWindow.
    /// </summary>
    public static class MainWindow
    {
        public static void ShowFirstRun() => ImlWindow.ShowFirstRun();

        public static void ShowUpgrade(string messageKey) => ImlWindow.ShowUpgrade(messageKey);
    }
}

using System;
using VolumeControl.Log;

namespace VolumeControl.LocalizationUtil
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            FLog.Initialize("LocaleUtil.log", (Log.Enum.EventType)95);

            var app = new App();
            var mainWindow = new MainWindow();

            return app.Run(mainWindow);
        }
    }
}

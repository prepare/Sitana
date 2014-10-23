using System;
using Sitana.Framework.Content;
using Sitana.Framework.Ui.Core;
using Sitana.Framework;
using System.Resources;
using System.Reflection;
using System.Windows.Forms;

namespace GameEditor
{
    public static class Program
    {
        static AppMain _appMain;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (_appMain = new AppMain())
            {
                Form form = (Form)Form.FromHandle(_appMain.Window.Handle);

                ContentLoader.Init(_appMain.Services, Assembly.GetExecutingAssembly(), "GameEditor.Assets");

                StylesManager.Instance.LoadStyles("AppStyles", true);
                _appMain.LoadView("MainView");

                _appMain.Window.AllowUserResizing = true;

                _appMain.Graphics.IsFullScreen = false;
                _appMain.CanClose = (a) =>
                    {
                        if (!MainController.Current.CanClose)
                        {
                            // This makes the game remain active after close button is pressed.
                            form.Hide();
                            form.Show();

                            MainController.Current.Exit();
                        }

                        return MainController.Current.CanClose;
                    };

                if (EditorSettings.Instance.WindowWidth != 0 || EditorSettings.Instance.WindowHeight != 0)
                {
                    _appMain.Graphics.PreferredBackBufferWidth = EditorSettings.Instance.WindowWidth;
                    _appMain.Graphics.PreferredBackBufferHeight = EditorSettings.Instance.WindowHeight;
                }

                _appMain.IsMouseVisible = true;
                _appMain.OnLoadContent += MainController.OnLoadContent;

                _appMain.OnLoadedView += (a) =>
                {
                    if (EditorSettings.Instance.Fullscreen)
                    {
                        form.WindowState = FormWindowState.Maximized;
                    }
                    else if (EditorSettings.Instance.WindowWidth == 0 || EditorSettings.Instance.WindowHeight == 0)
                    {
                        a.ResizeToView();
                    }
                };

                _appMain.Resized += (w, h) =>
                {
                    if (form.WindowState == FormWindowState.Normal)
                    {
                        EditorSettings.Instance.WindowWidth = w;
                        EditorSettings.Instance.WindowHeight = h;
                    }

                    EditorSettings.Instance.Fullscreen = form.WindowState == FormWindowState.Maximized;
                };

                _appMain.InactiveSleepTime = TimeSpan.FromSeconds(1);

                _appMain.Run();

                EditorSettings.Instance.Serialize();
            }
        }
    }
}
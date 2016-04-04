﻿// SITANA - Copyright (C) The Sitana Team.
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.
using Microsoft.Xna.Framework;
using Sitana.Framework.Diagnostics;
using Sitana.Framework.Input.TouchPad;
using UIKit;
using System;

namespace Sitana.Framework.Ui.Core
{
    public partial class AppMain
    {
        void PlatformInit()
        {
			var view = Services.GetService<UIViewController>().View;
			view.Window.AddGestureRecognizer(new TouchPad_iOSGestureRecognizer(view));

            UiUnit.PixelsPerMm = UIScreen.MainScreen.Scale * 160.0 / 25.4;
        }
        
        protected virtual void OnSize(int width, int height)
        {
            var rect = new Rectangle(0, 0, width, height);
            PerformanceProfiler.Instance.ComputeContentRect(ref rect);

            MainView.Bounds = rect;
            if ( Resized != null )
            {
                Resized(rect.Width, rect.Height);
            }
        }

        protected void PlatformUpdate(GameTime gameTime)
        {
        }
    }
}

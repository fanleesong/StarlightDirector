﻿using StarlightDirector.Commanding;
using System;

namespace StarlightDirector.App.UI.Forms {
    partial class FMain {

        private void CmdViewZoomIn_Executed(object sender, EventArgs e) {
            visualizer.Renderer.ZoomIn();
        }

        private void CmdViewZoomOut_Executed(object sender, EventArgs e) {
            visualizer.Renderer.ZoomOut();
        }

        private readonly Command CmdViewZoomIn = CommandManager.CreateCommand();
        private readonly Command CmdViewZoomOut = CommandManager.CreateCommand();

    }
}
﻿using System.Drawing;
using StarlightDirector.Beatmap;
using StarlightDirector.UI.Controls.Previewing;
using StarlightDirector.UI.Rendering.Direct2D;

namespace StarlightDirector.UI.Controls.Rendering {
    internal sealed class HoldRibbonMesh : RibbonMesh {

        public HoldRibbonMesh(D2DRenderContext context, Note startNote, Note endNote, double now)
            : base(context, startNote, endNote, now) {
        }

        protected override void BuildVertices() {
            var startTiming = StartNote.Temporary.HitTiming.TotalSeconds;
            var endTiming = EndNote.Temporary.HitTiming.TotalSeconds;
            var finishPosition = EndNote.Basic.FinishPosition;
            var now = Now;
            var context = RenderContext;

            var xs = new float[JointCount];
            var ys = new float[JointCount];
            var rs = new float[JointCount];

            for (var i = 0; i < JointCount; ++i) {
                var timing = (endTiming - startTiming) / (JointCount - 1) * i + startTiming;
                var startPosition = EndNote.Basic.StartPosition;
                xs[i] = NotesLayerUtils.GetNoteXPosition(context, now, timing, startPosition, finishPosition, true, true);
                ys[i] = NotesLayerUtils.GetNoteYPosition(context, now, timing, true, true);
                rs[i] = NotesLayerUtils.GetNoteRadius(now, timing);
            }

            for (var i = 0; i < JointCount; ++i) {
                var x = xs[i];
                var y = ys[i];
                var r = rs[i];
                var vertex1 = new PointF(x - r, y);
                var vertex2 = new PointF(x + r, y);
                Vertices[i * 2] = vertex1;
                Vertices[i * 2 + 1] = vertex2;
            }
        }

    }
}

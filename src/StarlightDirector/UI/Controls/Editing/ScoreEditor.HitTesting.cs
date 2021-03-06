using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenCGSS.StarlightDirector.Models.Beatmap;
using OpenCGSS.StarlightDirector.Models.Beatmap.Extensions;

namespace OpenCGSS.StarlightDirector.UI.Controls.Editing {
    partial class ScoreEditor {

        public ScoreEditorHitTestResult HitTest(Point location, Control relativeTo) {
            location = relativeTo.PointToScreen(location);
            location = PointToClient(location);
            return HitTest(location);
        }

        public ScoreEditorHitTestResult HitTest(int x, int y, Control relativeTo) {
            return HitTest(new Point(x, y), relativeTo);
        }

        public ScoreEditorHitTestResult HitTest(Point location) {
            return HitTest(location.X, location.Y);
        }

        /// <summary>
        /// Performs a hit test and returns the result.
        /// </summary>
        /// <param name="x">The X coordinate of the hit test point, relative to this control.</param>
        /// <param name="y">The Y coordinate of the hit test point, relative to this control.</param>
        /// <returns>The result of this hit test.</returns>
        public ScoreEditorHitTestResult HitTest(int x, int y) {
            var score = Project?.Project.GetScore(Difficulty);
            if (score == null || !score.HasAnyBar) {
                return ScoreEditorHitTestResult.GetInvalidResult(x, y);
            }

            var barArea = ScoreEditorLayout.GetBarArea(Config, ClientSize);
            if (!barArea.Contains(x, y)) {
                return ScoreEditorHitTestResult.GetInvalidResult(x, y);
            }

            var config = Config;
            ScoreEditorHitRegion hitRegion;
            var relativeX = x - barArea.Left;
            if (relativeX < config.InfoAreaWidth) {
                hitRegion = ScoreEditorHitRegion.InfoArea;
            } else if (relativeX < config.InfoAreaWidth + config.GridNumberAreaWidth - config.NoteRadius) {
                hitRegion = ScoreEditorHitRegion.GridNumberArea;
            } else if (relativeX < config.InfoAreaWidth + config.GridNumberAreaWidth + config.GridAreaWidth + config.NoteRadius) {
                hitRegion = ScoreEditorHitRegion.GridArea;
            } else {
                hitRegion = ScoreEditorHitRegion.SpecialNoteArea;
            }

            var gridArea = ScoreEditorLayout.GetGridArea(config, ClientSize);
            var columnWidth = gridArea.Width / (config.NumberOfColumns - 1);

            var barStartY = (float)ScrollOffsetY;
            if (y > barStartY + config.NoteRadius) {
                return ScoreEditorHitTestResult.GetInvalidResult(x, y);
            }

            var unit = Look.BarLineSpaceUnit;
            var spaceUnitRatio = ScoreEditorLayout.SpaceUnitRadiusRatio;
            foreach (var bar in score.Bars) {
                var numGrids = bar.GetNumberOfGrids();
                var barHeight = numGrids * unit;

                var hitInThisBar = barStartY + config.NoteRadius >= y && y > barStartY - (barHeight - config.NoteRadius);
                if (!hitInThisBar) {
                    // Continue to the next bar.
                    barStartY -= barHeight;
                    continue;
                }

                // Calculate zooming compensation.
                var firstClearDrawnRatio = ScoreEditorLayout.BarZoomRatio.FirstOrDefault(i => unit * i >= config.NoteRadius * spaceUnitRatio);
                if (firstClearDrawnRatio == 0) {
                    firstClearDrawnRatio = numGrids;
                }
                var newUnit = unit * firstClearDrawnRatio;

                // Locate the column.
                // Remember, gridArea is already adjusted.
                var relativeGridX = x - gridArea.Left;
                var testCol = (int)((relativeGridX + config.NoteRadius) / columnWidth);
                if (testCol < 0) {
                    return new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, null, -1, NotePosition.Default);
                }
                var testX = testCol * columnWidth;
                int col;
                if (Math.Abs(relativeGridX - testX) < config.NoteRadius) {
                    col = testCol;
                } else if (Math.Abs(relativeGridX - (testX + columnWidth)) < config.NoteRadius) {
                    col = testCol + 1;
                } else {
                    if (hitRegion == ScoreEditorHitRegion.SpecialNoteArea) {
                        col = -1;
                    } else {
                        return new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, null, -1, NotePosition.Default);
                    }
                }

                // Y position of the hit, relative to start of this bar.
                var relativeY = -(y - barStartY);

                // List the gaming note in current column.
                var possibleNotesInColumn = bar.Notes.Where(n => (int)n.Basic.FinishPosition == col + 1).ToList();

                // Variables for row locating.
                int testRow, row;
                float testY;

                // If no note is hit, follow the traditional algorithm to find if there is any hit on special notes.
                if (possibleNotesInColumn.Count == 0) {
                    // Locate the row.
                    testRow = (int)((relativeY + config.NoteRadius) / newUnit);
                    if (testRow < 0) {
                        break;
                    }
                    testY = testRow * newUnit;
                    if (Math.Abs(relativeY - testY) < config.NoteRadius) {
                        row = testRow;
                    } else if (Math.Abs(relativeY - (testY + newUnit)) < config.NoteRadius) {
                        row = testRow + 1;
                    } else {
                        return new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, null, -1, NotePosition.Default);
                    }
                    row *= firstClearDrawnRatio;

                    // Hit any gaming note?
                    var note = bar.Notes.FirstOrDefault(n => n.Basic.IndexInGrid == row && (int)n.Basic.FinishPosition == col + 1);

                    // Hit any special note?
                    if (note == null && hitRegion == ScoreEditorHitRegion.SpecialNoteArea) {
                        note = bar.Notes.FirstOrDefault(n => n.Helper.IsSpecial && n.Basic.IndexInGrid == row);
                    }

                    var result = new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, note, row, col + 1);
                    return result;
                }

                // Otherwise, use the new algorithm to find a possible note in current column.
                possibleNotesInColumn.Sort(Note.TimingComparison);

                var noteRadius = config.NoteRadius;
                for (var i = 0; i < possibleNotesInColumn.Count; ++i) {
                    var currentNote = possibleNotesInColumn[i];
                    var currentY = unit * currentNote.Basic.IndexInGrid;
                    // Is the Y coordinate inside current note's region?
                    if (currentY - noteRadius <= relativeY && relativeY <= currentY + noteRadius) {
                        // We got a possible match!
                        Note nextNote;
                        float nextY;

                        if (i < possibleNotesInColumn.Count - 1) {
                            // The next note is in the same bar.
                            nextNote = possibleNotesInColumn[i + 1];
                            nextY = unit * nextNote.Basic.IndexInGrid;
                        } else {
                            // The next note is in the next bar, or...
                            var nextBar = bar.GetNextBar();

                            // it is too far away (then ignore it, we found a match).
                            if (nextBar == null || nextBar.Notes.Count == 0) {
                                var result = new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, currentNote, currentNote.Basic.IndexInGrid, col + 1);
                                return result;
                            }

                            var notesOnSameColumnInNextBar = nextBar.Notes.Where(n => (int)n.Basic.FinishPosition == col + 1).ToList();
                            notesOnSameColumnInNextBar.Sort(Note.TimingComparison);
                            nextNote = notesOnSameColumnInNextBar.FirstOrDefault();
                            if (nextNote == null) {
                                // Also too far away. We've found a match.
                                var result = new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, currentNote, currentNote.Basic.IndexInGrid, col + 1);
                                return result;
                            }

                            nextY = barHeight + unit * nextNote.Basic.IndexInGrid;
                        }

                        // Is the Y coordinate not overlapped by the next note (because the notes are drawn from bottom to top)?
                        if (relativeY < nextY - noteRadius) {
                            var result = new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, currentNote, currentNote.Basic.IndexInGrid, col + 1);
                            return result;
                        }
                    }
                }

                // Locate the row. Again.
                testRow = (int)((relativeY + config.NoteRadius) / newUnit);
                if (testRow < 0) {
                    break;
                }
                testY = testRow * newUnit;
                if (Math.Abs(relativeY - testY) < config.NoteRadius) {
                    row = testRow;
                } else if (Math.Abs(relativeY - (testY + newUnit)) < config.NoteRadius) {
                    row = testRow + 1;
                } else {
                    return new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, null, -1, NotePosition.Default);
                }
                row *= firstClearDrawnRatio;

                // Sorry, no can do. Maybe you hit the empty area.
                return new ScoreEditorHitTestResult(new Point(x, y), hitRegion, bar, null, row, col + 1);
            }

            return new ScoreEditorHitTestResult(new Point(x, y), hitRegion, null, null, -1, NotePosition.Default);
        }

    }
}

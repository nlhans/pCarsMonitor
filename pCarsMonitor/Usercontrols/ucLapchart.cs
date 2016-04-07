using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pCarsTelemetry.API;

namespace pCarsMonitor.Usercontrols
{
    public partial class ucLapchart : UserControl
    {
        private Timer updateData = new Timer();
        private DataController data;
        private PcarsTelemetrySample sample;

        private int lineheight = 21;
        private Font f = new Font("Calibri", 12.0f);

        private List<Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>> cols;

        public ucLapchart(DataController datactl)
        {
            data = datactl;
            data.Data += DataOnData;
            InitializeComponent();

            updateData = new Timer { Interval = 750 };
            updateData.Tick += updateData_Tick;
            updateData.Start();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            cols = new List<Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>>();


            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Driver", 200, (driver, g, pt) => g.DrawString("P"+ driver.Participant.mRacePosition + " " + driver.Name, f, Brushes.White, pt)));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Last lap", 100, (driver, g, pt) =>
                {
                    var laps = driver.GetSector("Lap");

                    var lastLap = laps.Any() ? laps.LastOrDefault().Value : -1;

                    if (lastLap > 0)
                        DrawLaptime(lastLap, Brushes.GreenYellow, g, pt);
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Best lap", 100, (driver, g, pt) =>
                {
                    var laps = driver.GetSector("Lap");

                    var bestLap = laps.Any() ? laps.Min(x => x.Value) : -1;

                    if (bestLap > 0)
                        DrawLaptime(bestLap, Brushes.GreenYellow, g, pt);
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Last S1", 70, (driver, g, pt) =>
                {
                    var sTimes = driver.GetSector("S1");

                    if (!sTimes.Any())
                        g.DrawString("??", f, Brushes.Red, pt);
                    else
                    {
                        var lastS1 = sTimes.LastOrDefault().Value;
                        DrawSector(lastS1, Brushes.GreenYellow, g, pt);
                    }
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Best S1", 70, (driver, g, pt) =>
                {
                    var sTimes = driver.GetSector("S1");

                    if (!sTimes.Any())
                        g.DrawString("??", f, Brushes.Red, pt);
                    else
                    {
                        var lastS1 = sTimes.Min(x=>x.Value);
                        DrawSector(lastS1, Brushes.GreenYellow, g, pt);
                    }
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Last S2", 70, (driver, g, pt) =>
                {
                    var sTimes = driver.GetSector("S2");

                    if (!sTimes.Any())
                        g.DrawString("??", f, Brushes.Red, pt);
                    else
                    {
                        var lastS1 = sTimes.LastOrDefault().Value;
                        DrawSector(lastS1, Brushes.GreenYellow, g, pt);
                    }
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Best S2", 70, (driver, g, pt) =>
                {
                    var sTimes = driver.GetSector("S2");

                    if (!sTimes.Any())
                        g.DrawString("??", f, Brushes.Red, pt);
                    else
                    {
                        var lastS1 = sTimes.Min(x => x.Value);
                        DrawSector(lastS1, Brushes.GreenYellow, g, pt);
                    }
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Last S3", 70, (driver, g, pt) =>
                {
                    var sTimes = driver.GetSector("S3");

                    if (!sTimes.Any())
                        g.DrawString("??", f, Brushes.Red, pt);
                    else
                    {
                        var lastS1 = sTimes.LastOrDefault().Value;
                        DrawSector(lastS1, Brushes.GreenYellow, g, pt);
                    }
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Best S3", 70, (driver, g, pt) =>
                {
                    var sTimes = driver.GetSector("S3");

                    if (!sTimes.Any())
                        g.DrawString("??", f, Brushes.Red, pt);
                    else
                    {
                        var lastS1 = sTimes.Min(x => x.Value);
                        DrawSector(lastS1, Brushes.GreenYellow, g, pt);
                    }
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("State", 50, (driver, g, pt) =>
                {
                    g.DrawString(driver.State.ToString(), f, Brushes.White, pt);
                }));
            cols.Add(new Tuple<string, int, Action<PcarsExtraParticipant, Graphics, PointF>>
                ("Location", 50, (driver, g, pt) =>
                {
                    g.DrawString(driver.Location.ToString(), f, Brushes.White, pt);
                }));
        }

        private void DataOnData(PcarsTelemetrySample data, PcarsExtraData myData)
        {
            sample = data;
        }

        private void DrawSector(float time, Brush br, Graphics g, PointF pt)
        {
            var min = (int)Math.Floor(time / 60.0);
            time -= min * 60;

            var str = min > 0 ? string.Format("{0:00}:{1:00.000}", min, time)
                : string.Format("{0:00.000}", time);

            g.DrawString(str, f, br, pt);
        }

        private void DrawLaptime(float time, Brush br, Graphics g, PointF pt)
        {
            var min = (int)Math.Floor(time/60.0);
            time -= min*60;

            var str = string.Format("{0:00}:{1:00.000}", min, time);

            g.DrawString(str, f, br, pt);
        }

        private void updateData_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                // Make a list of ALL sectors of the track.
                // And list surrounding players on the columns (up to 5?)

                var g = e.Graphics;
                g.FillRectangle(Brushes.Black, e.ClipRectangle);

                int x = 5;
                int y = 5;
                // Draw header
                foreach (var hdr in cols)
                {
                    g.DrawString(hdr.Item1, f, Brushes.Yellow, x, y);
                    x += hdr.Item2;
                }

                var selPlayers = sample.mParticipantData.Take(sample.mNumParticipants).OrderBy(z => z.mRacePosition);

                foreach (var drv in selPlayers)
                {
                    var part = data.ExtraData.LookupParticipant(drv);
                    x = 5;
                    y += lineheight;

                    foreach (var col in cols)
                    {
                        col.Item3(part, g, new PointF(x, y));
                        x += col.Item2;
                    }
                }
            }
            catch
            {
                
            }
        }
    }
}

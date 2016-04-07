using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pCarsMonitor.Entities;
using pCarsTelemetry.API;

namespace pCarsMonitor.Usercontrols
{
    public partial class ucTimingchart : UserControl
    {
        private PcarsTelemetrySample telemSample;
        private Timer updateData = new Timer();

        private int colKeyWidth = 200;
        private int colDriverWidth = 180;
        private int lineheight = 21;

        private DataController data;

        public ucTimingchart(DataController datactl)
        {
            data = datactl;
            data.Data += data_Data;
            InitializeComponent();
            updateData = new Timer{Interval=750};
            updateData.Tick += updateData_Tick;
            updateData.Start();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        private void data_Data(PcarsTelemetrySample data, PcarsExtraData myData)
        {
            telemSample = data;
        }

        void updateData_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Make a list of ALL sectors of the track.
            // And list surrounding players on the columns (up to 5?)

            var g = e.Graphics;
            g.FillRectangle(Brushes.Black, e.ClipRectangle);

            try
            {
                if (data.TrackLoaded == null || data.TrackLoaded.Sections == null)
                    return;

                // Is there any driver higher than me?
                var me = data.ExtraData.Me;
                if (me == null)
                    return;

                var f = new Font("Calibri", 14.0f);
                var fb = new Font("Calibri", 16.0f, FontStyle.Bold);

                var sections = data.TrackLoaded.Sections;
                var col = -1;
                g.DrawString("Sector", fb, Brushes.White, 5, 5);
                var lastType = TrackSectionType.Filler;

                foreach (var sector in sections)
                {
                    if (sector.SectionType != lastType)
                        col++;
                    lastType = sector.SectionType;
                    col++;
                    g.DrawString(sector.Name, f, Brushes.White, 5, 5 + col*lineheight);
                }

                var myPos = me.Participant.mRacePosition;

                var overallBest = new Dictionary<string, float>();

                foreach (var sector in sections)
                {
                    overallBest.Add(sector.Name, float.MaxValue);

                    foreach (var part in data.ExtraData.Participants)
                    {
                        if (part.TimingBest.ContainsKey(sector.Name))
                        {
                            if (overallBest[sector.Name] >= part.TimingBest[sector.Name])
                            {
                                overallBest[sector.Name] = part.TimingBest[sector.Name];
                            }
                        }
                    }
                }

                // Display top 3
                for (int pos = 1; pos < 4; pos++)
                {
                    var player = data.ExtraData.Participants.FirstOrDefault(x => x.Participant.mRacePosition == pos);
                    if (player != null)
                        DisplayColumn(g, overallBest, sections, player, me, pos);
                }

                // IF I am in 1-3; display a 5th player
                if (myPos <= 3)
                {
                    var player = data.ExtraData.Participants.FirstOrDefault(x => x.Participant.mRacePosition == 4);
                    if (player != null)
                        DisplayColumn(g, overallBest, sections, player, me, 4);
                }
                else
                {
                    DisplayColumn(g, overallBest, sections, me, me, 4);
                }

            }
            catch
            {
                
            }

            base.OnPaint(e);
        }

        private void DisplayColumn(Graphics g, Dictionary<string, float> overallBest, IEnumerable<TrackSection> sections, PcarsExtraParticipant player, PcarsExtraParticipant split, int pos)
        {
            var f = new Font("Calibri", 14.0f);
            var fb = new Font("Calibri", 14.0f, FontStyle.Underline);

            var currentLap = (int)player.Participant.mCurrentLap;
            var lastLap = (int)player.Participant.mCurrentLap - 1;
            var col = -1;
            var x = colKeyWidth + (pos - 1)*colDriverWidth;

            // draw name as header
            var nameBrush = (player == split) ? Brushes.DeepPink : Brushes.Yellow;
            Font fh;
            if (player.State == PcarsState.Pits || player.State == PcarsState.Outlap)
                fh = new Font("Calibri", 16.0f, FontStyle.Bold | FontStyle.Underline);
            else 
                fh = new Font("Calibri", 16.0f, FontStyle.Bold);

            g.DrawString("P"+player.Participant.mRacePosition + " " + player.Name, fh, nameBrush, x, 5);

            var lastType = TrackSectionType.Filler;

            foreach (var sector in sections)
            {
                if (sector.SectionType != lastType)
                    col++;
                lastType = sector.SectionType;
                col++;
                var y = 5 + col*lineheight;


                if (player.Timing.ContainsKey(sector.Name) &&
                    player.TimingBest.ContainsKey(sector.Name))
                {
                    // OK, there are 4 different modes:
                    // 0) If player in pits draw all it's best times and a split to it's best lap
                    // 1) Drawing current lap if player after region
                    // 2) Drawing split as bold if player is in regoin
                    // 3) Drawing last lap if player is behind region
                    var playerPos = player.Participant.mCurrentLapDistance;

                    var splitTime = -1.0;
                    var brush = Brushes.White;
                    var drawBold = false;

                    var bestSplitForPlayer = (player.TimingBest.ContainsKey(sector.Name)
                        ? player.TimingBest[sector.Name]
                        : float.MaxValue);

                    if (player.State == PcarsState.Pits || player.State == PcarsState.Outlap)
                    {
                        splitTime = bestSplitForPlayer;
                        brush = Brushes.Yellow;
                        drawBold = false;
                    } else
                    if (sector.Start <= playerPos && sector.End >= playerPos) // in this sector
                    {
                        if (player.Timing[sector.Name].ContainsKey(lastLap))
                            splitTime = player.Timing[sector.Name][lastLap];

                        brush = Brushes.Yellow;
                        drawBold = true;
                    }else if (sector.End >= playerPos) // draw last lap
                    {
                        if (player.Timing[sector.Name].ContainsKey(lastLap))
                            splitTime = player.Timing[sector.Name][lastLap];

                        brush = Brushes.Yellow;
                    }
                    else if (sector.End <= playerPos) // draw this lap
                    {
                        if (player.Timing[sector.Name].ContainsKey(currentLap))
                            splitTime = player.Timing[sector.Name][currentLap];

                        brush = Brushes.White;
                    }

                    // Is this split time it's best time?
                    if (splitTime <= bestSplitForPlayer)
                        brush = Brushes.GreenYellow;
                    if (splitTime <= overallBest[sector.Name] && overallBest[sector.Name] < 10000)
                        brush = Brushes.DeepPink;

                    if (splitTime > 0 && splitTime < 10000)
                    {
                        if (sector.Name.StartsWith("Speedtrap"))
                        {
                            var distance = sector.End - sector.Start;
                            var spd = Math.Round(distance/splitTime*3.6, 1).ToString() + "kmh";
                            g.DrawString(spd, (drawBold ? fb : f), brush, x, y);
                        }
                        else
                        {
                            var splitStr = Math.Round(splitTime, 2).ToString();
                            g.DrawString(splitStr, (drawBold ? fb : f), brush, x, y);

                            // Draw a delta time
                            if (overallBest[sector.Name] < 10000)
                            {
                                var delta = Math.Round(splitTime - overallBest[sector.Name], 2).ToString();
                                g.DrawString(delta, (drawBold ? fb : f), Brushes.White, x + 50, y);
                            }
                        }
                    }
                }

            }
        }

    }
}

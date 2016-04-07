using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pCarsMonitor.Entities;
using pCarsTelemetry.API;

namespace pCarsMonitor.Usercontrols
{
    public enum TrackMapDisplay
    {
        Corners,
        Sectors,
        Regions,
        Blank
    }

    public partial class ucTrackmap : UserControl
    {
        private DataController data;
        private PcarsExtraData sampleExtra;
        private PcarsTelemetrySample sample;
        private bool sampleNone = true;

        private float worldX_min;
        private float worldX_max;
        private float worldY_min;
        private float worldY_max;

        private int displayX_min;
        private int displayX_max;
        private int displayY_min;
        private int displayY_max;

        private float scale;
        private float xOff;
        private float yOff;

        public TrackMapDisplay Display { get; private set; }
        private Bitmap _BackgroundTrackMap = new Bitmap(1,1);

        public bool Flip { get; private set; }

        public ucTrackmap(DataController dataCtl)
        {
            data = dataCtl;
            Display = TrackMapDisplay.Sectors;

            dataCtl.Data += dataCtl_Data;
            dataCtl.TrackChange += dataCtl_TrackChange;
            this.Layout += ucTrackmap_Layout;

            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        void ucTrackmap_Layout(object sender, LayoutEventArgs e)
        {
            displayX_min = 25;
            displayX_max = this.Width - 50;

            displayY_min = 25;
            displayY_max = this.Height - 50;

            dataCtl_TrackChange(sender, e);
        }

        private void dataCtl_TrackChange(object sender, EventArgs e)
        {
            if (data.TrackLoaded == null)
                return;

            // Render new background
            var tpts = data.TrackLoaded.Points;
            if (!tpts.Any())
                return;

            worldX_min = tpts.Min(x => x.X);
            worldX_max = tpts.Max(x => x.X);

            worldY_min = tpts.Min(x => x.Z);
            worldY_max = tpts.Max(x => x.Z);

            var bWidth = displayX_max - displayX_min;
            var bHeight = displayY_max - displayY_min;

            Bitmap b = new Bitmap(bWidth + 25, bHeight + 25);
            using (var g = Graphics.FromImage(b))
            {
                g.CompositingQuality =CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                ;
                // Calculate world scaling
                var xRange = worldX_max - worldX_min;
                var yRange = worldY_max - worldY_min;

                var xScale = bWidth/xRange;
                var yScale = bHeight/yRange;

                if (xScale/yScale > 2)
                {
                    xScale = b.Height/xRange;
                    yScale = b.Width/yRange;
                    Flip = true;
                }
                else
                    Flip = false;

                // Take the lowest scale, because otherwise we will draw out of proportion
                scale = Math.Min(xScale, yScale);
                xOff = -(Flip ? worldY_min : 0)*scale;
                yOff = -(Flip ? worldX_min : worldY_min)*scale;

                // What type of sections are we displaying?
                List<TrackSection> sections = data.TrackLoaded.Sections.Where(FilterByType).OrderBy(x => x.Start).ToList();

                int lastMeter = 0;

                foreach (var section in new List<TrackSection> (sections))
                {
                    if (section.Start != lastMeter + 1)
                    {
                        // Add a new sector that bridges lastMeter > this start
                        sections.Add(new TrackSection(string.Empty, TrackSectionType.Filler, 
                            lastMeter + 1,
                            section.Start - 1, Color.FromArgb(0x2A, 0x9E, 0x90)));
                    }
                    lastMeter = section.End;
                }
                var trackLength = (int)Math.Round(tpts.Max(x => x.Meter));
                if (lastMeter < trackLength)
                {
                        sections.Add(new TrackSection(string.Empty, TrackSectionType.Filler, 
                            lastMeter + 1,
                            trackLength, Color.FromArgb(0x2A, 0x9E, 0x90)));
                }

                // Render each section in it's own color!
                foreach (var section in sections)
                {
                    var pts = tpts.Where(x => x.Meter >= section.Start && x.Meter <= section.End);

                    foreach (var pt in pts)
                    {
                        var pnt = new PointF(GetX(pt.X, pt.Z), GetY(pt.X, pt.Z));
                        var brsh = new SolidBrush(section.Color);
                        g.FillEllipse(brsh, pnt.X, pnt.Y, 10, 10);
                    }
                }
            }

            lock (_BackgroundTrackMap)
            {
                _BackgroundTrackMap = b;
            }
        }

        private bool FilterByType(TrackSection arg)
        {
            switch (this.Display)
            {
                case TrackMapDisplay.Blank:
                    return false;

                case TrackMapDisplay.Corners:
                    return arg.SectionType == TrackSectionType.Corner;

                case TrackMapDisplay.Regions:
                    return arg.SectionType == TrackSectionType.Region;

                case TrackMapDisplay.Sectors:
                    return arg.SectionType == TrackSectionType.Sector;

                default:
                    return false;
            }
        }

        private void dataCtl_Data(PcarsTelemetrySample data,PcarsExtraData extraData)
        {
            sample = data;
            sampleExtra = extraData;

            sampleNone = false;
            this.Invalidate();
        }

        private float GetX(float worldX, float worldY)
        {
            if (Flip)
                return xOff + scale * worldY;
            else
                return xOff + scale*(worldX_max- worldX);
        }

        private float GetY(float worldX, float worldY)
        {
            if (Flip)
                return yOff + scale * worldX;
            else
            return yOff + scale * worldY;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                g.FillRectangle(Brushes.Black, 0, 0, this.Width, this.Height);
                if (_BackgroundTrackMap != null)
                {
                    lock (_BackgroundTrackMap)
                    {
                        CompositingMode compMode = g.CompositingMode;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.DrawImage(_BackgroundTrackMap, 25, 25);
                        g.CompositingMode = compMode;

                    }
                }
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (sampleNone)
                    return;

                var f = new Font("Arial", 12f);
                var ft = new Font("Arial", 7f);

                var myPosition = sampleExtra.Me.Participant.mRacePosition;
                var myLap = sampleExtra.Me.Participant.mCurrentLap;
                var myMeters = sampleExtra.Me.Participant.mCurrentLapDistance;
                g.DrawString(myMeters.ToString("00000m"), f, Brushes.White, 10, 10);

                float bubblesize = 34f;
                // get all drivers and draw a dot!
                for (int k = 0; k < sample.mNumParticipants; k++)
                {
                    var driver = sample.mParticipantData[k];
                    var x = driver.mWorldPosition[0];
                    var y = driver.mWorldPosition[2];

                    // Lookup the "extra" participant
                    // This contains "extra" info we calculated ourselves.
                    var extra = sampleExtra.LookupParticipant(driver);
                    
                    if (extra != null && driver.mRacePosition != 0 && driver.mRacePosition <= 120 && Math.Abs(x) >= 0.1)
                    {
                        float a1 =25+ GetX(x,y);
                        float a2 = 25 + GetY(x,y);

                        double arrowSize = bubblesize / 2;
                        double arrowAngle = 50.0f / 180.0f * Math.PI;
                        double heading_angle = extra.Heading;
                        if (Flip)
                            heading_angle -= Math.PI/2;
                        else
                            heading_angle = Math.PI+ heading_angle;
                        //heading_angle =  driver.Heading*Math.PI/2;
                        PointF[] arrow = new PointF[3];
                        arrow[0] = new PointF(Convert.ToSingle(a1 + Math.Sin(heading_angle) * (arrowSize + 10)), Convert.ToSingle(a2 + Math.Cos(heading_angle) * (arrowSize + 10)));
                        arrow[1] = new PointF(Convert.ToSingle(a1 + Math.Sin(heading_angle + arrowAngle) * arrowSize), Convert.ToSingle(a2 + Math.Cos(heading_angle + arrowAngle) * arrowSize));
                        arrow[2] = new PointF(Convert.ToSingle(a1 + Math.Sin(heading_angle - arrowAngle) * arrowSize), Convert.ToSingle(a2 + Math.Cos(heading_angle - arrowAngle) * arrowSize));

                        g.FillPolygon(Brushes.White, arrow, FillMode.Winding); ;


                        Brush c;
                        if (driver.mRacePosition == myPosition) // Player
                            c = Brushes.Magenta;
                        // TODO: Lookup speed
                        else if (extra.Speed < 35) // Stopped
                            c = Brushes.Red;
                        else if (driver.mCurrentLap != myLap) // TODO: need more logic to truly show drivers that are 1 lap down.
                            // InRace && lapped vehicle
                            c = new SolidBrush(Color.FromArgb(80, 80, 80));
                        else if (driver.mRacePosition > myPosition) // In front of player.
                            c = Brushes.YellowGreen;
                        else // Behind player, but not lapped.
                            c = new SolidBrush(Color.FromArgb(90, 120, 120));

                        a1 -= bubblesize/2f;
                        a2 -= bubblesize/2f;

                        g.FillEllipse(c, a1, a2, bubblesize, bubblesize);
                        g.DrawEllipse(new Pen(Color.White, 1f), a1, a2, bubblesize, bubblesize);

                        g.DrawString(driver.mRacePosition.ToString(), f, Brushes.White, a1 + 5, a2 + 2);
                        g.DrawString(extra.Speed.ToString("000"), ft, Brushes.White, a1+5, a2+20);
                    }
                }

            }
            catch (Exception ex)
            {
                Graphics g = e.Graphics;
                g.FillRectangle(Brushes.Black, 0, 0, this.Width, this.Height);

                Font f = new Font("Arial", 10f);

                g.DrawString(ex.Message, f, Brushes.White, 10, 10);
                g.DrawString(ex.StackTrace, f, Brushes.White, 10, 40);

            }
            //base.OnPaint(e);
        }
    }
}

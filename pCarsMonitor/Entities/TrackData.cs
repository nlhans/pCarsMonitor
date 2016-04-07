using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using System.Xml.Schema;
using pCarsTelemetry.API;

namespace pCarsMonitor.Entities
{
    public enum TrackRecordState
    {
        Off,
        WaitingForStart,
        Recording,
        Saving,
        Done
    }

    public class TrackData : IDataConsumer
    {
        public string Venue { get; private set; }

        public bool RecordMode
        {
            get { return state != TrackRecordState.Off; }
        }

        private TrackRecordState state = TrackRecordState.Off;

        public bool Recording
        {
            get { return state == TrackRecordState.Recording; }
        }

        public bool RecordingDone
        {
            get { return state == TrackRecordState.Done; }
        }

        private DateTime RecordingStart { get; set; }

        private float LastMeter { get; set; }

        public Dictionary<string, string> Keys;

        public List<TrackSection> Sections;
        public List<TrackPoint> Points { get; private set; } 

        public TrackData(string track, string loadFile)
        {
            Venue = track;

            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Points = new List<TrackPoint>();
            Sections = new List<TrackSection>();
            Keys = new Dictionary<string, string>();

            var lines = File.ReadAllLines(loadFile);

            TrackFileMode mode = TrackFileMode.Init;
            bool valid = false;
            foreach (var line in lines)
            {
                // Skip empty lines
                if (line.Trim() == string.Empty)
                    continue;

                // Switch mode on magic lines
                switch (line)
                {
                    case "sections":
                        mode = TrackFileMode.Sections;
                        continue;

                    case "points":
                        mode = TrackFileMode.Points;
                        continue;

                    case "header":
                        mode = TrackFileMode.Keys;
                        continue;
                }

                // Parse line according to mode
                switch (mode)
                {
                    case TrackFileMode.Init:
                        Console.WriteLine("Parsing line in INIT mode");
                        break;

                    case TrackFileMode.Keys:
                        var kvp = line.Split("=".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
                        if (kvp.Length != 2) continue;
                        Keys.Add(kvp[0], kvp[1]);
                        break;

                    case TrackFileMode.Points:
                        var tp = new TrackPoint(line, out valid);
                        if (valid) Points.Add(tp);
                        break;

                    case TrackFileMode.Sections:
                        var ts = new TrackSection(line, out valid);
                        if (valid) Sections.Add(ts);
                        break;
                }

            }

            Sections.Add(new TrackSection("Lap", TrackSectionType.Lap, 0, (int)Math.Floor(Points.Max(x => x.Meter)), Color.Khaki));

            Console.WriteLine("Found " + Keys.Count + " keys");
            foreach (var kvp in Keys)
                Console.WriteLine(kvp.Key + "=" + kvp.Value);

            Console.WriteLine("Found " + Points.Count + " points");
            Console.WriteLine("Found " + Sections.Count + " sections");

            foreach(var sect in Sections)
                Console.WriteLine("Section " + sect.Name + " " + sect.Start + "m to " + sect.End + "m");

        }

        public TrackData(string track)
        {
            Venue = track;
            state = TrackRecordState.WaitingForStart;
            LastMeter = -1000;

            Points = new List<TrackPoint>();
            Sections = new List<TrackSection>();
            Keys = new Dictionary<string, string>();
        }

        public void Save(string file)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var buffer = new List<string>();

            buffer.Add("header");
            buffer.AddRange(Keys.Select(kvp => kvp.Key + "=" + kvp.Value));
            buffer.Add("");
            buffer.Add("points");
            buffer.AddRange(Points.Select(pt => pt.Save()));
            buffer.Add("");
            buffer.Add("sections");
            buffer.AddRange(Sections.Where(x=>x.SectionType != TrackSectionType.Filler && x.SectionType != TrackSectionType.Lap ).Select(st => st.Save()));

            File.WriteAllLines(file, buffer);
        }


        public void Push(PcarsTelemetrySample telemetrySample)
        {
            var me = telemetrySample.mParticipantData.FirstOrDefault();

            bool RecordThis = false;
            switch (state)
            {
                case TrackRecordState.Off:
                    // Dont do anything!
                    break;

                case TrackRecordState.WaitingForStart:
                    if (me.mCurrentLapDistance < 10 && LastMeter > 500)
                    {
                        // We're starting the benchmark lap
                        state = TrackRecordState.Recording;
                        RecordThis = true;

                        Console.WriteLine("Starting recording!");
                        Points.Clear();

                        RecordingStart = DateTime.Now;
                    }
                    break;

                case TrackRecordState.Recording:

                    if (LastMeter > 500 && me.mCurrentLapDistance < 10)
                    {
                        Console.WriteLine("Done recording!");
                        state = TrackRecordState.Saving;
                    }
                    else
                    {
                        RecordThis = true;
                    }
                    break;

                case TrackRecordState.Saving:
                    var length = Points.Max(x => x.Meter);

                    var recordingMs = DateTime.Now.Subtract(RecordingStart).TotalMilliseconds;
                    var avgSpeed = length / (recordingMs / 1000.0f) * 3.6;

                    var resolution = Math.Round(length / Points.Count, 2);

                    Console.WriteLine("Track distance: " + length);
                    Console.WriteLine("Points: " + Points.Count);

                    Console.WriteLine("Average speed: " + Math.Round(avgSpeed,2) + "km/h");
                    Console.WriteLine("Resolution: " + Math.Round(resolution,3 ));

                    // Add information to annotate the file
                    Keys.Clear();
                    Keys.Add("Points", Points.Count.ToString());
                    Keys.Add("Length", Math.Round(length,1).ToString());
                    Keys.Add("Speed", Math.Round(avgSpeed,2).ToString());
                    Keys.Add("Resolution", Math.Round(resolution,3).ToString());
                    Keys.Add("Date", DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());

                    this.Save("tracks/" + telemetrySample.mTrackLocation + " " + telemetrySample.mTrackVariation + ".pcars-track");
                    state = TrackRecordState.Done;
                    break;
            }

            LastMeter = me.mCurrentLapDistance;

            if (RecordThis)
            {
                if (!Points.Any(x => Math.Abs(x.Meter - me.mCurrentLapDistance) < 0.5))
                {
                    Points.Add(new TrackPoint(me.mCurrentLapDistance, me.mWorldPosition));
                }
            }
        }
    }
}

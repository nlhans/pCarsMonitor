using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using pCarsMonitor.Entities;
using pCarsTelemetry.API;

namespace pCarsMonitor
{
    public delegate void TelemetryDelegate(PcarsTelemetrySample data,PcarsExtraData myData);

    public class DataController
    {
        public TrackData TrackLoaded { get; set; }

        public PcarsExtraData ExtraData { get; private set; }
        public event EventHandler TrackChange;

        public event TelemetryDelegate Data;
        public SharedMemory<PcarsTelemetrySample> telemetry;
        private Timer telemetryTimer;


        public DataController()
        {
            telemetry = new SharedMemory<PcarsTelemetrySample>("$pcars$");

            ExtraData = new PcarsExtraData();

            telemetryTimer = new Timer();
            telemetryTimer.Tick += telemetryTimer_Tick;
            telemetryTimer.Interval = 1; // TODO: use multimedia timer
            telemetryTimer.Start();

            // Load track on name
            Data += DataController_Data;

        }

        private void DataController_Data(PcarsTelemetrySample data, PcarsExtraData extraData)
        {
            if (this.TrackLoaded == null)
            {
                // Load new track anyway.. does it exist?
                var trackPath = "tracks/" + data.mTrackLocation +" " +data.mTrackVariation + ".pcars-track";
                if (File.Exists(trackPath))
                {
                    Console.WriteLine("Loading track " + data.mTrackLocation + data.mTrackVariation);
                    TrackLoaded = new TrackData(data.mTrackLocation, trackPath);
                }
                else
                {
                    Console.WriteLine("Loading EMPTY track - will record 1 lap");
                    TrackLoaded = new TrackData(data.mTrackLocation); // empty
                }

                if (TrackChange != null)
                    TrackChange(this, new EventArgs());
            }

            if (TrackLoaded != null && TrackLoaded.RecordingDone)
                TrackLoaded = null;

            // What if the name changes?
            if (TrackLoaded != null && data.mTrackLocation != TrackLoaded.Venue)
            {
                // will load on next iteration
                TrackLoaded = null;
            }
        }

        private void telemetryTimer_Tick(object sender, System.EventArgs e)
        {
            PcarsTelemetrySample sample = telemetry.Read();

            if (TrackLoaded != null)
                TrackLoaded.Push(sample);

            foreach (var part in this.ExtraData.Participants)
            {
                part.TrackSession = this.TrackLoaded;
            }

            // Post-process samples to get:
            // - Each competitors approximate speed
            // - Each competitors approximate heading
            // - TODO: Lap times
            this.ExtraData.Tick(sample);

            if (Data != null)
                Data(sample, ExtraData);
        }
    }
}

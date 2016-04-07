using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms.VisualStyles;
using pCarsMonitor.Entities;
using pCarsTelemetry.API;

namespace pCarsMonitor
{
    public class PcarsExtraParticipant
    {
        public TrackData TrackSession { get; set; }
        public PcarsParticipant Participant { get; set; }
        private PcarsParticipant LastData { get; set; }
        private PcarsTelemetrySample LastSample { get; set; }
        private DateTime LastSampleDate;

        public PcarsLocation Location { get; private set; }
        public PcarsState State { get; private set; }

        // Map <time, meter>
        // Keeps only of current lap
        private Dictionary<int,float> Meter2TimeTable = new Dictionary<int, float>();
        private int lastLap = -1;
        private int lastMeter = -1000;

        // Map <section, <lap, time>>  
        public Dictionary<string, float> TimingBest = new Dictionary<string, float>();
        public Dictionary<string, Dictionary<int, float>> Timing = new Dictionary<string, Dictionary<int, float>>();

        public Dictionary<int, float> GetSector(string sector)
        {
            if (!Timing.ContainsKey(sector))
                return new Dictionary<int, float>();
            else
            {
                return Timing[sector];
            }
        }

        public double Scale;

        public string Name
        {
            get { return Participant.mName; }
        }

        private double UncorrectedSpeed = 0.0;
        public double Speed { get { return UncorrectedSpeed*Scale; } }
        public double Heading { get; private set; }

        public double TopSpeed { get; private set; }

        public PcarsExtraParticipant(PcarsTelemetrySample sample, PcarsParticipant data)
        {
            LastData = data;
            Participant = data;

            LastSample = sample;

        }

        public void Tick(PcarsTelemetrySample newSample, PcarsParticipant newData)
        {
            /** THIS WHOLE JUNK OF CODE PROCESSES SPEED AND HEADING INFORMATION **/
            // unfortunately coordinates x/z do not scale properly to speed, so a scale is applied
            // additionally timestamps are rubbish
            var dx = newData.mWorldPosition[0] - LastData.mWorldPosition[0];
            var dy = newData.mWorldPosition[1] - LastData.mWorldPosition[1];
            var dz = newData.mWorldPosition[2] - LastData.mWorldPosition[2];

            var dt = 0.0;
            var dm = Math.Abs(newData.mCurrentLapDistance - LastData.mCurrentLapDistance); // delta meter

            if (newSample.mEventTimeRemaining > 0)
                dt = Math.Abs(LastSample.mEventTimeRemaining - newSample.mEventTimeRemaining);
            else if (newSample.mCurrentTime > 0)
                dt = Math.Abs(newSample.mCurrentTime - LastSample.mCurrentTime);
            else
            {
                // Damn you Project CARS API with unreliable timestamps.
                dt = DateTime.Now.Subtract(LastSampleDate).TotalSeconds;
            }
            if (dt > .33 && dm > 0)
            {
                var ds = Math.Sqrt(dx*dx + dy*dy + dz*dz); // delta distance
                var spd = ds/dt;

                UncorrectedSpeed = spd;
                if (ds > .5)
                Heading = Math.PI - Math.Atan2(dx, dz);

                LastData = Participant;
                LastSample = newSample;
                LastSampleDate = DateTime.Now;
            }
            Participant = newData;

            /** APPLY SECTOR/CORNER TIMINGS **/
            bool Record = false;
            bool Clear = false;
            bool Save = false;
            var oldState = State;
            var meters = (int)Math.Round(newData.mCurrentLapDistance);
            var lapNow = (int)newData.mCurrentLap;


            // Try to get timestamp
            bool validTime = true;
            float t = 0.0f;
            if (newSample.mEventTimeRemaining > 0)
                t = -newSample.mEventTimeRemaining;
            else if (newSample.mCurrentTime > 0)
                t = newSample.mCurrentTime;
            else
            {
                validTime = false;
            }

            switch (State)
            {
                case PcarsState.Offline:
                    if (Participant.mIsActive == true)
                        State = PcarsState.Pits;
                    break;

                case PcarsState.Pits: // as in practice pits; not racing pits
                    // The PCARS lap counter is dicky around entering pits, it doesn't count up
                    // So driver can run sectors, return to pits and have a broken timing screen.
                    if (lastMeter > 0)
                    {
                        Console.WriteLine("leaving pits");
                        // Clear old data
                        Clear = true;

                        State = PcarsState.Outlap;
                    }
                    if (!Participant.mIsActive)
                        State = PcarsState.Offline;
                    break;

                case PcarsState.Outlap:
                    Record = true;
                    if (!Participant.mIsActive)
                        State = PcarsState.Offline;
                    else if (meters == 0 && lastMeter == 0)
                        State = PcarsState.Pits;
                    else if (lastMeter > meters && meters != 0)
                    {
                        State = PcarsState.Hotlap;

                        // Store lap
                        Save = true;
                    }
                    break;

                case PcarsState.Hotlap:
                    Record = true;
                    if (!Participant.mIsActive)
                        State = PcarsState.Offline;
                    else if (meters == 0 && lastMeter == 0) // went back to pits
                        State = PcarsState.Pits;
                    else if (lastMeter > meters && meters != 0 && lastLap != lapNow)
                    {
                        Console.WriteLine("{0} went from lap {1} to {2}", Name, lastLap, lapNow);
                        // Store lap
                        Save = true;
                    }
                    break;

            }
            if (State != oldState)
            {
                Console.WriteLine("{0} went from state {1} to {2}", Name, oldState, State);
            }

            if (Save)
            {
                TickTiming(lastLap);
                Meter2TimeTable.Clear();

                // Process all best laptimes
                foreach (var sector in TimingBest.Keys.ToList())
                {
                    if (Timing.ContainsKey(sector) &&
                        Timing[sector].ContainsKey(lastLap) &&
                        Timing[sector][lastLap] > 0)
                        TimingBest[sector] = Math.Min(TimingBest[sector], Timing[sector][lastLap]);
                }

                lastLap = lapNow;
            }
            if (Record && validTime)
            {
                // Only add incremental meters
                if (newData.mCurrentLapDistance > lastMeter && !Meter2TimeTable.ContainsKey(meters))
                {
                    Meter2TimeTable.Add(meters, t);
                    lastMeter = meters;
                    TickTiming(lapNow);
                }

                switch (newData.mCurrentSector)
                {
                    case 1:
                        Location = PcarsLocation.Sector1;
                        break;
                    case 2:
                        Location = PcarsLocation.Sector2;
                        break;
                    case 3:
                        Location = PcarsLocation.Sector3;
                        break;
                }
            }
            else if (!Record)
            {
                if (newData.mIsActive)
                    Location = PcarsLocation.Offline;
                else
                    Location = PcarsLocation.Pits;
            }
            if (Clear)
            {
                Console.WriteLine(Name + ": clearing data without recording the lap " + lapNow);

                Meter2TimeTable.Clear();

                foreach (var sect in Timing.Keys.ToList())
                {
                    Timing[sect].Remove(lapNow);
                }
            }

            lastMeter = meters;
        }

        private void TickTiming(int lap)
        {
            if (this.TrackSession == null || this.TrackSession.Sections == null)
                return;

            foreach (var sector in this.TrackSession.Sections)
            {
                if (Timing.ContainsKey(sector.Name) == false)
                    Timing.Add(sector.Name, new Dictionary<int, float>());
                if (TimingBest.ContainsKey(sector.Name) == false)
                    TimingBest.Add(sector.Name, float.MaxValue);

                // Is start & end in time table?
                if (lastMeter >= sector.End && !Timing[sector.Name].ContainsKey(lap))
                {
                    // find start of lap
                    var timeStart = 0.0f;
                    var timeEnd = 0.0f;
                    var lastDistance = Meter2TimeTable.Keys.FirstOrDefault();

                    bool foundStart = false;
                    bool foundEnd = false;

                    foreach (var kvp in Meter2TimeTable)
                    {
                        var t = kvp.Value;
                        var m = kvp.Key;

                        // Find start
                        if (!foundStart)
                        {
                            if (sector.Start == 0)
                            {
                                if (m < 10)
                                {
                                    // RIGHT HERE
                                    timeStart = t;
                                    foundStart = true;
                                }
                            }
                            if (lastDistance < sector.Start && m >= sector.Start)
                            {
                                // RIGHT HERE
                                timeStart = t;
                                foundStart = true;
                            }
                        }
                        if (!foundEnd)
                        {
                            if (lastDistance < sector.End && m >= sector.End)
                            {
                                // RIGHT HERE
                                timeEnd = t;
                                foundEnd = true;
                                break;
                            }
                        }
                        lastDistance = m;
                    }

                    if (foundEnd && foundStart)
                    {
                        var dt = timeEnd - timeStart;
                        if (dt > 0)
                        {
                            Timing[sector.Name].Add(lap, dt);
                        }

                        var distance = sector.End - sector.Start;
                        Console.WriteLine("Participant " + Name + " > " + sector.Name + " took " + dt + " (" + Math.Round(distance/dt*3.6,2)+"km/h)");
                    }
                }
            }
        }
    }

    public enum PcarsState
    {
        Offline,
        Pits,
        Outlap,
        Hotlap,
    }

    public enum PcarsLocation
    {
        Pits,
        Sector1,
        Sector2,
        Sector3,
        Offline
    }
}
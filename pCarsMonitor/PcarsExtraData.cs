using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using pCarsTelemetry.API;

namespace pCarsMonitor
{
    public class PcarsExtraData
    {
        public List<PcarsExtraParticipant> Participants = new List<PcarsExtraParticipant>();
        public PcarsExtraParticipant Me { get; private set; }

        public void Tick(PcarsTelemetrySample sample)
        {
            // Check all participants that already exist
            for(int k = 0; k < sample.mNumParticipants; k++)
            {
                var part = sample.mParticipantData[k];
                if (string.IsNullOrEmpty(part.mName))
                    continue;

                var myPart = LookupParticipant(part);

                if (myPart != null)
                    myPart.Tick(sample, part);
                else
                {
                    Participants.Add(new PcarsExtraParticipant(sample, part));
                    Console.WriteLine("Adding participant " + part.mName);
                }
            }
            // Apply a scale to all participants
            var mySpeed = sample.mSpeed;
            Me = (sample.mViewedParticipantIndex < 0) ? null : LookupParticipant(sample.mParticipantData[sample.mViewedParticipantIndex]);

            if (Me != null && Me.Scale < 0.5)
                Me.Scale = 0.5;

            // only apply speed factor corrections above 30m/s
            if (Me != null && Me.Speed > 0 && mySpeed > 20 && Math.Abs(sample.mWorldAcceleration[2]) < 3)
            {
                var oldError = Me.Scale;
                Me.Scale = 1;
                // low-pass filter scale to some degree
                var error = oldError * 0.995f + 0.005f * Me.Speed / mySpeed;
                //Console.WriteLine(error);
                foreach (var p in Participants)
                    p.Scale = error;
            }
        }

        public PcarsExtraParticipant LookupParticipant(PcarsParticipant driver)
        {
            return Participants.FirstOrDefault(x => x.Name == driver.mName);
        }
    }
}
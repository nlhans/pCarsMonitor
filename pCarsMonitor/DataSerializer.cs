using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using pCarsTelemetry.API;

namespace pCarsMonitor
{
    public class DataSerializer
    {
        private DataController data;

        private Dictionary<string, Dictionary<int, Dictionary<float, float>>> records =
            new Dictionary<string, Dictionary<int, Dictionary<float, float>>>();

        private string CurrentTrack;
        private string CurrentSession;

        private bool exporting = false;

        public DataSerializer(DataController dataCtl)
        {
            data = dataCtl;

            data.Data += DataOnData;
        }

        private void DataOnData(PcarsTelemetrySample data, PcarsExtraData myData)
        {
            if (exporting) return;

            var timestamp = data.mEventTimeRemaining;

            if (timestamp > 0)
            {
                // Store all competitors data, on a given timestamp.
                // The timestamp is against end of session, because PCARS derpiness
                foreach (var comp in data.mParticipantData)
                {
                    var id = comp.mName;
                    var lap = (int) comp.mCurrentLap;

                    if (records.ContainsKey(id) == false)
                        records.Add(id, new Dictionary<int, Dictionary<float, float>>());
                    if (records[id].ContainsKey(lap) == false)
                        records[id].Add(lap, new Dictionary<float, float>());

                    if (records[id][lap].ContainsKey(timestamp) == false)
                        records[id][lap].Add(timestamp, comp.mCurrentLapDistance);
                }
            }

            if (timestamp == -1 && records.Count > 0)
            {
                // Export & clear
                Export(true);

                records.Clear();
            }
        }

        public void Export(bool final)
        {
            exporting = true;
            var dn = DateTime.Now;
            var date = string.Format("{0:0000}{1:00}{2:00} {3:00}{4:00}{5:00}", dn.Year, dn.Month, dn.Day, dn.Hour, dn.Minute, dn.Second);

            var file = "data/" + CurrentTrack + " - " + CurrentSession + " " + date + " " + (final ? "FINAL" : "INTERMEDIATE") + ".pcars-time";
 
            var exp = "";
            foreach (var compKvp in records)
            {
                exp += "competitor=" + compKvp.Key + "\r\n";

                foreach (var lapKvp in compKvp.Value)
                {
                    exp += "lap=" + lapKvp.Key + "\r\n";
                    foreach (var time2meter in lapKvp.Value)
                        exp += time2meter.Key + "|" + time2meter.Value + "\r\n";
                }
            }

            exporting = false;
            File.WriteAllText(file, exp);
        }
    }
}

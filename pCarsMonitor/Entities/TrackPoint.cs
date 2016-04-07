using System;

namespace pCarsMonitor.Entities
{
    public class TrackPoint
    {
        public float Meter { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }

        public TrackPoint(float meter, float[] pos)
        {
            Meter = meter;
            X = pos[0];
            Y = pos[1];
            Z = pos[2];
        }

        public TrackPoint(string line, out bool valid)
        {
            var data = line.Split("|".ToCharArray(), 4);

            if (data.Length == 4)
            {
                float pm = 0;
                float px = 0;
                float py = 0;
                float pz = 0;
                valid = true;

                valid &= float.TryParse(data[0], out pm);
                valid &= float.TryParse(data[1], out px);
                valid &= float.TryParse(data[2], out py);
                valid &= float.TryParse(data[3], out pz);

                Meter = pm;
                X = px;
                Y = py;
                Z = pz;
            }
            else
            {
                valid = false;
            }
        }

        public string Save()
        {
            return string.Format("{0}|{1}|{2}|{3}", Meter, X, Y, Z);
        }
    }
}
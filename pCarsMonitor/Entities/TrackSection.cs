using System;
using System.Drawing;
using System.Globalization;

namespace pCarsMonitor.Entities
{
    public class TrackSection
    {
        public string Name { get; private set; }
        public TrackSectionType SectionType { get; private set; }
        public int Start { get; private set; }
        public int End { get; private set; }
        public Color Color { get; private set; }

        public TrackSection(string name, TrackSectionType type, int start, int end, Color c)
        {
            Name = name;
            SectionType = type;
            Start = start;
            End = end;
            Color = c;
        }

        public TrackSection(string line, out bool valid)
        {
            var data = line.Split("|".ToCharArray());

            if (data.Length == 5)
            {
                TrackSectionType pt;
                int pc = 0;
                int ps = 0;
                int pe = 0;
                valid = true;

                Name = data[0];
                valid &= Enum.TryParse(data[1], true, out pt);

                valid &= int.TryParse(data[2], out ps);
                valid &= int.TryParse(data[3], out pe);
                valid &= int.TryParse(data[4], NumberStyles.HexNumber, null, out pc);

                // Parse to color
                var a = (pc >> 24) & 0xFF;
                if (a == 0) a = 255;
                var r = (pc >> 16) & 0xFF;
                var g = (pc >> 8) & 0xFF;
                var b = pc & 0xFF;
                Color = Color.FromArgb(a, r, g, b);
                //https://coolors.co/app/264653-2a9d8f-e9c46a-f4a261-e76f51

                SectionType = pt;
                Start = ps;
                End = pe;
            }
            else
            {
                valid = false;
            }
        }

        public string Save()
        {
            var c = string.Format("{0:XX}{1:XX}{2:XX}", Color.R, Color.G, Color.B);
            return string.Format("{0}|{1}|{2}|{3}|{4:X}", Name, SectionType, Start, End, c);
        }
    }
}
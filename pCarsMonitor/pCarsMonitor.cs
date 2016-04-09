using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using pCarsMonitor.Entities;
using pCarsMonitor.Usercontrols;

namespace pCarsMonitor
{
    public partial class pCarsMonitor : Form
    {
        private DataController data = new DataController();
        private DataSerializer serializer;

        private ucTrackmap trackmap;
        private ucLapchart laps;
        private ucTimingchart timing;
        private ucCarState car;

        public pCarsMonitor()
        {
            var dbg = new Debug(data);
            dbg.Show();
            InitializeComponent();

            //serializer = new DataSerializer(data);

            trackmap = new ucTrackmap(data);
            laps = new ucLapchart(data);
            timing = new ucTimingchart(data);
            car = new ucCarState(data);
            Controls.Add(trackmap);
            Controls.Add(laps);
            Controls.Add(timing);
            Controls.Add(car);

            this.Layout += Form1_Layout;
        }

        private void Form1_Layout(object sender, LayoutEventArgs e)
        {
            trackmap.Location = new Point(0, 0);
            trackmap.Size = new Size(this.Width/2, this.Height*3/4);

            car.Location = new Point(0, trackmap.Height);
            car.Size = new Size(this.Width/2, this.Height/4);

            var lapsHeight = Math.Max(300, this.Height*35/100); // 30%
            var timingHeight = this.Height - lapsHeight;

            laps.Location = new Point(this.Width/2, 0);
            laps.Size = new Size(this.Width/2, lapsHeight);

            timing.Location = new Point(this.Width/2, lapsHeight);
            timing.Size = new Size(this.Width/2, timingHeight);

        }
    }
}

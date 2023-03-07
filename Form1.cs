using Road_Marking_Detect.Model;
using Road_Marking_Detect.ModelView;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Road_Marking_Detect
{
    public partial class Form1 : Form
    {
        OpenImage openImage;
        public Form1()
        {
            InitializeComponent();
            openImage = new OpenImage();
            pictureBox1.Image = openImage.GetImageForPictureBox(true, true) ;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openImage = new OpenImage();
            pictureBox1.Image = openImage.GetImageForPictureBox(true, true);
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            pictureBox1.Image = openImage.GetImageForPictureBox(checkBox1.Checked, checkBox2.Checked);
        }
    }
}

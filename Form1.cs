using OpenCvSharp;
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
            LoadImage(@"C:\Users\danma\Downloads\Дорога_1.jpg");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var tempImg = pictureBox1.Image;
            pictureBox1.Image = null;
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != "openFileDialog1" && openFileDialog1.FileName.Length > 0)
                LoadImage(openFileDialog1.FileName);
            else
                pictureBox1.Image = tempImg;
        }
        private void LoadImage(string file_name)
        {
            openImage = new OpenImage(file_name);
            pictureBox1.Image = openImage.GetImageForPictureBox(false, true);
            procentOfWhiteLabel.Text = openImage.GetProcentOfWhiteText();
        }
        private void GetImage(object sender, EventArgs e)
        {
            pictureBox1.Image = openImage.GetImageForPictureBox(checkBox1.Checked, checkBox2.Checked);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CreateReport.Create(openImage.GetImageForPictureBox(),openImage.markingQualityDetection);
        }
    }
}

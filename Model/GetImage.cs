using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Road_Marking_Detect.Model
{
    class GetImage
    {
        public static Mat OpenFromFile(string path = "")
        {
            if (path == "")
            {
                var file_Dialog = new OpenFileDialog();
                file_Dialog.ShowDialog();
                path = file_Dialog.FileName;
            }
            var image = Cv2.ImRead(path, ImreadModes.Color);
            return image;
        }
    }
}

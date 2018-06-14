using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Ocl;
using Emgu.CV.Structure;

namespace HandPaint.SkinDetector
{
    public class HsvSkinDetector : IColorSkinDetector
    {
        public override Image<Gray, byte> DetectSkin(Image<Bgr, byte> Img, IColor min, IColor max)
        {
            Image<Hsv, Byte> currentHsvFrame = Img.Convert<Hsv, Byte>();
            Image<Gray, byte> skin = new Image<Gray, byte>(Img.Width, Img.Height);
            skin = currentHsvFrame.InRange((Hsv)min, (Hsv)max);
            //CvInvoke.Erode(skin, skin, new Mat(6, 6, DepthType.Cv8U, 1), new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());
            //CvInvoke.Dilate(skin, skin, new Mat(3, 3, DepthType.Cv8U, 1), new Point(-1, -1), 1, BorderType.Constant, new MCvScalar());
            //CvInvoke.MorphologyEx(skin,skin,MorphOp.Open, new Mat(3, 3, DepthType.Cv8U, 1), new Point(-1,-1), 3, BorderType.Default, new MCvScalar());
            //CvInvoke.GaussianBlur(skin,skin, new Size(5,5), 100);
            return skin;
        }
    }
}

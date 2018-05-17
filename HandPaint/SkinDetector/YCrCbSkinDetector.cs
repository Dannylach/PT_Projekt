using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace HandPaint.SkinDetector
{
    class YCrCbSkinDetector : IColorSkinDetector
    {
        public override Image<Gray, byte> DetectSkin(Image<Bgr, byte> Img, IColor min, IColor max)
        {
            Image<Ycc, Byte> currentYCrCbFrame = Img.Convert<Ycc, Byte>();
            Image<Gray, byte> skin = new Image<Gray, byte>(Img.Width, Img.Height);
            skin = currentYCrCbFrame.InRange((Ycc)min, (Ycc)max);
            CvInvoke.Erode(skin, skin, new Mat(12, 12, DepthType.Cv8U, 1), new Point(6, 6), 1, BorderType.Constant, new MCvScalar());
            CvInvoke.Dilate(skin, skin, new Mat(6, 6, DepthType.Cv8U, 1), new Point(3, 3), 2, BorderType.Constant, new MCvScalar());
            return skin;
        }
    }
}

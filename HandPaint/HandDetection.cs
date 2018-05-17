using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using HandPaint.SkinDetector;
using OpenTK;
using Size = System.Drawing.Size;

namespace HandPaint
{
    public class HandDetection
    {
        private Image<Bgr, byte> ImageFrame;
        private IColor hsv_min = new Hsv(10, 45, 100);
        private IColor hsv_max = new Hsv(20, 255, 230);
        private readonly IColor YCrCb_min = new Ycc(100, 131, 80);
        private readonly IColor YCrCb_max = new Ycc(200, 185, 135);
        private RotatedRect box;
        private int[,] startIndex;
        private int[,] endIndex;
        private int[,] depthIndex;
        private Mat defects;
        private VectorOfPoint currentContour;



        public Bitmap DetectHand(Bitmap source)
        {
            ImageFrame = new Image<Bgr, byte>(source); 

            var skinDetector = new HsvSkinDetector();

            var skin = skinDetector.DetectSkin(ImageFrame, hsv_min, hsv_max);

            ExtractContourAndHull(ImageFrame, skin);

            DrawAndComputeFingersNum();

            return ImageFrame.Bitmap; 
        }

        private void ExtractContourAndHull(Image<Bgr,byte> originalImage, Image<Gray, byte> skin)
        {
            var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(skin, contours, new Mat(), RetrType.List, ChainApproxMethod.ChainApproxSimple);
            var result2 = 0;
            VectorOfPoint biggestContour = null;
            if (contours.Size != 0)
                biggestContour = contours[0];

            for (var i=0; i < contours.Size; i++)
            {
                var result1 = contours[i].Size;
                if (result1 <= result2) continue;
                result2 = result1;
                biggestContour = contours[i];
            }

            if (biggestContour == null) return;
            
            currentContour = new VectorOfPoint();
            CvInvoke.ApproxPolyDP(biggestContour, currentContour, 0, true);
            //TODO Get to know why it gives exception
            //ImageFrame.Draw(biggestContour, 3, new Bgr(Color.LimeGreen));
            biggestContour = currentContour;


            var pointsToFs = new PointF[currentContour.Size];
            for (var i = 0; i < currentContour.Size; i++)
                pointsToFs[i] = new PointF(currentContour[i].X, currentContour[i].Y);

            var hull = CvInvoke.ConvexHull(pointsToFs, true);
            
            pointsToFs = new PointF[biggestContour.Size];
            for (var i = 0; i < biggestContour.Size; i++)
                pointsToFs[i] = new PointF(biggestContour[i].X, biggestContour[i].Y);

            box = CvInvoke.MinAreaRect(pointsToFs);
            var points = box.GetVertices();

            var ps = new Point[points.Length];
            for (var i = 0; i < points.Length; i++)
                ps[i] = new Point((int) points[i].X, (int) points[i].Y);

            var hullToPoints = new Point[hull.Length];
            for (var i=0; i<hull.Length; i++)
                hullToPoints[i] = Point.Round(hull[i]);

            originalImage.DrawPolyline(hullToPoints, true, new Bgr(200, 125, 75), 2);
            originalImage.Draw(new CircleF(new PointF(box.Center.X, box.Center.Y), 3), new Bgr(200, 125, 75), 2);

            //ellip.MCvBox2D = CvInvoke.cvFitEllipse2(biggestContour.Ptr);
            //currentFrame.Draw(new Ellipse(ellip.MCvBox2D), new Bgr(Color.LavenderBlush), 3);

            //PointF center;
            //float radius;
            //CvInvoke.cvMinEnclosingCircle(biggestContour.Ptr, out center, out radius);
            //currentFrame.Draw(new CircleF(center, radius), new Bgr(Color.Gold), 2);

            //currentFrame.Draw(new CircleF(new PointF(ellip.MCvBox2D.center.X, ellip.MCvBox2D.center.Y), 3), new Bgr(100, 25, 55), 2);
            //currentFrame.Draw(ellip, new Bgr(Color.DeepPink), 2);

            //CvInvoke.cvEllipse(currentFrame, new Point((int)ellip.MCvBox2D.center.X, (int)ellip.MCvBox2D.center.Y), new System.Drawing.Size((int)ellip.MCvBox2D.size.Width, (int)ellip.MCvBox2D.size.Height), ellip.MCvBox2D.angle, 0, 360, new MCvScalar(120, 233, 88), 1, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, 0);
            //currentFrame.Draw(new Ellipse(new PointF(box.center.X, box.center.Y), new SizeF(box.size.Height, box.size.Width), box.angle), new Bgr(0, 0, 0), 2);


            /*var filteredHull = new VectorOfPointF();

            for (var i = 0; i < hull.Length - 1; i++)
            {
                if (Math.Sqrt(Math.Pow(hull[i].X - hull[i + 1].X, 2) + Math.Pow(hull[i].Y - hull[i + 1].Y, 2)) >
                    box.Size.Width / 10)
                {
                    filteredHull.Push();
                }
            }*/

            var convexHull = new VectorOfInt();
            CvInvoke.ConvexHull(currentContour, convexHull, false, false);
            defects = new Mat();
            CvInvoke.ConvexityDefects(currentContour, convexHull, defects);
            Matrix<int> m = new Matrix<int>(defects.Rows, defects.Cols, defects.NumberOfChannels); // copy Mat to a matrix...
            defects.CopyTo(m);
            Matrix<int>[] channels = m.Split();
            if (channels.Length != 0)
            {
                startIndex = channels.ElementAt(0).Data;
                endIndex = channels.ElementAt(1).Data;
                depthIndex = channels.ElementAt(2).Data;
            }
            
        }
        
        private void DrawAndComputeFingersNum()
        {
            int fingerNum = 0;

            #region hull drawing
            //for (int i = 0; i < filteredHull.Total; i++)
            //{
            //    PointF hullPoint = new PointF((float)filteredHull[i].X,
            //                                  (float)filteredHull[i].Y);
            //    CircleF hullCircle = new CircleF(hullPoint, 4);
            //    currentFrame.Draw(hullCircle, new Bgr(Color.Aquamarine), 2);
            //}
            #endregion

            #region defects drawing
            if (startIndex == null) return;
            for (var i = 0; i < startIndex.Length; i++)
            {
                PointF startPoint = new PointF(currentContour[startIndex[i, 0]].X, currentContour[startIndex[i, 0]].Y);

                PointF depthPoint = new PointF(currentContour[depthIndex[i, 0]].X, currentContour[depthIndex[i, 0]].Y);

                PointF endPoint = new PointF(currentContour[endIndex[i, 0]].X, currentContour[endIndex[i, 0]].Y);

                LineSegment2D startDepthLine = new LineSegment2D(new Point((int)startPoint.X, (int)startPoint.Y), new Point((int)depthPoint.X, (int)depthPoint.Y));

                LineSegment2D depthEndLine = new LineSegment2D(new Point((int)depthPoint.X, (int)depthPoint.Y), new Point((int)endPoint.X, (int)endPoint.Y));

                CircleF startCircle = new CircleF(startPoint, 5f);

                CircleF depthCircle = new CircleF(depthPoint, 5f);

                CircleF endCircle = new CircleF(endPoint, 5f);

                //Custom heuristic based on some experiment, double check it before use
                if ((startCircle.Center.Y < box.Center.Y || depthCircle.Center.Y < box.Center.Y) && (startCircle.Center.Y < depthCircle.Center.Y) && (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) + Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.Size.Height / 6.5))
                {
                    fingerNum++;
                    ImageFrame.Draw(startDepthLine, new Bgr(Color.Green), 2);
                    //currentFrame.Draw(depthEndLine, new Bgr(Color.Magenta), 2);
                }


                ImageFrame.Draw(startCircle, new Bgr(Color.Red), 2);
                ImageFrame.Draw(depthCircle, new Bgr(Color.Yellow), 5);
                //currentFrame.Draw(endCircle, new Bgr(Color.DarkBlue), 4);*/
            }
            #endregion

            //MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_DUPLEX, 5d, 5d);
            //currentFrame.Draw(fingerNum.ToString(), ref font, new Point(50, 150), new Bgr(Color.White));
        }
    }
}

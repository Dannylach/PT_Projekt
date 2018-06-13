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
        private IColor hsv_min = new Hsv(90, 100, 100);
        private IColor hsv_max = new Hsv(120, 200, 230);
        private readonly IColor YCrCb_min = new Ycc(100, 131, 80);
        private readonly IColor YCrCb_max = new Ycc(200, 185, 135);
        private RotatedRect box;
        private int[,] startIndex;
        private int[,] endIndex;
        private int[,] depthIndex;
        private Mat defects;
        private VectorOfPoint currentContour;
        private int fingerNumb;
        

        public PointF DetectHand(Bitmap source)
        {
            ImageFrame = new Image<Bgr, byte>(source); 

            var skinDetector = new HsvSkinDetector();

            var skin = skinDetector.DetectSkin(ImageFrame, hsv_min, hsv_max);

            ExtractContourAndHull(ImageFrame, skin);

            

            return DrawAndComputeFingersNum();
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

            var convexHull = new VectorOfInt();
            CvInvoke.ConvexHull(currentContour, convexHull, false, false);
            defects = new Mat();
            CvInvoke.ConvexityDefects(currentContour, convexHull, defects);
            if (!defects.IsEmpty)
            {
                Matrix<int> m = new Matrix<int>(defects.Rows, defects.Cols,
                    defects.NumberOfChannels); // copy Mat to a matrix...
                defects.CopyTo(m);
                Matrix<int>[] channels = m.Split();
                if (channels.Length >= 2)
                {
                    startIndex = channels.ElementAt(0).Data;
                    endIndex = channels.ElementAt(1).Data;
                    depthIndex = channels.ElementAt(2).Data;
                }
            }
        }

        private PointF DrawAndComputeFingersNum()
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

            if (startIndex == null) return new PointF(0, 0);
            double tempLenght = 0;
            PointF tempPointF = new PointF();
            for (var i = 0; i < startIndex.Length; i++)
            {
                PointF startPoint = new PointF(currentContour[startIndex[i, 0]].X, currentContour[startIndex[i, 0]].Y);

                PointF depthPoint = new PointF(currentContour[depthIndex[i, 0]].X, currentContour[depthIndex[i, 0]].Y);

                PointF endPoint = new PointF(currentContour[endIndex[i, 0]].X, currentContour[endIndex[i, 0]].Y);

                LineSegment2D startDepthLine = new LineSegment2D(new Point((int) startPoint.X, (int) startPoint.Y),
                    new Point((int) depthPoint.X, (int) depthPoint.Y));

                LineSegment2D depthEndLine = new LineSegment2D(new Point((int) depthPoint.X, (int) depthPoint.Y),
                    new Point((int) endPoint.X, (int) endPoint.Y));

                CircleF startCircle = new CircleF(startPoint, 5f);

                CircleF depthCircle = new CircleF(depthPoint, 5f);

                CircleF endCircle = new CircleF(endPoint, 5f);
                if ((startCircle.Center.Y < box.Center.Y || depthCircle.Center.Y < box.Center.Y) &&
                    (startCircle.Center.Y < depthCircle.Center.Y) &&
                    (Math.Sqrt(Math.Pow(startCircle.Center.X - depthCircle.Center.X, 2) +
                               Math.Pow(startCircle.Center.Y - depthCircle.Center.Y, 2)) > box.Size.Height / 6.5))
                {
                    fingerNum++;
                    //ImageFrame.Draw(startDepthLine, new Bgr(Color.Green), 2);
                    var lenghtLine = startCircle.Center.X > depthCircle.Center.X
                        ? Math.Pow((startCircle.Center.X - depthCircle.Center.X), 2)
                        : Math.Pow((depthCircle.Center.X - startCircle.Center.X), 2);
                    lenghtLine += startCircle.Center.Y > depthCircle.Center.Y
                        ? Math.Pow((startCircle.Center.Y - depthCircle.Center.Y), 2)
                        : Math.Pow((depthCircle.Center.Y - startCircle.Center.Y), 2);
                    lenghtLine = Math.Sqrt(lenghtLine);
                    if (tempLenght < lenghtLine)
                    {
                        tempLenght = lenghtLine;
                        tempPointF.X = startCircle.Center.X;
                        tempPointF.Y = startCircle.Center.Y;
                    }

                    //ImageFrame.Draw(startCircle, new Bgr(Color.Red), 2);
                    //ImageFrame.Draw(depthCircle, new Bgr(Color.Yellow), 5);
                }
            }

            var tempCircleF = new CircleF(tempPointF, 10);
            ImageFrame.Draw(tempCircleF, new Bgr(Color.MediumVioletRed));
            fingerNumb = fingerNum;
            #endregion

            return tempPointF;
        }

        public bool IsDrawing()
        {
            return fingerNumb < 3;
        }

        public int GetFingerNumb()
        {
            return fingerNumb;
        }
    }
}

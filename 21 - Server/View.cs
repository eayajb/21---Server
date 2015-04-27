using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.Kinect;
using Models;
using System.Windows.Shapes;
using System.Windows.Media;

namespace _21___Server
{
    class View
    {
        MainWindow main;
        Models.ModelsHandler modelsHandler;

        Canvas bodyPointsCanvas1, bodyPointsCanvas2, mainBodyCanvas;
        double canv1Height, canv1Width;
        double canv2Height, canv2Width;
        double mainCanvHeight, mainCanvWidth;

        double refFrameDepth, refFrameHeight, refFrameWidth;
        double cam1RefDepth, cam1RefHeight, cam1RefWidth = 0;
        double cam2RefDepth, cam2RefHeight, cam2RefWidth = 0;

        public View(MainWindow inMain, Models.ModelsHandler modelsHandler)
        {
            this.main = inMain;
            this.modelsHandler = modelsHandler;

            Thread modelListenerThread = new Thread(ModelListenerThread);
            modelListenerThread.SetApartmentState(ApartmentState.STA);
            modelListenerThread.Start();
        }

        void ModelListenerThread()
        {
            for (; ; )
            {
                if (modelsHandler.ViewsSetBool)
                {
                    GetViewsFrameRefs();
                    modelsHandler.ViewsSetBool = false;
                }
                else if (modelsHandler.AllPersonsUpdatedBool)
                {
                    GetAllPersonData();
                    modelsHandler.AllPersonsUpdatedBool = false;
                }
            }
        }

        /// <summary>
        /// DRAWING FUNCTIONS
        /// </summary>

        Boolean drawDataUnlockedBool = false;
        Boolean drawingBool = false;
        List<ColorSpacePoint> client1Points = new List<ColorSpacePoint>();
        List<ColorSpacePoint> client2Points = new List<ColorSpacePoint>();
        List<ColorSpacePoint> mainPoints = new List<ColorSpacePoint>();
        
        private void GetAllPersonData()
        {
            if (!drawingBool)
            {
                this.drawDataUnlockedBool = false;
                this.client1Points.Clear();
                this.client2Points.Clear();
                this.mainPoints.Clear();

                Dictionary<double, Person> personData = modelsHandler.PersonManager.PersonDict;
                foreach (Person person in personData.Values)
                {
                    if (person.Client1Bool)
                    {
                        foreach (ColorSpacePoint point in person.CSPointsList1)
                        {
                            client1Points.Add(point);
                        }
                    }
                    if (person.Client2Bool)
                    {
                        foreach (ColorSpacePoint point in person.CSPointsList2)
                        {
                            client2Points.Add(point);
                        }
                    }

                    //ALL "ROTATED" POINTS SHOULD BE DRAWN TO MAIN CANVAS
                    List<ColorSpacePoint> adjPoints = new List<ColorSpacePoint>();
                    if (person.ConvertedDataBool)
                        adjPoints = person.ConvertedPoints;

                    foreach (ColorSpacePoint point in adjPoints)
                    {
                        mainPoints.Add(point);
                    }

                    if (person.Client1Bool)
                    {
                        foreach (ColorSpacePoint point in person.CSPointsList1)
                        {
                            mainPoints.Add(point);
                        }
                    }
                }

                this.drawDataUnlockedBool = true;
            }
        }

        public void DrawAllPersonData()
        {
            if (this.drawDataUnlockedBool)
            {
                this.drawingBool = true;

                this.bodyPointsCanvas1.Children.Clear();
                this.bodyPointsCanvas2.Children.Clear();
                this.mainBodyCanvas.Children.Clear();

                DrawToCanvas1(client1Points);
                DrawToCanvas2(client2Points);

                DrawToMainCanvas(mainPoints);

                this.drawingBool = false;
            }
        }

        // #########################

        private void DrawToCanvas1(List<ColorSpacePoint> points)
        {
            this.bodyPointsCanvas1.Children.Clear();

            foreach (ColorSpacePoint point in points)
            {
                Ellipse ellipse = GetRedEllipse();

                if (point.X > 0 && point.Y > 0)
                {
                    ///CONVERT POSITION TO CANVAS
                    Double convX = canv1Width * (point.X / this.cam1RefWidth);
                    Double convY = canv1Height * (point.Y / this.cam1RefHeight);

                    ///SET POSITION AND ADD TO CANVAS
                    Canvas.SetLeft(ellipse, convX - (ellipse.Width / 2));
                    Canvas.SetTop(ellipse, convY - (ellipse.Height / 2));

                    this.bodyPointsCanvas1.Children.Add(ellipse);
                }
            }
        }

        // #########################

        private void DrawToCanvas2(List<ColorSpacePoint> points)
        {
            foreach (ColorSpacePoint point in points)
            {
                Ellipse ellipse = GetRedEllipse();

                if (point.X > 0 && point.Y > 0)
                {
                    ///CONVERT POSITION TO CANVAS
                    Double convX = canv2Width * (point.X / this.cam2RefWidth);
                    Double convY = canv2Height * (point.Y / this.cam2RefHeight);

                    ///SET POSITION AND ADD TO CANVAS
                    Canvas.SetLeft(ellipse, convX - (ellipse.Width / 2));
                    Canvas.SetTop(ellipse, convY - (ellipse.Height / 2));

                    bodyPointsCanvas2.Children.Add(ellipse);
                }
            }
        }

        // #########################

        private void DrawToMainCanvas(List<ColorSpacePoint> points)
        {
            foreach (ColorSpacePoint point in points)
            {
                Ellipse ellipse = GetRedEllipse();

                if (point.X > 0 && point.Y > 0)
                {
                    ///CONVERT POSITION TO CANVAS
                    Double convX = mainCanvWidth * (point.X / this.refFrameWidth);
                    Double convY = mainCanvHeight * (point.Y / this.refFrameHeight);

                    ///SET POSITION AND ADD TO CANVAS
                    Canvas.SetLeft(ellipse, convX - (ellipse.Width / 2));
                    Canvas.SetTop(ellipse, convY - (ellipse.Height / 2));

                    mainBodyCanvas.Children.Add(ellipse);
                }
            }
        }

        // #########################

        private Ellipse GetRedEllipse()
        {
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Red
            };

            return ellipse;
        }

        /// <summary>
        /// GETTERS
        /// </summary>

        private void GetViewsFrameRefs()
        {
            double[] frameRefs = this.modelsHandler.ViewsData.GetFramerRefs();

            this.cam1RefDepth = frameRefs[0];
            this.cam1RefHeight = frameRefs[1];
            this.cam1RefWidth = frameRefs[2];
            this.cam2RefDepth = frameRefs[3];
            this.cam2RefHeight = frameRefs[4];
            this.cam2RefWidth = frameRefs[5];

            SetMainReferenceFrame(cam1RefDepth, cam1RefHeight, cam1RefWidth);
        }

        /// <summary>
        /// SETTERS
        /// </summary>

        internal void SetAllCanvas(Canvas bodyCanvas1, Canvas bodyCanvas2, Canvas mainBodyCanvas)
        {
            this.bodyPointsCanvas1 = bodyCanvas1;
            this.canv1Height = bodyCanvas1.Height;
            this.canv1Width = bodyCanvas1.Width;

            this.bodyPointsCanvas2 = bodyCanvas2;
            this.canv2Height = bodyCanvas2.Height;
            this.canv2Width = bodyCanvas2.Width;

            this.mainBodyCanvas = mainBodyCanvas;
            this.mainCanvHeight = mainBodyCanvas.Height;
            this.mainCanvWidth = mainBodyCanvas.Width;
        }

        internal void SetMainReferenceFrame(double refFrameDepth, double refFrameHeight, double refFrameWidth)
        {
            this.refFrameDepth = refFrameDepth;
            this.refFrameHeight = refFrameHeight;
            this.refFrameWidth = refFrameWidth;
        }
    }
}
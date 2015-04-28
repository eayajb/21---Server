using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace _21___Server
{
    class DataHandler
    {
        Models.ModelsHandler modelsHandler;
        RegistrationHelper registrationHelper;
        
        Matrix<double> rotationMatrix = null;
        Vector<double> translationVector = null;
        private bool registrationRequiredBool = false;

        public DataHandler( Models.ModelsHandler modelsHandler )
        {
            this.modelsHandler = modelsHandler;
            this.registrationHelper = new RegistrationHelper();
        }

// ############################
        //DATA INPUT

        internal void SetRefernceFrameData(string clientID, double[] viewData)
        {
            this.modelsHandler.ViewsData.SetViewsData(clientID, viewData);
        }
        
        internal void PassData( string clientID, List< ServerData.Person > personList )
        {
            SetData( clientID, personList );

            if ( !registrationHelper.RegistrationCompleteBool )
                RegisterCameraData( clientID, personList );
    
            Server.SendCodeToClientList("t");
        }

// ############################
        //REGISTRATION

        int registrationCounter = 0;
        int registrationIterations = 300;

        internal void RegisterCameraData( string clientID, List< ServerData.Person > personList )
        {
            if ( RegistrationRequired )
            {
                registrationHelper.AddClientData(clientID, personList);

                if (registrationCounter == 0)
                    Console.WriteLine("REGISTRATION IS BEING CALCULATED...");

                if ( registrationHelper.RegistrationCompleteBool || registrationCounter >= registrationIterations )
                {
                    Console.WriteLine("REGISTRATION SUCCESSFULLY CALCULATED TO, RMSE: " + registrationHelper.RMSE);
                    Console.WriteLine("*** CALCULATED IN " + registrationCounter + " ITERATIONS ***");
                    Console.WriteLine();
                    if (registrationCounter >= registrationIterations)
                        Console.WriteLine("ENTER 'r' TO RE-CALCULATE...");
                    registrationCounter = 0;

                    this.rotationMatrix = registrationHelper.RotationMatrix;
                    this.translationVector = registrationHelper.TranslationVector;

                    this.RegistrationRequired = false;
                }

                registrationCounter++;
            }
        }

//############################
        //DATA HANDLER

        internal void SetData(string clientID, List< ServerData.Person > personList)
        {
            foreach( ServerData.Person person in personList ){
                if (clientID == "CLIENT1")
                {
                    this.modelsHandler.PersonManager.UpdatePersonData
                        (clientID, person.ID, person.bodyPointsDict, person.csPoints, person.csPoints, false);
                }
                else if (clientID == "CLIENT2")
                {
                    this.modelsHandler.PersonManager.UpdatePersonData
                        (clientID, person.ID, person.bodyPointsDict, person.csPoints, ConvertCSPoints(person.csPoints), true);
                }
            }
        }

//###########################
        //CONVERTERS

        internal Dictionary<JointType, Point3D> ConvertToRefernceFrame(Dictionary<JointType, Point3D> bodyPointsDict)
        {
            Dictionary<JointType, Point3D> convertedBodyPointsDict = new Dictionary<JointType, Point3D>();
            Point3D convertedPoint = new Point3D();

            Vector<double> point = DenseVector.OfArray(new double[3]);

            foreach (JointType joint in bodyPointsDict.Keys)
            {
                point[0] = bodyPointsDict[joint].X;
                point[1] = bodyPointsDict[joint].Y;
                point[2] = bodyPointsDict[joint].Z;

                if (registrationHelper.RegistrationCompleteBool)
                    point = point * this.rotationMatrix + this.translationVector;

                convertedPoint.X = point[0];
                convertedPoint.Y = point[1];
                convertedPoint.Z = point[2];

                convertedBodyPointsDict.Add(joint, convertedPoint);
            }

            Point3D head1 = bodyPointsDict[JointType.Head];
            Point3D head2 = convertedBodyPointsDict[JointType.Head];
            //if (registrationHelper.RegistrationCompleteBool)
            //    Console.WriteLine( "bpHEAD " + head1.X + " " + head2.X );

            return convertedBodyPointsDict;
        }

        internal List<ColorSpacePoint> ConvertCSPoints(List<ColorSpacePoint> csPoints)
        {
            List<ColorSpacePoint> convertedPoints = new List<ColorSpacePoint>();
            ColorSpacePoint convertedPoint = new ColorSpacePoint();

            Vector<double> vPoint = DenseVector.OfArray(new double[3]);

            foreach (ColorSpacePoint point in csPoints)
            {
                vPoint[0] = point.X;
                vPoint[1] = point.Y;

                if (registrationHelper.RegistrationCompleteBool)
                    vPoint = vPoint * this.rotationMatrix + this.translationVector;

                convertedPoint.X = (float)vPoint[0];
                convertedPoint.Y = (float)vPoint[1];

                convertedPoints.Add(convertedPoint);
            }

            ColorSpacePoint head1 = csPoints[1];
            ColorSpacePoint head2 = convertedPoints[1];
            //if( registrationHelper.RegistrationCompleteBool )
            //    Console.WriteLine("csHEAD " + head1.X + " " + head2.X);

            return convertedPoints;
        }

//###########################
        //FIELDS

        public bool RegistrationRequired 
        { 
            get {   return this.registrationRequiredBool;     }
            set 
            {
                if(this.registrationRequiredBool != value )
                    this.registrationRequiredBool = value;

                if (this.registrationRequiredBool)
                    registrationHelper.RegistrationCompleteBool = false;
            } 
        }
    }
}
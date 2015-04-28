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
    class RegistrationHelper
    {
        private bool registrationCompleteBool = false;

        private double registrationTolerance = 1.5;
        private double accuracy = 0;
        private double rmse = 100;

        bool client1, client2 = false;
        List<Point3D> client1PointsList = new List<Point3D>();
        List<Point3D> client2PointsList = new List<Point3D>();
        List<Point3D> buffer, list1, list2 = new List<Point3D>();
        List<Point3D> centroidListA, centroidListB;
        Matrix<double> matrixA, matrixB, centroidMatrixA, centroidMatrixB, Rotation;
        Vector<double> centroidVectorA, centroidVectorB, Translation;
        Point3D centroidPointA, centroidPointB;

        public RegistrationHelper(){}

        internal void AddClientData(string clientID, List<ServerData.Person> personList)
        {
            foreach ( ServerData.Person person in personList )
            {
                if (clientID == "CLIENT1")
                {
                    client1 = true;
                    client1PointsList.AddRange( person.bodyPointsDict.Values );
                    buffer = client2PointsList.ToList();
                    client2PointsList.Clear();
                }
                if (clientID == "CLIENT2")
                {
                    client2 = true;
                    client2PointsList.AddRange( person.bodyPointsDict.Values );
                    buffer = client1PointsList.ToList();
                    client1PointsList.Clear();
                }
            }
            
            if( client1 && client2 )
            {
                if (client1PointsList.Count < 1)
                {
                    list1 = buffer;
                    list2 = client2PointsList.ToList();
                }
                if (client2PointsList.Count < 1)
                {
                    list1 = client1PointsList.ToList();
                    list2 = buffer;
                }

                list1.TrimExcess();
                list2.TrimExcess();

                //CALCULATE
                if ( BuildMatricies() )
                {
                    CalculateRotationMatrix();
                    CalculateTranslationMatrix();
                    CalculateAccuracy();
                }
            }
        }

//############################
        //CALCULATIONS

        internal Boolean BuildMatricies()
        {
            if ( list1.Count != list2.Count )
                return false;

           
            this.matrixB = CreateMatrix( list1, list1.Count );
            this.matrixA = CreateMatrix( list2, list2.Count );
            

            centroidPointB = Centroid( list1 );
            centroidListB = new List<Point3D>( list1.Count );
            foreach ( Point3D point in list1 )
                centroidListB.Add( centroidPointB );

            centroidPointA = Centroid( list2 );
            centroidListA = new List<Point3D>( list2.Count );
            foreach( Point3D point in list2 )
                centroidListA.Add(centroidPointA);

            centroidListA.TrimExcess();
            centroidListB.TrimExcess();


            this.centroidMatrixB = CreateMatrix( centroidListB, list1.Count );
            this.centroidVectorB = centroidMatrixB.Column(0);
            
            this.centroidMatrixA = CreateMatrix( centroidListA, list2.Count );
            this.centroidVectorA = centroidMatrixA.Column(0);
            

            return true;
        }

        internal void CalculateRotationMatrix()
        {
            if (centroidMatrixA != null && centroidMatrixB != null)
            {
                Matrix<double> H = (matrixA.Subtract(centroidMatrixA)).Multiply(((matrixB.Subtract(centroidMatrixB)).Transpose()));
                var svd = H.Svd( true );
                this.Rotation = ( svd.U ).Multiply( svd.VT );

                if ( this.Rotation.Determinant() < 0 )
                {
                    Matrix<double> neg = DenseMatrix.OfArray(new double[3, 3] { { 0 ,  0 ,  0 }, { 0 ,  0 ,  0 }, { -1 ,  -1 ,  -1 } });
                    this.Rotation.Multiply(neg);
                }
            }
            else
            {
                Console.WriteLine("*** CENTROIDS HAVE NOT BEEN CALCULATED YET!! ***");
            }
        }

        internal void CalculateTranslationMatrix()
        {
            this.Translation = (-Rotation.Multiply(centroidVectorA)) + centroidVectorB;
        }

        internal void CalculateAccuracy()
        {
            double err = 0;
            Vector<double> error = DenseVector.OfArray(new double[3]);

            for (int i = 0; i < this.matrixB.ColumnCount; i++)
            {
                Vector<double> Pa = this.matrixA.Column(i);
                Vector<double> Pb = this.matrixB.Column(i);

                error = (Pa * Rotation) + Translation - Pb;

                double X = error[0];
                double Y = error[1];
                double Z = error[2];

                err += (X * X) + (Y * Y) + (Z * Z);
            }

            RMSE = Math.Sqrt(err);
            this.accuracy = 1 - (err / 100);

            if (rmse < registrationTolerance)
                RegistrationCompleteBool = true;

        }

        //############################
        //HELPERS

        internal Matrix<double> CreateMatrix(List<Point3D> pointsList, int count )
        {
            if (pointsList.Count() != count)
                Console.WriteLine( "LENGTH ERROR" );

            Matrix<double> matrix = DenseMatrix.OfArray( new double[ 3, count ] );
            
            for ( int column = 0; column < count; column ++ )
            {
                Point3D point = pointsList[column];
                double value = 0;

                for (int row = 0; row < 3; row++)
                {
                    switch (row)
                    {
                        case (0):
                            value = point.X;
                            break;
                        case (1):
                            value = point.Y;
                            break;
                        case (2):
                            value = point.Z;
                            break;
                    }

                    matrix.At(row, column, value);
                }
            }

            return matrix;
        }

        internal Point3D Centroid(List<Point3D> inputList)
        {
            int counter = 0;
            double x = 0;
            double y = 0;
            double z = 0;

            foreach (Point3D point in inputList)
            {
                counter++;
                x += point.X;
                y += point.Y;
                z += point.Z;
            }

            return new Point3D((x / counter), (y / counter), (z / counter));
        }

//#########################
        //FIELDS

        public bool RegistrationCompleteBool
        {
            get{ return this.registrationCompleteBool;  }
            set
            {
                if (this.registrationCompleteBool != value)
                    this.registrationCompleteBool = value;
            }
        }

        public double RMSE
        {
            get {   return this.rmse;  }
            set 
            {
                if (this.rmse != value)
                    this.rmse = value;
            }
        }

        public Matrix<double> RotationMatrix
        {
            get { return this.Rotation;  }
        }

        public Vector<double> TranslationVector
        {
            get {   return this.Translation;   }
        }
    }
}
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;
    using System.Windows.Shapes;
    using System.Collections.Generic;
    using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kinect2FacialHD
{


    public enum DisplayFrameType
    {
        Color,
        Infrared,
        Depth
    }
    public partial class MainWindow : Window
    {

        private KinectSensor _sensor = null;

        private BodyFrameSource _bodySource = null;

        private BodyFrameReader _bodyReader = null;

        //añadimos los framesfaciales...
        private FaceFrameSource _faceFrameSource = null;

        private FaceFrameReader _faceFrameReader = null;
        // Fin.

        private HighDefinitionFaceFrameSource _faceSource = null;

        private HighDefinitionFaceFrameReader _faceReader = null;

        private FaceAlignment _faceAlignment = null;

        private FaceModel _faceModel = null;

        private List<Ellipse> _points = new List<Ellipse>();

        Body[] bodies = null; //ARRAY DE BODIES.


        private MultiSourceFrameReader multiSourceReader;


        public MainWindow()
        {
            
            InitializeComponent();
            InitCanvas();

        }

        private void InitCanvas()
        {
            for (int index = 0; index < FaceModel.VertexCount; index++) {                                  
                Ellipse ellipse = new Ellipse                   
                {                        
                    Width = 2.0,                        
                    Height = 2.0,     
                    Fill = new SolidColorBrush(Colors.Blue)                    
                };                 
                _points.Add(ellipse);
                canvas.Children.Add(ellipse);               
            }            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                /// <summary>
                /// Tarea a realizar por alumno
                /// Fase Inicialización
                /// </summary>
                /// /////////////////////////////////////////////////////////////////////////////////////////////////
                // Obtener fuentes de cuerpos, lector de cuerpos, handler para eventos de frames de cuerpos
                _bodySource = _sensor.BodyFrameSource;
                _bodyReader = _bodySource.OpenReader();
                _bodyReader.FrameArrived += BodyReader_FrameArrived;

                // Obtener fuente facial, lector facial, handler para eventos de frames faciales.
                _faceSource = new HighDefinitionFaceFrameSource(_sensor);
                _faceReader = _faceSource.OpenReader();
                _faceReader.FrameArrived += FaceReader_FrameArrived;

                //Añadimos el reader de gestos faciales. y el handler.
                _faceFrameSource = new FaceFrameSource(this._sensor, 0,
                                                       FaceFrameFeatures.BoundingBoxInColorSpace |
                                                       FaceFrameFeatures.FaceEngagement |
                                                       FaceFrameFeatures.Glasses |
                                                       FaceFrameFeatures.Happy |
                                                       FaceFrameFeatures.LeftEyeClosed |
                                                       FaceFrameFeatures.MouthOpen |
                                                       FaceFrameFeatures.PointsInColorSpace | 
                                                       FaceFrameFeatures.RightEyeClosed
                                                       );
                _faceFrameReader = this._faceFrameSource.OpenReader();
                _faceFrameReader.FrameArrived += FaceFrameReader_FrameArrived;


                // Crear FaceModel, FaceAlignmet
                _faceModel = new FaceModel();
                _faceAlignment = new FaceAlignment();

                // Abrir el sensor.
                _sensor.Open();
                // Asignar el multireader
                multiSourceReader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body);
                multiSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }


        private void FaceFrameReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    evaluateFaceExpression(frame);
                }
            }
        }

        private void evaluateFaceExpression(FaceFrame f)
        {
            if (f.FaceFrameResult != null)
            {
                if (f.FaceFrameResult.FaceProperties[FaceProperty.RightEyeClosed] == DetectionResult.Yes)
                {
                    canvas.Background = new SolidColorBrush(Colors.LightGray);
                }
                if (f.FaceFrameResult.FaceProperties[FaceProperty.LeftEyeClosed] == DetectionResult.Yes)
                {
                    canvas.Background = new SolidColorBrush(Colors.LightPink);
                }
                if (f.FaceFrameResult.FaceProperties[FaceProperty.MouthOpen] == DetectionResult.Yes)
                {
                    canvas.Background = new SolidColorBrush(Colors.LightGreen);
                }
                if (f.FaceFrameResult.FaceProperties[FaceProperty.Happy] == DetectionResult.Yes)
                {
                    canvas.Background = new SolidColorBrush(Colors.LightYellow);
                }
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Get a reference to the multi-frame
            var reference = e.FrameReference.AcquireFrame();

            // Open infrared frame
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    //displayColorImage.Source = null;
                    displayColorImage.Source = this.ToBitmap(frame);
                }
            }
        }

        private ImageSource ToBitmap(InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort[] frameData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(frameData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
            {
                ushort ir = frameData[infraredIndex];

                byte intensity = (byte)(ir >> 7);

                pixels[colorIndex++] = (byte)(intensity / 1); // Blue
                pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                pixels[colorIndex++] = (byte)(intensity / 0.4); // Red

                colorIndex++;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.multiSourceReader != null)
            {
                this.multiSourceReader.Dispose();
            }
            if (_faceModel != null)
            {
                _faceModel.Dispose();
                _faceModel = null;
            }

            GC.SuppressFinalize(this);
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            /// <summary>
            /// Tarea a realizar por alumno
            /// Adquirir frame
            /// Crear un array de bodies
            /// GetAndRefreshBodyData
            /// Asignar trackingid del body a la fuente de datos faciales
            /// </summary>
            /// /////////////////////////////////////////////////////////////////////////////////////////////////
            /// 

            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {

                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();

                if (!_faceSource.IsTrackingIdValid)
                {
                    if (body != null)
                    {
                        _faceSource.TrackingId = body.TrackingId;
                    }
                }

                if (!_faceFrameSource.IsTrackingIdValid)
                {
                    if (body != null)
                    {
                        _faceFrameSource.TrackingId = body.TrackingId;
                    }
                }
            }
        }
        private void FaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            /// <summary>
            /// Tarea a realizar por alumno
            /// Procesar frame facial
            /// </summary>
            /// /////////////////////////////////////////////////////////////////////////////////////////////////

            using (var frame = e.FrameReference.AcquireFrame())
            {
                if(frame != null && frame.IsFaceTracked)
                {
                    frame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                    RenderFacePoints();
                }
            }
        }

        
        private void RenderFacePoints()
        {
            /// <summary>
            /// Tarea a realizar por alumno
            /// Renderizar nube de puntos faciales
            /// </summary>
            /// /////////////////////////////////////////////////////////////////////////////////////////////////
            /// 
            Ellipse ellipse;
            if (_faceModel == null) return;
            var vertices = _faceModel.CalculateVerticesForAlignment(_faceAlignment);
            bool P1 = false;
            if (vertices.Count > 0)
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    DepthSpacePoint point = _sensor.CoordinateMapper.MapCameraPointToDepthSpace(vertices[i]);
                    if (!(float.IsInfinity(point.X)) && !(float.IsInfinity(point.Y)))
                    {
                        ellipse = _points[i];
                        Canvas.SetLeft(ellipse, point.X);
                        Canvas.SetTop(ellipse, point.Y);
                    }
                }
            }
        }
    }
}
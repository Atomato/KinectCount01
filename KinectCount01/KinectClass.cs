using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Windows.Media.Media3D; //PresentationCore 어셈블리 참조
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace KinectCount01
{
    public enum WorkoutType
    {
        Squat = 0,
        BicepsCurl = 1
    }

    class KinectClass
    {
        #region Member Variables
        private KinectSensor _KinectDevice;
        private short[] _DepthPixelData;
        private Skeleton[] _FrameSkeletons;
        public WorkoutType currentWorkoutType = WorkoutType.Squat;
        public bool isDepthEncodingReady = false;
        public bool isSkeletonFrameReady = false;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine;
        public bool isInitialize = false;
        #endregion Member Variables

        #region Constructor
        public KinectClass()
        {
            
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
            
            JointPositions = new Point3D[20];
            
        }
        #endregion Constructor

        #region Methods
        /// <summary>
        /// 키넥트 status가 바뀌었을 때의 이벤트 핸들러
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.                    
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }

        /// <summary>
        /// 스켈레톤 프레임이 준비되었을 때 실행
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectDevice_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                using (SkeletonFrame skeletonframe = e.OpenSkeletonFrame())
                {
                    DepthEncoding(KinectDevice, depthFrame);
                    SkeletonEncoding(KinectDevice, skeletonframe);
                }
            }
        }

        private void DepthEncoding(KinectSensor kinectDevice, DepthImageFrame depthFrame)
        {
            if (kinectDevice != null && depthFrame != null)
            {
                depthFrame.CopyPixelDataTo(_DepthPixelData);
                int playerIndex;
                int j;
                int k;
                const int bitPerByte = 8;

                for (int i = 0; i < _DepthPixelData.Length; i++)
                {
                    playerIndex = _DepthPixelData[i] & DepthImageFrame.PlayerIndexBitmask;
                    j = i / bitPerByte;
                    k = i % bitPerByte;

                    if (playerIndex != 0)
                    {
                        EncodedDepth[j] |= (byte) (1 << k);
                    }
                    else
                    {
                        EncodedDepth[j] &= (byte)(~(1 << k));
                    }
                }
                isDepthEncodingReady = true;
            }
        }

        private void SkeletonEncoding(KinectSensor kinectDevice, SkeletonFrame skeletonframe)
        {
            if (kinectDevice != null && skeletonframe != null)
            {
                skeletonframe.CopySkeletonDataTo(this._FrameSkeletons);
                Skeleton skeleton = GetPrimarySkeleton(this._FrameSkeletons);

                if (skeleton != null)
                {
                    // 모든 관절 위치 
                    foreach (var value in Enum.GetValues(typeof(JointType)))
                    {
                        JointPositions[(int)value] = GetJointPoint(skeleton.Joints[(JointType)value]);
                    }

                    WorkoutCounting.Count(JointPositions, currentWorkoutType);
                    isSkeletonFrameReady = true;
                }                
            }
        }

        /// <summary>
        /// 가장 가까운 스켈레톤 선택
        /// </summary>
        /// <param name="skeletons"></param>
        /// <returns></returns>
        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;

            if (skeletons != null)
            {
                //Find the closest skeleton       
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }

            return skeleton;
        }

        /// <summary>
        /// 스켈레톤 좌표를 뎁스 좌표로 변환
        /// </summary>
        /// <param name="joint"></param>
        /// <returns></returns>
        private Point3D GetJointPoint(Joint joint)
        {
            DepthImagePoint point = this.KinectDevice.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, this.KinectDevice.DepthStream.Format);
            double pointx = (double)point.X;
            double pointy = (double)point.Y;
            double pointz = (double)point.Depth;

            return new Point3D(pointx, pointy, pointz);
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            var result = e.Result;
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.25;

            if (result.Confidence >= ConfidenceThreshold)
            {
                if (result.Words[0].Text == "initialize")
                {
                    Console.WriteLine("speech recognized: initialize");
                    WorkoutCounting.Initialize();
                    isInitialize = true;
                }
                if (result.Words[0].Text == "change")
                {
                    var workoutType = result.Words[2].Text;
                    switch (workoutType)
                    {
                        case "squat":
                            currentWorkoutType = WorkoutType.Squat;
                            WorkoutCounting.Initialize();
                            isInitialize = true;
                            Console.WriteLine("speech recognized: change to squat");
                            break;
                        case "biceps":
                            currentWorkoutType = WorkoutType.BicepsCurl;
                            WorkoutCounting.Initialize();
                            isInitialize = true;
                            Console.WriteLine("speech recognized: change to biceps curl");
                            break;
                        default:
                            break;
                    }
                }
            }

        }

        private void CreateGrammars(RecognizerInfo ri)
        {
            var workoutType = new Choices();
            workoutType.Add("squat");
            workoutType.Add("biceps curl");

            var gb = new GrammarBuilder();
            gb.Culture = ri.Culture;
            gb.Append("change");
            gb.Append("to");
            gb.Append(workoutType);

            var g = new Grammar(gb);
            speechEngine.LoadGrammar(g);
            
            var init = new GrammarBuilder();
            init.Culture = ri.Culture;
            init.Append("initialize");

            var i = new Grammar(init);
            speechEngine.LoadGrammar(i);
        }
        #endregion Methods

        #region Properties
        public KinectSensor KinectDevice
        {
            get { return this._KinectDevice; }
            set
            {
                if (this._KinectDevice != value)
                {
                    //Uninitialize
                    if (this._KinectDevice != null)
                    {
                        this._KinectDevice.Stop();
                        this._KinectDevice.AllFramesReady -= KinectDevice_AllFramesReady;
                        _KinectDevice.ColorStream.Disable();
                        _KinectDevice.DepthStream.Disable();
                        this._KinectDevice.SkeletonStream.Disable();
                        this._FrameSkeletons = null;

                        _KinectDevice.AudioSource.Stop();
                        this.speechEngine.SpeechRecognized -= SpeechRecognized;
                        //this.speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                        this.speechEngine.RecognizeAsyncStop();
                    }

                    this._KinectDevice = value;

                    //Initialize
                    if (this._KinectDevice != null)
                    {
                        if (this._KinectDevice.Status == KinectStatus.Connected)
                        {
                            _KinectDevice.ColorStream.Enable();
                            _KinectDevice.DepthStream.Enable(DepthImageFormat.Resolution80x60Fps30);
                            _KinectDevice.SkeletonStream.Enable();
                            DepthImageStream depthStream = this._KinectDevice.DepthStream;
                            this._FrameSkeletons = new Skeleton[this._KinectDevice.SkeletonStream.FrameSkeletonArrayLength];
                            _DepthPixelData = new short[depthStream.FramePixelDataLength];
                            EncodedDepth = new byte[depthStream.FramePixelDataLength / 8];
                            this.KinectDevice.AllFramesReady += KinectDevice_AllFramesReady;
                            this._KinectDevice.Start();

                            RecognizerInfo ri = GetKinectRecognizer();
                            if (null != ri)
                            {
                                speechEngine = new SpeechRecognitionEngine(ri.Id);

                                CreateGrammars(ri);

                                speechEngine.SpeechRecognized += SpeechRecognized;
                                //speechEngine.SpeechRecognitionRejected += SpeechRejected;

                                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                                // This will prevent recognition accuracy from degrading over time.
                                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                                speechEngine.SetInputToAudioStream(
                                    _KinectDevice.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                            }
                        }
                    }
                }
            }
        }
        public byte[] EncodedDepth { get; set; }
        public Point3D[] JointPositions { get; set; }
        #endregion Properties
    }
}

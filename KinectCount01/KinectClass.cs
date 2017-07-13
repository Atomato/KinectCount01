using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Windows.Media.Media3D; //PresentationCore 어셈블리 참조

namespace KinectCount01
{
    class KinectClass
    {
        #region Member Variables
        private KinectSensor _KinectDevice;
        private short[] _DepthPixelData;
        private Skeleton[] _FrameSkeletons;
        public Boolean IsSkeletonFrameReady = false;
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
                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    DepthEncoding(KinectDevice, depthFrame);

                    if (frame != null)
                    {
                        Skeleton skeleton;

                        frame.CopySkeletonDataTo(this._FrameSkeletons);

                        for (int i = 0; i < this._FrameSkeletons.Length; i++)
                        {
                            skeleton = this._FrameSkeletons[i];

                            // 스켈레톤 트래킹 되고 있는 상태인가
                            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                // 모든 관절 위치 
                                foreach (var value in Enum.GetValues(typeof(JointType)))
                                {
                                    JointPositions[(int)value] = GetJointPoint(skeleton.Joints[(JointType)value]);
                                }

                                // 머리 좌표와 엉덩이 좌표로 카운팅함
                                SquatCount.Count(JointPositions[(int)JointType.Head], JointPositions[(int)JointType.HipCenter]);
                                IsSkeletonFrameReady = true;
                            }
                        }
                    }
                }
            }
        }

        private void DepthEncoding(KinectSensor kinectDevice, DepthImageFrame depthFrame)
        {
            if (kinectDevice != null && depthFrame != null)
            {
                depthFrame.CopyPixelDataTo(_DepthPixelData);
                int playerIndex;
                const int bitPerByte = 8;
                byte[] encodedDepth = new byte[depthFrame.Width * depthFrame.Height / bitPerByte];

                for (int i = 0; i < _DepthPixelData.Length; i++)
                {
                    playerIndex = _DepthPixelData[i] & DepthImageFrame.PlayerIndexBitmask;
                    int j = i / bitPerByte;

                    if (playerIndex != 0)
                    {
                        encodedDepth[j] |= (byte) (1 << j);
                    }
                }
            }
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
                    }

                    this._KinectDevice = value;

                    //Initialize
                    if (this._KinectDevice != null)
                    {
                        if (this._KinectDevice.Status == KinectStatus.Connected)
                        {
                            _KinectDevice.ColorStream.Enable();
                            _KinectDevice.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                            _KinectDevice.SkeletonStream.Enable();
                            DepthImageStream depthStream = this._KinectDevice.DepthStream;
                            this._FrameSkeletons = new Skeleton[this._KinectDevice.SkeletonStream.FrameSkeletonArrayLength];
                            _DepthPixelData = new short[depthStream.FramePixelDataLength];
                            this.KinectDevice.AllFramesReady += KinectDevice_AllFramesReady;
                            this._KinectDevice.Start();
                        }
                    }
                }
            }
        }
        public Point3D[] JointPositions { get; set; }
        #endregion Properties
    }
}

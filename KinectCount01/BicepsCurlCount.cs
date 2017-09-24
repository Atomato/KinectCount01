using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media.Media3D; //PresentationCore 어셈블리 참조

namespace KinectCount01
{
    class BicepsCurlCount
    {
        #region Member Variables
        private static int InitialSetCount;
        private static double HeadPoint;
        private static double HipCenterPoint;
        private static int SummationCount;
        private static bool IsHandUp;
        private static int CountNumber;
        private static double ThresholdPoint;
        public static byte[] countBytes = new byte[2];
        public static bool IsCountUp;
        #endregion Member Variables

        #region Methods
        public static void Count(Point3D Head, Point3D HipCenter, Point3D HandRight)
        {
            if (InitialSetCount < 150)
            {
                if (InitialSetCount < 120) InitialSetCount++; // 스켈레톤 만들어지고 잠시 대기
                else // 카운트 30 동안의 머리와 엉덩이 좌표 평균 구함
                {
                    HeadPoint += Head.Y;
                    HipCenterPoint += HipCenter.Y;
                    SummationCount++;
                    InitialSetCount++;
                    if (InitialSetCount == 150)
                    {
                        HeadPoint = HeadPoint / SummationCount;
                        HipCenterPoint = HipCenterPoint / SummationCount;

                        ThresholdPoint = HipCenterPoint; // 카운트 문턱
                        Console.WriteLine($"Count Ready and threshold point is {ThresholdPoint,0:F0}");
                        countBytes = BitConverter.GetBytes((short) ThresholdPoint);
                    }
                }
            }
            else
            {
                CheckCount(HandRight); // 머리, 엉덩이 좌표 초기화 끝나면 카운트 시작
            }
        }

        private static void CheckCount(Point3D HandRight)
        {
            if (HandRight.Y < ThresholdPoint) // 오른손이 일정 높이 위로 카면 카운트 업
            {
                if (!IsHandUp)
                {
                    CountNumber++;
                    Console.WriteLine($"The current count number is {CountNumber}");
                    IsCountUp = true;
                    IsHandUp = true;
                }
            }
            else
            {
                if (IsHandUp)
                {
                    IsHandUp = false;
                }
            }
        }
        public static void InitializeThreshold()
        {
            InitialSetCount = 0;
            HeadPoint = 0;
            HipCenterPoint = 0;
            SummationCount = 0;
            ThresholdPoint = 0;
            countBytes = BitConverter.GetBytes((short) ThresholdPoint);
        }
        #endregion Methods
    }
}

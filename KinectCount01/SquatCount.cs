using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media.Media3D; //PresentationCore 어셈블리 참조

namespace KinectCount01
{
    public static class SquatCount
    {
        #region Member Variables
        private static int InitialSetCount;
        private static double HeadPoint;
        private static double HipCenterPoint;
        private static int SummationCount;
        private static bool IsHeadDown;
        private static int CountNumber;
        private static double ThresholdPoint;
        public static byte[] countBytes = new byte[2];
        public static bool IsCountUp;
        #endregion Member Variables

        #region Methods
        public static void Count(Point3D Head, Point3D HipCenter)
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

                        ThresholdPoint = (HeadPoint + HipCenterPoint)/2; // 카운트 문턱
                        Console.WriteLine($"Count Ready and threshold point is {ThresholdPoint,0:F0}");
                        countBytes = BitConverter.GetBytes((short)SquatCount.ThresholdPoint);
                    }
                }
            }
            else
            {
                CheckCount(Head); // 머리, 엉덩이 좌표 초기화 끝나면 카운트 시작
            }
        }

        private static void CheckCount(Point3D Head)
        {
            if (Head.Y > ThresholdPoint) // 머리가 일정 높이 아래로 카면 카운트 업
            {
                if (!IsHeadDown)
                {
                    CountNumber++;
                    //Console.WriteLine($"The current count number is {SquatCount.CountNumber}");
                    Console.WriteLine($"Head is at {Head}");
                    IsCountUp = true;
                    IsHeadDown = true;
                }
            }
            else
            {
                if (IsHeadDown)
                {
                    IsHeadDown = false;
                    Console.WriteLine($"Head is at {Head}");
                }
            }
        }
        #endregion Methods
    }
}

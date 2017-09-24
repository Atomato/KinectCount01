using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Windows.Media.Media3D; //PresentationCore 어셈블리 참조

namespace KinectCount01
{
    static class WorkoutCounting
    {
        #region Member Variables
        private static int initialSetCount;
        private static double headPoint;
        private static double hipCenterPoint;
        private static int summationCount;
        private static double thresholdPoint;
        private static bool isOverThreshold;
        private static bool workoutDirection; //true이면 아래로 움직일때 카운트. false이면 반대
        private static int countNum;
        public static byte[] countBytes = new byte[2];
        public static bool isCountUp;
        #endregion Member Variables

        #region Methods
        public static void Count(Point3D[] jointPositions, WorkoutType workoutType)
        {
            switch (workoutType)
            {
                case WorkoutType.Squat:
                    SquatCount(jointPositions[(int)JointType.Head], jointPositions[(int)JointType.HipCenter]);
                    break;
                case WorkoutType.BicepsCurl:
                    BicepsCurlCount(jointPositions[(int)JointType.Head], jointPositions[(int)JointType.HipCenter],
                            jointPositions[(int)JointType.HandRight]);
                    break;
                default:
                    break;
            }
        }

        private static void SquatCount(Point3D head, Point3D hipCenter)
        {
            if (initialSetCount < 150)
            {
                if (initialSetCount < 120) initialSetCount++; // 스켈레톤 만들어지고 잠시 대기
                else // 카운트 30 동안의 머리와 엉덩이 좌표 평균 구함
                {
                    headPoint += head.Y;
                    hipCenterPoint += hipCenter.Y;
                    summationCount++;
                    initialSetCount++;
                    if (initialSetCount == 150)
                    {
                        headPoint = headPoint / summationCount;
                        hipCenterPoint = hipCenterPoint / summationCount;

                        thresholdPoint = (headPoint + hipCenterPoint) / 2; // 카운트 문턱
                        Console.WriteLine($"Ready to count and threshold point is {thresholdPoint,0:F0}");
                        countBytes = BitConverter.GetBytes((short)thresholdPoint);

                        workoutDirection = true;
                    }
                }
            }
            else
            {
                CheckCount(head, workoutDirection); // 머리, 엉덩이 좌표 초기화 끝나면 카운트 시작
            }
        }

        private static void BicepsCurlCount(Point3D head, Point3D hipCenter, Point3D handRight)
        {
            if (initialSetCount < 150)
            {
                if (initialSetCount < 120) initialSetCount++; // 스켈레톤 만들어지고 잠시 대기
                else // 카운트 30 동안의 머리와 엉덩이 좌표 평균 구함
                {
                    headPoint += head.Y;
                    hipCenterPoint += hipCenter.Y;
                    summationCount++;
                    initialSetCount++;
                    if (initialSetCount == 150)
                    {
                        headPoint = headPoint / summationCount;
                        hipCenterPoint = hipCenterPoint / summationCount;

                        thresholdPoint = (headPoint + 3*hipCenterPoint) / 4; // 카운트 문턱
                        Console.WriteLine($"Ready to count and threshold point is {thresholdPoint,0:F0}");
                        countBytes = BitConverter.GetBytes((short)thresholdPoint);

                        workoutDirection = false;
                    }
                }
            }
            else
            {
                CheckCount(handRight, workoutDirection); // 머리, 엉덩이 좌표 초기화 끝나면 카운트 시작
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coreJoint">운동할 때 움직이는 조인트</param>
        /// <param name="workoutDirection">어느 방향으로 움직일 때 카운트 할 지</param>
        private static void CheckCount(Point3D coreJoint, bool workoutDirection)
        {
            if ((coreJoint.Y > thresholdPoint) == workoutDirection) 
            {
                if (!isOverThreshold) //코어 조인트가 쓰레쉬홀드를 넘는 순간 카운트 업
                {
                    countNum++;
                    Console.WriteLine($"The current count number is {countNum}");
                    isCountUp = true;
                    isOverThreshold = true;
                }
            }
            else
            {
                isOverThreshold = false;
            }
        }

        public static void Initialize()
        {
            initialSetCount = 121;
            headPoint = 0;
            hipCenterPoint = 0;
            summationCount = 0;
            countNum = 0;
            thresholdPoint = 0;
            countBytes = BitConverter.GetBytes((short) thresholdPoint);
        }
        #endregion Methods
    }
}

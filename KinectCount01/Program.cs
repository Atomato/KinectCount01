﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;
using System.Windows.Media.Media3D; //PresentationCore 어셈블리 참조

using System.Net;
using System.Net.Sockets;

namespace KinectCount01
{
    class Program
    {
        static void Main(string[] args)
        {
            KinectClass sensor = new KinectClass();

            TcpListener server = null;
            try
            {
                // Set the TcpListener on port 8200.
                Int32 port = 8200;
                IPAddress localAddr = IPAddress.Parse("192.168.0.9");

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // buffer index
                const int systemBytesNum = 1;
                const int depthOffset = systemBytesNum;
                int skelOffset = depthOffset + sensor.EncodedDepth.Length;
                int countOffset = skelOffset + (2 * sensor.JointPositions.Length);
                int sendMsgLength = countOffset + 1;

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;
                StringBuilder txData = new StringBuilder();
                Byte[] sendMsg = new Byte[sendMsgLength];
                Byte[] skelBytes;
                Byte[] rejectMsg = new Byte[1];
                Byte[] systemBytes = new byte[1];
                rejectMsg[0] = 0xFF; //데이터 보낼 준비가 안될 때 보낼 메시지

                // Enter the listening loop.
                while (true)
                {
                    Console.WriteLine("Waiting for a connection... ");

                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;
                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = Encoding.ASCII.GetString(bytes, 0, i);

                        // 클라이언트에서 "y\n"를 보내면 조인트 좌표 보냄
                        if (data.Equals("y\n"))
                        {
                            if (sensor.isDepthEncodingReady && sensor.isSkeletonFrameReady)
                            {
                                systemBytes[0] = (byte)sensor.currentWorkoutType;
                                if (sensor.isInitialize)
                                {
                                    systemBytes[0] |= 0x80;
                                    Buffer.BlockCopy(systemBytes, 0, sendMsg, 0, 1);
                                    sensor.isInitialize = false;
                                }
                                else
                                {
                                    systemBytes[0] &= 0x7F;
                                    Buffer.BlockCopy(systemBytes, 0, sendMsg, 0, 1);
                                }


                                Buffer.BlockCopy(sensor.EncodedDepth, 0, sendMsg, depthOffset, sensor.EncodedDepth.Length);
                                
                                // 클라이언트에 보낼 스켈레톤 데이터
                                foreach (var value in Enum.GetValues(typeof(JointType)))
                                {
                                    skelBytes = BitConverter.GetBytes((short) sensor.JointPositions[(int)value].X);
                                    Buffer.BlockCopy(skelBytes, 0, sendMsg, skelOffset + 2*(int)value, 1);

                                    skelBytes = BitConverter.GetBytes((short) sensor.JointPositions[(int)value].Y);
                                    Buffer.BlockCopy(skelBytes, 0, sendMsg, skelOffset + 2*(int)value + 1, 1);
                                }

                                // 카운트 업일 때
                                if (WorkoutCounting.isCountUp)
                                {
                                    WorkoutCounting.countBytes[0] |= 0x80; //마지막 비트가 카운트 업을 나타냄
                                    Buffer.BlockCopy(WorkoutCounting.countBytes, 0, sendMsg, countOffset, 1);
                                    WorkoutCounting.countBytes[0] &= 0x7F;
                                    WorkoutCounting.isCountUp = false;
                                }
                                else
                                {
                                    Buffer.BlockCopy(WorkoutCounting.countBytes, 0, sendMsg, countOffset, 1);
                                } 
                                
                                stream.Write(sendMsg, 0, sendMsg.Length);
                                sensor.isDepthEncodingReady = false;
                                sensor.isSkeletonFrameReady = false;
                            }
                            else
                            {
                                stream.Write(rejectMsg, 0, rejectMsg.Length); //데이터 준비 안됨
                            }
                        }
                    }

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine("IOException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }


            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
    }
}

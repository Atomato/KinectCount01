using System;
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

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;
                StringBuilder txData = new StringBuilder();
                Byte[] sendMsg = new Byte[640];
                Byte[] skelBytes;

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
                            /*
                            if (sensor.IsSkeletonFrameReady)
                            {
                                // 클라이언트에 보낼 데이터. "/"은 구분 단위
                                foreach (var value in Enum.GetValues(typeof(JointType)))
                                {
                                    txData.Append($"{sensor.JointPositions[(int)value].X}/{sensor.JointPositions[(int)value].Y}/");
                                }
                                txData.Append($"{SquatCount.ThresholdPoint,0:F0}/{SquatCount.CountNumber}\n");

                                byte[] msg = Encoding.ASCII.GetBytes(txData.ToString());

                                // Send back a response.
                                stream.Write(msg, 0, msg.Length);

                                txData.Clear(); // StringBuilder 데이터 클리어

                                // 카운트 업하면 콘솔에 표시
                                if (SquatCount.IsCountUp)
                                {
                                    Console.WriteLine($"The current count number is {SquatCount.CountNumber}");
                                    SquatCount.IsCountUp = false;
                                    SquatCount.CountNumber--;
                                }
                                sensor.IsSkeletonFrameReady = false;
                            }
                            else
                            {
                                byte[] msg = Encoding.ASCII.GetBytes("n\n");
                                stream.Write(msg, 0, msg.Length);
                            }
                            */

                            if (sensor.IsDepthEncodingReady && sensor.IsSkeletonFrameReady)
                            {
                                Buffer.BlockCopy(sensor.EncodedDepth, 0, sendMsg, 0, sensor.EncodedDepth.Length);
                                

                                // 클라이언트에 보낼 스켈레톤 데이터
                                foreach (var value in Enum.GetValues(typeof(JointType)))
                                {
                                    skelBytes = BitConverter.GetBytes((short) sensor.JointPositions[(int)value].X);
                                    Buffer.BlockCopy(skelBytes, 0, sendMsg, 600 + 2 * (int)value, 1);

                                    skelBytes = BitConverter.GetBytes((short)sensor.JointPositions[(int)value].Y);
                                    Buffer.BlockCopy(skelBytes, 0, sendMsg, 600 + 2 * (int)value +1, 1);
                                }

                                // 카운트 업하면 콘솔에 표시
                                if (SquatCount.IsCountUp)
                                {
                                    Console.WriteLine($"The current count number is {SquatCount.CountNumber}");
                                    SquatCount.IsCountUp = false;
                                    SquatCount.CountNumber--;
                                }

                                stream.Write(sendMsg, 0, sendMsg.Length);
                                sensor.IsDepthEncodingReady = false;
                                sensor.IsSkeletonFrameReady = false;
                            }
                            else
                            {
                                byte[] msg = Encoding.ASCII.GetBytes("n\n");
                                stream.Write(msg, 0, msg.Length);
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

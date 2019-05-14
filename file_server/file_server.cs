using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Transportlaget;
using Library;

namespace Application
{
    class file_server
    {
        /// <summary>
        /// The BUFSIZE
        /// </summary>
        private const int BUFSIZE = 1000;
        private const string APP = "FILE_SERVER";

        /// <summary>
        /// Initializes a new instance of the <see cref="file_server"/> class.
        /// </summary>
        private file_server()
        {
            Transport transport = new Transport(BUFSIZE, APP);
            Console.WriteLine("Server starts...");
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[10000];
                    transport.receive(ref buffer);
                    string fileName = LIB.extractFileName(Encoding.ASCII.GetString(buffer));
                    long fileLength = LIB.check_File_Exists(fileName);
                    Console.WriteLine(fileName);
                    if (fileLength == 0) //File not found
                    {
                        string errorMessage = "DoesNotExist";
                        Console.WriteLine(errorMessage);
                        byte[] messageBytes = Encoding.ASCII.GetBytes(errorMessage);
                        transport.send(messageBytes, messageBytes.Length);
                    }
                    else // File found
                    {
                        Console.WriteLine("File length sent: " + fileLength);
                        byte[] fileLengthBytes = Encoding.ASCII.GetBytes(fileLength.ToString());
                        transport.send(fileLengthBytes, fileLengthBytes.Length);
                        Thread.Sleep(1);
                        sendFile(fileName, fileLength, transport);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                finally
                {

                }
            }
        }

        /// <summary>
        /// Sends the file.
        /// </summary>
        /// <param name='fileName'>
        /// File name.
        /// </param>
        /// <param name='fileSize'>
        /// File size.
        /// </param>
        /// <param name='tl'>
        /// Tl.
        /// </param>
        private void sendFile(String fileName, long fileSize, Transport transport)
        {

            FileStream Fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(Fs.Length) / Convert.ToDouble(BUFSIZE)));
            int TotalLength = (int)Fs.Length;
            int bytesSent = 0;
            for (int i = 0; i < NoOfPackets; ++i)
            {
                if ((TotalLength - bytesSent) > BUFSIZE)
                {
                    byte[] sendingBuffer = new byte[BUFSIZE];
                    Fs.Read(sendingBuffer, 0, BUFSIZE);
                    transport.send(sendingBuffer, sendingBuffer.Length);
                    bytesSent += BUFSIZE;
                }
                else
                {
                    byte[] sendingBuffer = new byte[TotalLength - bytesSent];
                    Fs.Read(sendingBuffer, 0, sendingBuffer.Length);
                    transport.send(sendingBuffer, sendingBuffer.Length);
                    bytesSent += sendingBuffer.Length;
                }

                Console.WriteLine("Send packet no. " + i);
                Console.WriteLine("Sending Bytes: " + bytesSent);
            }
            Fs.Close();
        }

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name='args'>
        /// The command-line arguments.
        /// </param>
        public static void Main(string[] args)
        {
            file_server server = new file_server();
            while (true)
            {

            }
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Transportlaget;
using Library;

namespace Application
{
	class file_client
	{
		/// <summary>
		/// The BUFSIZE.
		/// </summary>
		private const int BUFSIZE = 1000;
		private const string APP = "FILE_CLIENT";

        /// <summary>
        /// Initializes a new instance of the <see cref="file_client"/> class.
        /// 
        /// file_client metoden opretter en peer-to-peer forbindelse
        /// Sender en forspÃ¸rgsel for en bestemt fil om denne findes pÃ¥ serveren
        /// Modtager filen hvis denne findes eller en besked om at den ikke findes (jvf. protokol beskrivelse)
        /// Lukker alle streams og den modtagede fil
        /// Udskriver en fejl-meddelelse hvis ikke antal argumenter er rigtige
        /// </summary>
        /// <param name='args'>
        /// Filnavn med evtuelle sti.
        /// </param>
        private file_client(String[] args)
        {
            string fName;
            while (true)
            {
                Console.WriteLine("Enter the name of the desired File: ");
                fName = Console.ReadLine();

                Transport transportLayer = new Transport(BUFSIZE, APP);

                receiveFile(fName, transportLayer);

            }
        }

        /// <summary>
        /// Receives the file.
        /// </summary>
        /// <param name='fileName'>
        /// File name.
        /// </param>
        /// <param name='transport'>
        /// Transportlaget
        /// </param>
        private void receiveFile(String fileName, Transport transport)
        {
            byte[] fNameInByte = Encoding.ASCII.GetBytes(fileName);
            transport.send(fNameInByte, fNameInByte.Length);
            byte[] ServerAnswer = new byte[BUFSIZE];
            transport.receive(ref ServerAnswer);
            string RecievedString = Encoding.ASCII.GetString(ServerAnswer);
            if (RecievedString.Contains("DoesNotExist"))
            {
                Console.WriteLine("Shit didnt exist");
            }
            else //Received file length
            {
                int fileLength = int.Parse(LIB.extractFileName(RecievedString));
                FileStream Fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
                int NoOfPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(fileLength) / Convert.ToDouble(BUFSIZE)));

                int bytesReceived = 0;
                for (int i = 0; i < NoOfPackets; ++i)
                {
                    if ((fileLength - bytesReceived) > BUFSIZE)
                    {
                        byte[] receiveBuffer = new byte[BUFSIZE];
                        transport.receive(ref receiveBuffer);
                        Fs.Write(receiveBuffer, 0, receiveBuffer.Length);
                        bytesReceived += BUFSIZE;
                    }
                    else
                    {
                        byte[] receiveBuffer = new byte[fileLength - bytesReceived];
                        transport.receive(ref receiveBuffer);
                        Fs.Write(receiveBuffer, 0, fileLength-bytesReceived);
                        bytesReceived += receiveBuffer.Length;
                    }
					Console.WriteLine("Received packet no. " + i);
					Console.WriteLine("Recieved Bytes: " + bytesReceived);
                }

                Fs.Close();
                Console.WriteLine("Done med din lortefil");
            }


        }

        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name='args'>
        /// First argument: Filname
        /// </param>
        public static void Main (string[] args)
		{
            file_client client = new file_client(args);
        }
	}
}
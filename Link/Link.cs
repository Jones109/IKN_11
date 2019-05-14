using System;
using System.IO.Ports;
using System.Text;

/// <summary>
/// Link.
/// </summary>
namespace Linklaget
{
	/// <summary>
	/// Link.
	/// </summary>
	public class Link
	{
		/// <summary>
		/// The DELIMITE for slip protocol.
		/// </summary>
		const byte DELIMITER = (byte)'A';
		/// <summary>
		/// The buffer for link.
		/// </summary>
		private byte[] buffer;
		/// <summary>
		/// The serial port.
		/// </summary>
		SerialPort serialPort;

		/// <summary>
		/// Initializes a new instance of the <see cref="link"/> class.
		/// </summary>
		public Link (int BUFSIZE, string APP)
		{
			// Create a new SerialPort object with default settings.
			#if DEBUG
				if(APP.Equals("FILE_SERVER"))
				{
					serialPort = new SerialPort("/dev/ttyS1",115200,Parity.None,8,StopBits.One);
				}
				else
				{
					serialPort = new SerialPort("/dev/ttyS1",115200,Parity.None,8,StopBits.One);
				}
			#else
				serialPort = new SerialPort("/dev/ttyS1",115200,Parity.None,8,StopBits.One);
			#endif
			if(!serialPort.IsOpen)
				serialPort.Open();

			buffer = new byte[(BUFSIZE*2)];

			// Uncomment the next line to use timeout
			//serialPort.ReadTimeout = 500;

			serialPort.DiscardInBuffer ();
			serialPort.DiscardOutBuffer ();
		}

		/// <summary>
		/// Send the specified buf and size.
		/// </summary>
		/// <param name='buf'>
		/// Buffer.
		/// </param>
		/// <param name='size'>
		/// Size.
		/// </param>
		public void send (byte[] buf, int size)
        {
            int numberOfAOrB = 0;
            //string dataToSend = Encoding.ASCII.GetString(buf);
            for (int i = 0; i < buf.Length; ++i)
            {
				if (buf[i] == (byte)'A' | buf[i] == (byte)'B')
                    numberOfAOrB++;
            }
            byte[] sendBuf = new byte[size + 2 + numberOfAOrB];
            int x = 0;
            sendBuf[0] = (byte) 'A';
            for (int i=1; i < sendBuf.Length-1; i++)
            {
                if (buf[i-1-x]==(byte)'A')
                {
                    sendBuf[i] = (byte) 'B';
                    sendBuf[++i] = (byte) 'C';
                    x++;
                }else if (buf[i-1-x]==(byte)'B')
                {
                    sendBuf[i] = (byte) 'B';
                    sendBuf[++i] = (byte) 'D';
                    x++;
                }
                else
                {
                    sendBuf[i] = buf[i-1-x];
                }
            }

            sendBuf[sendBuf.Length-1] = (byte) 'A';


			serialPort.Write(sendBuf, 0, size+2+numberOfAOrB);
		}

		/// <summary>
		/// Receive the specified buf and size.
		/// </summary>
		/// <param name='buf'>
		/// Buffer.
		/// </param>
		/// <param name='size'>
		/// Size.
		/// </param>
		public int receive (ref byte[] buf)
		{
			while(serialPort.ReadChar()!=DELIMITER)
			{
			}
            
			byte readChar = 0;
			int i = 0;
			byte[] tempbuf = new byte[1500];
			while (readChar != DELIMITER)
			{
				readChar = (byte)serialPort.ReadByte();
                if (readChar != 'A')
                    tempbuf[i++] = readChar;      
            }

            int x = 0;
			for (int j = 0; j < i; j++){
				if(tempbuf[j+1]==(byte)'D' && tempbuf[j] == (byte)'B'){
					buf[j-x] = (byte)'B';
					j++;
                    x++;
                }else if(tempbuf[j + 1] == (byte)'C' && tempbuf[j] == (byte)'B'){
					buf[j-x] = (byte)'A';
					j++;
                    x++;
                }else{
					buf[j-x] = tempbuf[j];
				}            
			}
			return i-x;
		}
	}
}

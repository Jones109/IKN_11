using System;
using Linklaget;

/// <summary>
/// Transport.
/// </summary>
namespace Transportlaget
{
    /// <summary>
    /// Transport.
    /// </summary>
    public class Transport
    {
        /// <summary>
        /// The link.
        /// </summary>
        private Link link;
        /// <summary>
        /// The 1' complements checksum.
        /// </summary>
        private Checksum checksum;
        /// <summary>
        /// The buffer.
        /// </summary>
        private byte[] buffer;
        /// <summary>
        /// The seq no.
        /// </summary>
        private byte seqNo;
        /// <summary>
        /// The old_seq no.
        /// </summary>
        private byte old_seqNo;
        /// <summary>
        /// The error count.
        /// </summary>
        private int errorCount;
        /// <summary>
        /// The DEFAULT_SEQNO.
        /// </summary>
        private const int DEFAULT_SEQNO = 2;
        /// <summary>
        /// The data received. True = received data in receiveAck, False = not received data in receiveAck
        /// </summary>
        private bool dataReceived;
        /// <summary>
        /// The number of data the recveived.
        /// </summary>
        private int recvSize = 0;


        /// <summary>
        /// Initializes a new instance of the <see cref="Transport"/> class.
        /// </summary>
        public Transport(int BUFSIZE, string APP)
        {
            link = new Link(BUFSIZE + (int)TransSize.ACKSIZE, APP);
            checksum = new Checksum();
            buffer = new byte[BUFSIZE + (int)TransSize.ACKSIZE];
            seqNo = 0;
            old_seqNo = DEFAULT_SEQNO;
            errorCount = 0;
            dataReceived = false;
        }

        /// <summary>
        /// Receives the ack.
        /// </summary>
        /// <returns>
        /// The ack.
        /// </returns>
        private bool receiveAck()
        {
            recvSize = link.receive(ref buffer);
            dataReceived = true;

            if (recvSize == (int)TransSize.ACKSIZE)
            {
                dataReceived = false;
                if (!checksum.checkChecksum(buffer, (int)TransSize.ACKSIZE) ||
                  buffer[(int)TransCHKSUM.SEQNO] != seqNo ||
                  buffer[(int)TransCHKSUM.TYPE] != (int)TransType.ACK)
                {
                    return false;
                }
                seqNo = (byte)((buffer[(int)TransCHKSUM.SEQNO] + 1) % 2); //Increment SeqNo as ACK received with correct SeqNo
            }

            return true;
        }

        /// <summary>
        /// Sends the ack.
        /// </summary>
        /// <param name='ackType'>
        /// Ack type.
        /// </param>
        private void sendAck(bool ackType)
        {
            byte[] ackBuf = new byte[(int)TransSize.ACKSIZE];
            ackBuf[(int)TransCHKSUM.SEQNO] = (byte)
                (ackType ? (byte)buffer[(int)TransCHKSUM.SEQNO] : (byte)(buffer[(int)TransCHKSUM.SEQNO] + 1) % 2); //Send true or false
            ackBuf[(int)TransCHKSUM.TYPE] = (byte)(int)TransType.ACK;
            checksum.calcChecksum(ref ackBuf, (int)TransSize.ACKSIZE);
            
			if(++errorCount==3){
				ackBuf[1]++;
				Console.WriteLine("Noise ! byte 1 is spoiled in the third Ack transmission");

			}
            
            link.send(ackBuf, (int)TransSize.ACKSIZE);         
        }

        /// <summary>
        /// Send the specified buffer and size.
        /// </summary>
        /// <param name='buffer'>
        /// Buffer.
        /// </param>
        /// <param name='size'>
        /// Size.
        /// </param>
		public void send(byte[] buf, int size)
        {
            do
            {

                byte[] packet = new byte[size + (int)TransSize.ACKSIZE]; //Save space for CS, ACK and type
                Array.Copy(buf, 0, packet, 4, size);
                packet[(int)TransCHKSUM.SEQNO] = seqNo;
                packet[(int)TransCHKSUM.TYPE] = 0; //Data 
                checksum.calcChecksum(ref packet, packet.Length); //Calculate checksum


                //if (++errorCount == 3)
                //           {
                //               packet[1]++;
                //Console.WriteLine($"Noise ! byte 1 is spoiled every third transmission");
                //    //reset values
                //    errorCount = 0;
                //}

                //Send data until ack received and correct seq number
                link.send(packet, packet.Length);

            } while (!receiveAck());
            //updates old_seqNo
            old_seqNo = DEFAULT_SEQNO;

        }


		public int receive(ref byte[] buf)
        {
            buffer = new byte[buffer.Length];
            int receivedSize = link.receive(ref buffer);
            while (true)
            {
                if (!checksum.checkChecksum(buffer, receivedSize))
                {
                    sendAck(false);
                    receivedSize = link.receive(ref buffer);
                }
                else if (old_seqNo != buffer[(int)TransCHKSUM.SEQNO])
                {
                    sendAck(true);
                    old_seqNo = buffer[(int)TransCHKSUM.SEQNO];
                    break;
                }
                else
                {
                    sendAck(true);
                    receivedSize = link.receive(ref buffer);
                }
            }


            //sendAck(true);
            Array.Copy(buffer, 4, buf, 0, buf.Length);
            return receivedSize;
        }

        
    }
}
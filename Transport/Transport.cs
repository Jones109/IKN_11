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
            byte[] packet = new byte[size + (int)TransSize.ACKSIZE]; //Save space for CS, ACK and type
            Array.Copy(buf, 0, packet, 4, size);
            packet[(int)TransCHKSUM.SEQNO] = seqNo;
            packet[(int)TransCHKSUM.TYPE] = 0; //Data 
            checksum.calcChecksum(ref packet, packet.Length); //Calculate checksum

            //Send data until ack received and correct seq number
            link.send(packet, packet.Length);
            Console.WriteLine("Data Sendt !" + packet.Length);
            while (!receiveAck())
            {
                errorCount++;
                Console.WriteLine("Sequential errors: " + errorCount);
                if (errorCount > 5)
                {
                    Console.WriteLine("Too many errors, aborting....");
                    errorCount = 0;
                    return;
                }
                link.send(packet, packet.Length);
            }

            //reset values
            errorCount = 0;
        }

        /// <summary>
        /// Receive the specified buffer.
        /// </summary>
        /// <param name='buffer'>
        /// Buffer.
        /// </param>
        public int receive(ref byte[] buf)
        {
            bool result = receiveAck();

            if (result & dataReceived)
            {
                //Data received
                if (checksum.checkChecksum(buffer, recvSize)) //Validate data with checksum
                {
                    //Data correct
                    sendAck(true);
                    Array.Copy(buffer, 4, buf, 0, buf.Length);
                    return buffer.Length - (int)TransSize.ACKSIZE;
                }
                //Checksum not valid - data corrupted
                sendAck(false);
                return -1;
            }
            else if (result & !dataReceived)
            {
                //ACK received
                sendAck(false);
                return -1;
            }
            else
            {
                //Error
                sendAck(false);
                return -1;
            }

        }
    }
}
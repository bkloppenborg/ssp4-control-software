using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Data.OleDb;

namespace SSP4
{
    class cCommunication
    {
        // Datamembers:
        protected string    mstrCommPort;
        protected int       miBaudRate;
        protected Parity    mparParity;
        protected int       miDataBits;
        protected System.IO.Ports.StopBits mstpBits;
        protected SerialPort mrSerialPort;
        protected int       miCharInputBufferSize;
        protected Thread    mCommunicationThread;
        protected Thread    mReaderThread;
        protected cSSP4Base moSSP4;
        protected int       miTimeout;

        // The Queues
        protected cPriorityQueue mCommandQueue;
        protected cPriorityQueue mReplyQueue;

        // Delegates
        public delegate void SetCommandWindowDelegate(string strText);

        public cCommunication()
        {
            // Init the queues
            mCommandQueue = new cPriorityQueue();
            mReplyQueue = new cPriorityQueue();

            // TODO: Aquire these variables from the SSP4 class.

            // Init defaults:
            mstrCommPort = "";
            miBaudRate = 2400;
            miDataBits = 8;
            mparParity = System.IO.Ports.Parity.None;
            mstpBits = System.IO.Ports.StopBits.One;
            miCharInputBufferSize = 7;
            miTimeout = 1000;
        }

        // Get or Set the SpectraCyber to which this command object belongs
        public cSSP4Base SSP4
        {
            get
            {
                return moSSP4;
            }
            set
            {
                moSSP4 = value;
            }
        }

        // Get or Set the command queue
        public cPriorityQueue CommandQueue
        {
            get
            {
                return mCommandQueue;
            }
            set
            {
                mCommandQueue = value;
            }
        }

        // Get or Set the Reply Queue
        public cPriorityQueue ReplyQueue
        {
            get
            {
                return mReplyQueue;
            }
            set
            {
                mReplyQueue = value;
            }
        }

        // Get or set the buffer size.
        public int BufferSize
        {
            get
            {
                return miCharInputBufferSize;
            }
            set
            {
                miCharInputBufferSize = value;
            }
        }

        // Get or Set the Comm port
        public string CommPort
        {
            get
            {
                return mstrCommPort;
            }
            set
            {
                // Close the comm port, change the comm port, re-open the comm port, reset the unit.
                mstrCommPort = value;
            }
        }

        // Get or set the Baud Rate
        public int BaudRate
        {
            get
            {
                return miBaudRate;
            }
            set
            {
                miBaudRate = value;
            }
        }

        // Get or set the Databits
        public int DataBits
        {
            get
            {
                return miDataBits;
            }
            set
            {
                miDataBits = value;
            }
        }

        // Get or Set the Stop Bits
        public System.IO.Ports.StopBits StopBits
        {
            get
            {
                return mstpBits;
            }
            set
            {
                mstpBits = value;
            }
        }

        // Get or set the parity
        public System.IO.Ports.Parity Parity
        {
            get
            {
                return mparParity;
            }
            set
            {
                mparParity = value;
            }
        }

        public void Connect()
        {
            // Init the communication thread
            ThreadStart tdComm = new ThreadStart(CommunicationThread);
            mCommunicationThread = new Thread(tdComm);

            // Init the reader thread
            ThreadStart tdRead = new ThreadStart(ReaderThread);
            mReaderThread = new Thread(tdRead);

            // Reduce / Clear the queues
            mCommandQueue.Reduce();
            mReplyQueue.Clear();

            // Start the Threads
            mCommunicationThread.Start();
            mReaderThread.Start();
       }

        public void Reset()
        {
            // Clear the queues
            mCommandQueue.Clear();
            mReplyQueue.Clear();
        }

        public static bool CommandRepeats(cCommandItem oCommand)
        {
            if(oCommand.CommandType == eCommandType.Repeat)
                return true;

            return false;
        }

        // #### Thread functions #### //
        public void CommunicationThread()
        {
            string strCommand;

            // Init the serial port, set the timeout value, open the port.
            mrSerialPort = new System.IO.Ports.SerialPort(mstrCommPort, miBaudRate, mparParity, miDataBits, mstpBits);
            mrSerialPort.ReadTimeout = miTimeout;

            if (!(mrSerialPort.IsOpen))
            {
                try
                {
                    mrSerialPort.Open();
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message);
                }

                for (; ; )
                {
                    // Get the first unread item off of the queue (which blocks this thread if no item exists)
                    cCommandItem oCommandItem = mCommandQueue.GetFirstItem();

                    // TODO: Figure out how to repeat commands.
                    if (oCommandItem.CommandType == eCommandType.Repeat)
                        mCommandQueue.Add(oCommandItem.Duplicate());

                    strCommand = oCommandItem.Command;

                    // Break out of the loop if a eCommandType.Termination command is sent
                    if (oCommandItem.CommandType == eCommandType.Termination || !(mrSerialPort.IsOpen))
                    {
                        // Copy the command into the reply for this queue item.
                        oCommandItem.Reply = strCommand;
                        mReplyQueue.Add(oCommandItem);
                        break;
                    }
                    else if (oCommandItem.CommandType == eCommandType.ScanStop)
                    {
                        // The command to stop is also the reply, copy that over.
                        oCommandItem.Reply = strCommand;

                        // Essentially clear off the command queue by selecting only the repeating commands
                        List<cCommandItem> lstCommands = mCommandQueue.FindAll(CommandRepeats);
                        mCommandQueue.Clear();

                        for (int i = 0; i < lstCommands.Count; i++)
                            mCommandQueue.Add(lstCommands[i]);

                    }

                    if(!(oCommandItem.CommandType == eCommandType.ScanStart || oCommandItem.CommandType == eCommandType.ScanEnd)) // || oCommandItem.CommandType == eCommandType.ScanStop))
                    {
                        mrSerialPort.DiscardInBuffer();
                        mrSerialPort.WriteLine(strCommand + "\r\n");
                    }

                    // DEBUGGING: Set the command window text:
                    //moSSP4.SetCommandWindowText(strCommand);  // Output The Command sent to the SSP-4
                    //moSSP4.SetCommandWindowText("Waiting for " + oCommandItem.CommandWaitTime);   // Output The Waiting Time before data is read from the SSP-4

                    // Give the Device some time to process the command.
                    System.Threading.Thread.Sleep(oCommandItem.CommandWaitTime);

                    // Handle the replies:
                    if (oCommandItem.CommandType == eCommandType.DataDiscard)
                    {
                        mrSerialPort.DiscardInBuffer();
                    }
                    else if (oCommandItem.ExpectReply)
                    {
                        string strReply = "";
                        byte tmpByte;                                               // Used in Mono-compatable reading method
                        //char[] carrInBuffer = new char[miCharInputBufferSize];    // Used in Windows-only reading method

                        try
                        {
                            // Mono-compatable method for reading data from the serial port.
                            // Read from the serial port using bytes. 
                            tmpByte = (byte)mrSerialPort.ReadByte();
                            while (tmpByte != 0x0A) // Read until a newline character found.
                            {
                                strReply += ((char)tmpByte);
                                tmpByte = (byte)mrSerialPort.ReadByte();
                            }

                            // Windows-only method for reading data.
                            // Read characters from the input buffer
                            //mrSerialPort.Read(carrInBuffer, 0, oCommandItem.NumCharactersToRead);
                            //foreach (char cCharacter in carrInBuffer)
                            //    strReply += cCharacter;

                            // Clear the buffer:
                            mrSerialPort.DiscardInBuffer();

                        }
                        catch (TimeoutException e)  // Probably caused by the box being off or the wrong COMM port being selected
                        {
                            oCommandItem.CommandType = eCommandType.Error;
                            strReply = "COMM_TIMEOUT_ERROR";
                        }
                        catch (Exception e)
                        {
                            System.Windows.Forms.MessageBox.Show("Unknown Error");
                            strReply = "UNKNOWN_EXCEPTION";
                            oCommandItem.CommandType = eCommandType.Error;
                        }

                        // Set the reply
                        oCommandItem.Reply = strReply;
                    }
                    else // Remove the first character from the command string and use that as the reply.
                    {
                        oCommandItem.Reply = strCommand.Remove(0, 1);
                    }


                    // Now, move the command to the other queue
                    mReplyQueue.Add(oCommandItem);

                    // Finally, sleep until the next command should be processed
                    System.Threading.Thread.Sleep(oCommandItem.TimeToNextCommand);
                }
            }
            
            // The thread is done running. Close the connection to the serial port
            mrSerialPort.Close();
        }

        public void ReaderThread()
        {
            for (; ; )
            {
                // Get the first unread reply off of the queue, send that to the SpectraCyber object for processing
                cCommandItem oCommandItem = mReplyQueue.GetFirstItem();
                moSSP4.ProcessReply(oCommandItem.Reply, oCommandItem.CommandType);

                // The program is closing, gracefully stop this thread
                if (oCommandItem.CommandType == eCommandType.Termination)
                {
                    break;
                }
            }

            // The thread is done running, empty out the command queue
            mCommandQueue.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SSP4
{
    // Command Types
    enum eCommandType
    {
        ChangeSetting,
        DataRequest,
        DataDiscard,
        Error,
        GeneralCommunication,
        Reset,   
        Rescan,
        Repeat,             
        Startup,
        ScanStart, 
        ScanEnd,
        ScanStop,
        Termination  
    }

    class cCommandItem
    {
        private int miTimeToNextCommand = 0;    //
        protected string mstrCommand;
        protected string mstrReply;
        protected bool mbCommandRead;
        protected int miWaitTime = 100;          // Original Basic Program waited a minimum of 100 ms before trying to read a reply.
        protected bool mbReplySet;
        protected bool mbExpectReply;
        protected int miNumCharsToRead;
        protected eCommandType menCommandType;
        protected int miPriority;

        // Constructors:
        public cCommandItem()
        {
            mstrCommand = "";
            mstrReply = "";
            mbCommandRead = false;
            mbReplySet = false;
            miWaitTime = 0;
            mbExpectReply = false;
            miNumCharsToRead = 0;
            menCommandType = eCommandType.GeneralCommunication;
            miPriority = SetPriority();
        }

        public cCommandItem(string strCommand, int iWaitTime, bool bExpectReply, int iNumCharsToRead, eCommandType eCommandType)
        {
            mstrCommand = strCommand;
            mstrReply = "";
            mbCommandRead = false;
            mbReplySet = false;
            mbExpectReply = bExpectReply;

            if(iWaitTime > miWaitTime)
                miWaitTime = iWaitTime;

            if(iNumCharsToRead > 0)
                miNumCharsToRead = iNumCharsToRead;

            menCommandType = eCommandType;
            miPriority = SetPriority();
        }

        // Set the priority of this command
        protected int SetPriority()
        {
            switch (menCommandType)
            {
                // Repeat commands do not get deleted from the queue like one would expect
                case eCommandType.Repeat:
                    return 6;

                // Cover the most standard cases first:
                case eCommandType.DataDiscard:
                case eCommandType.DataRequest:
                case eCommandType.Error:
                case eCommandType.GeneralCommunication:
                case eCommandType.Rescan:
                case eCommandType.ScanEnd:
                    return 5;
                    
                case eCommandType.ScanStart:
                case eCommandType.ScanStop:
                    return 3;
                    
                case eCommandType.ChangeSetting:
                    return 2;
                    
                case eCommandType.Reset:
                    return 1;

                case eCommandType.Startup:
                case eCommandType.Termination:
                    return 0;
                    
                default:
                    return 5;
                    
            }
        }

        public cCommandItem Duplicate()
        {
            return new cCommandItem(mstrCommand, miWaitTime, mbExpectReply, miNumCharsToRead, menCommandType);
        }

        // Find out whether or not this command has been read
        public bool CommandRead
        {
            get
            {
                return mbCommandRead;
            }
        }

        // Find out whether or not the reply has been set on this command
        public bool ReplySet
        {
            get
            {
                return mbReplySet;
            }
            
        }

        // Get the amount of time to wait between when the command is sent and the command should be read.
        public int CommandWaitTime
        {
            get
            { 
                return miWaitTime;
            }
           
        }

        public int TimeToNextCommand
        {
            get
            {
                return miTimeToNextCommand;
            }
        }

        public bool ExpectReply
        {
            get
            {
                return mbExpectReply;
            }
        }

        // Get the number of characters to read from the serial port
        public int NumCharactersToRead
        {
            get
            {
                return miNumCharsToRead;
            }
            
        }

        // Get or Set the Command
        public string Command
        {
            get
            {
                mbCommandRead = true;
                return mstrCommand;
            }
            set
            {
                mstrCommand = value;
            }
        }

        // Get or Set the Reply
        public string Reply
        {
            get
            {
                return mstrReply;
            }
            set
            {
                mbReplySet = true;
                mstrReply = value;
            }
        }

        // Get the command type
        public eCommandType CommandType
        {
            get
            {
                return menCommandType;
            }
            set
            {
                menCommandType = value;
            }
        }

        // Get the command's priority
        public int Priority
        {
            get
            {
                return miPriority;
            }
        }

    }
}

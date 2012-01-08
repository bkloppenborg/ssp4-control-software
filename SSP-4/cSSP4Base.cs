using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SSP4
{
    enum ObjectRoles
    {
        Target,
        Calibrator,
        Dark,
        Sky
    }

    class cSSP4Base
    {
        // #### Datamembers #### //
        protected bool mbOnline;					// Whether or not the unit is online (able to send/receive commands, inited, etc)
        protected bool mbScanning;                 // Wheter or not the SpectraCyber is in the middle of a scan.
        protected string mstrSaveFile;

        // For Data output
        protected List<int> mDataList = new List<int>();
        System.IO.StreamWriter mSaveFile = null;

        // Gain
        protected int miGain;
        protected int[] miarrGainValues = new int[] {100, 10, 1 };  // The gain of the unit, either 1, 10, or 100 corresponding to 1, 2, or 3

        // Integration
        protected double mdIntegrationTime;
        protected double mdIntegrationTimeMin = 0.01;
        protected double mdIntegrationTimeMax = 65.53; 
        protected double mdIntegrationStepSize = 0.01;

        // Temperature
        protected int miTemperatureSetting;
        protected double mdTemperature;
        protected int miTemperatureMin = -40;
        protected int miTemperatureMax = 0;
        protected bool mbTemperatureRequestSent;


        // Datamembers for the Serial Port connection
        protected string    mstrCommPort;
        protected int       miBaudRate = 19200;
        protected int       miDataBits = 8;
        protected System.IO.Ports.Parity  mparParity = System.IO.Ports.Parity.None;
        protected System.IO.Ports.StopBits mstpBits = System.IO.Ports.StopBits.One;
        protected int miCharInputBufferSize = 7;        // Maximum Length of any reply from the SSP4

        // some comment about the stuff below here...
        protected cCommunication mcommCommunication;
        protected cPriorityQueue mCommandQueue;          // The command queue
        protected cPriorityQueue mReplyQueue;

        // A connection back to the form and delegates
        protected frmMain mpForm;
        public delegate void SetCommandWindowDelegate(string strText);
        public delegate void UpdateTemperatureDelegate(double dTemp);
        public delegate void UpdateSSP4Status();
        public delegate void IncrementProgressBarDelegate();
        public delegate void ResetProgressBarDelegate();

        protected string mstrTargetName;
        protected string mstrFilter;
        protected string mstrTelescopeName;
        protected string mstrObserverName;
        protected string mstrConditions;
        protected string mstrScanStart;
        protected ObjectRoles mObjectRole;


        public cSSP4Base()
        {
            // Setup the datamembers
            mbOnline = false;
            mbScanning = false;
            mstrSaveFile = "";
            mdIntegrationTime = mdIntegrationTimeMin;
            mdTemperature = (double) miTemperatureMax;
            miTemperatureSetting = -25;                  // Default temperature for the SSP-4 upon start is -25 C.
            miGain = miarrGainValues[2];                // By default the gain is at 1 (2nd item in array)  
            mdTemperature = 0.0;
            mstrCommPort = "";
            mbTemperatureRequestSent = false;
            mstrTargetName = "";
            mstrFilter = "";

            mstrTelescopeName = "";
            mstrObserverName = "";
            mstrConditions = "";
            mstrScanStart = "";
            mObjectRole = ObjectRoles.Calibrator;

            // Setup the communcation object.
            mcommCommunication = new cCommunication();
            mCommandQueue = mcommCommunication.CommandQueue;
            mReplyQueue = mcommCommunication.ReplyQueue;
        }


        /////////////////////////////////
        // Delegate Functions:
        /////////////////////////////////

        // Temporary functions:
        public void SetCommandWindowText(string strCommand)
        {
            if (!(mpForm.mbIsClosing))
            {
                SetCommandWindowDelegate pDelegate = new SetCommandWindowDelegate(mpForm.SetCommandWindowText);
                mpForm.Invoke(pDelegate, new object[] { strCommand });
            }
        }

        // Update the temperature on the main form:
        public void UpdateTemperature(double dTemp)
        {
            if (!(mpForm.mbIsClosing))
            {
                UpdateTemperatureDelegate pDelegate = new UpdateTemperatureDelegate(mpForm.UpdateTemperature);
                mpForm.Invoke(pDelegate, new object[] { dTemp });
            }
        }

        public void UpdateStatus()
        {
            if (!(mpForm.mbIsClosing))
            {
                UpdateSSP4Status pDelegate = new UpdateSSP4Status(mpForm.UpdateSSP4Status);
                mpForm.Invoke(pDelegate, new object[] { });
            }
        }

        public void IncrementProgressBar()
        {
            if (!(mpForm.mbIsClosing))
            {
                IncrementProgressBarDelegate pDelegate = new IncrementProgressBarDelegate(mpForm.IncrementProgressBar);
                mpForm.Invoke(pDelegate);
            }
        }

        public void ResetProgressBar()
        {
            if (!(mpForm.mbIsClosing))
            {
                ResetProgressBarDelegate pDelegate = new ResetProgressBarDelegate(mpForm.ResetProgressBar);
                mpForm.Invoke(pDelegate);
            }
        }

        public void WriteObservingInfo()
        {
            // Only attempt to write out if the save file is not null.
            if (mSaveFile != null)
            {
                // Now, write out the telescope, obsever and condition information, provided that they contain information.
                if (mstrTelescopeName.Length > 0)
                {
                    mSaveFile.WriteLine("StartTelescopeName, " + GetTimeString());
                    mSaveFile.WriteLine(mstrTelescopeName);
                    mSaveFile.WriteLine("EndTelescopeName, " + GetTimeString());
                }

                if (mstrObserverName.Length > 0)
                {
                    mSaveFile.WriteLine("StartObserverNames, " + GetTimeString());
                    mSaveFile.WriteLine(mstrObserverName);
                    mSaveFile.WriteLine("EndObserverNames, " + GetTimeString());
                }

                if (mstrConditions.Length > 0)
                {
                    mSaveFile.WriteLine("StartConditions, " + GetTimeString());
                    mSaveFile.WriteLine(mstrConditions);
                    mSaveFile.WriteLine("EndConditions, " + GetTimeString());
                }
            }
        }

        /////////////////////////////////
        // Functions:
        /////////////////////////////////

        public void Connect()
        {
            // Setup the port, database and then open the connections
            mcommCommunication.CommPort = mstrCommPort;
            mcommCommunication.BaudRate = miBaudRate;
            mcommCommunication.DataBits = miDataBits;
            mcommCommunication.Parity = System.IO.Ports.Parity.None;
            mcommCommunication.StopBits = mstpBits;
            mcommCommunication.BufferSize = miCharInputBufferSize;
            mcommCommunication.SSP4 = this;
            mcommCommunication.Connect();

            // Reset the unit
            Reset();
        }

        public void Disconnect()
        {
            // Close the connection to the unit and return control to the two-button interface:
            SendCommand("SEXIT0", 0, true, 3, eCommandType.GeneralCommunication);
        }

        public string GetTimeString()
        {
            return DateTime.Now.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss");
        }

        public void SetParentForm(frmMain pForm)
        {
            mpForm = pForm;
        }

        public virtual void Reset()
        {
            // Reset the communication threads:
            mcommCommunication.Reset();
            
            // If the unit is online, take it offline:
            if(mbOnline)
                SendCommand("SEXIT0", 0, true, 3, eCommandType.Reset);
            
            // Now, bring the unit back up, send it settings:
            SendCommand("SSTART", 0, true, 1, eCommandType.Reset);
        }


        public void Stop()
        {
            // Stop the scan.  This clears the queue so we have to restart the temperature polling.
            SendCommand("_STOP", 0, false, 0, eCommandType.ScanStop);
        }

        public void ProcessReply(string strReply, eCommandType eType)
        {
            // Handle special (non-supported command) cases first
            if (eType == eCommandType.ScanStart)
            {
                // Reset the Progress Bar, clear the data list.
                ResetProgressBar();
                mDataList.Clear();

                // Set the Role of this object according to mObjectRole
                string strRole = "Calibrator";

                if (mObjectRole == ObjectRoles.Target)
                    strRole = "Target";
                else if (mObjectRole == ObjectRoles.Dark)
                    strRole = "Dark";
                else if (mObjectRole == ObjectRoles.Sky)
                    strRole = "Sky";

                // Create the start-of-scan header.
                mstrScanStart = "StartScan, " + GetTimeString() + ", " + mstrTargetName + ", " +strRole + ", " + miGain + ", " + mdIntegrationTime.ToString("0.00") + ", " + mstrFilter;
            }
            else if (eType == eCommandType.ScanStop || eType == eCommandType.ScanEnd)
            {
                // Signal that the SSP-4 is done scanning.
                mbScanning = false;
                UpdateStatus();

                // Copy the data over to the output string.
                int iNumPoints = mDataList.Count;
                int iValue = 0;
                string strToWrite = iNumPoints.ToString();
                double dAverage = 0;
                double sumOfDerivation = 0;

                for (int i = 0; i < iNumPoints; i++)
                {
                    iValue = mDataList[i];
                    dAverage += iValue;
                    sumOfDerivation += (iValue * iValue);
                    strToWrite += ", " + iValue;
                }

                // Compute the average and standard deviation.
                dAverage /= iNumPoints;
                double sumOfDerivationAverage = sumOfDerivation / iNumPoints;
                double Stdev = Math.Sqrt(sumOfDerivationAverage - (dAverage * dAverage));

                // Now, write the data out to the file, including the scan header.
                mSaveFile.WriteLine(mstrScanStart);
                mSaveFile.WriteLine(strToWrite);
                mSaveFile.WriteLine("EndScan, " + GetTimeString());
                mSaveFile.Flush();

                // Reset the Progress Bar
                ResetProgressBar();

                // Output Some Statistics of the data.  Format the double to have two decimal points, automatically rounded.
                SetCommandWindowText("Object: " + mstrTargetName 
                    + "\t Gain: " + miGain 
                    + "\t Int: " + mdIntegrationTime.ToString("0.00") 
                    + "\t Filt: " + mstrFilter
                    + "\t NPoints: " + iNumPoints 
                    + "\t Average: " + dAverage.ToString("0.00") 
                    + "\t StDev: " + Stdev.ToString("0.00"));
            }
            else if (eType == eCommandType.Rescan)
            {
                //Scan();
            }
            else if (eType == eCommandType.Error)
            {
                // Clearly something is wrong, stop the threads and break communication with the box.
                mSaveFile.WriteLine("Error, " + GetTimeString() + ", " + strReply);
                SendCommand("_DISCONNECT", 0, false, 0, eCommandType.Termination);
                mbOnline = false;
                UpdateStatus();
                System.Windows.Forms.MessageBox.Show("An Error Has Occurred: " + strReply + "\n" + "Closing Connection.", "Connection Error", System.Windows.Forms.MessageBoxButtons.OK);
            }
            else if (eType == eCommandType.DataDiscard)
            {
                // TODO: Clear the buffer or something
            }
            else if (eType == eCommandType.Termination)
            {
                mSaveFile.WriteLine("SessionEnd");
                mSaveFile.Flush();
            }
            else
            {
                // TODO: Add Processing Capabilities:

                // Init vars
                char cFirstCharacter = strReply[0];

                // If we reach this point, we either have a data point, or a setting change.  

                // Ready for a long set of if-statements?  Go!
                if (cFirstCharacter == '!')         // The box is up and running
                {
                    SetTemperature(miTemperatureSetting);
                    SetIntegrationTime(mdIntegrationTime);
                    SetGain(miGain);

                    // Start polling for the temperature
                    StartTemperaturePolling();

                    mbOnline = true;
                    UpdateStatus();
                }
                else if (cFirstCharacter == 'G')    // Gain Setting Change Reply
                {
                    int iIndex = 0;

                    strReply = strReply.Remove(0, 4);

                    try
                    {
                        iIndex = Convert.ToInt32(strReply) - 1;
                    }
                    catch
                    {
                        // If some odd errors start showing up, do something with the error here.
                    }

                    miGain = miarrGainValues[iIndex];
                }
                else if (cFirstCharacter == 'I')    // Integration Setting Change Reply
                {
                    int iIntegrationTime = 0;

                    strReply = strReply.Remove(0, 1);

                    try
                    {
                        iIntegrationTime = Convert.ToInt32(strReply);
                    }
                    catch
                    {
                        // If some odd errors start showing up, do something with the error here.
                    }

                    mdIntegrationTime = iIntegrationTime * mdIntegrationStepSize;
                }
                else if (mbScanning && cFirstCharacter == 'C')    // Data Point, only valid if a scan is active.
                {
                    strReply = strReply.Remove(0, 2);
                    int iCounts = 0;
                    bool bInsertPoint = true;

                    try
                    {
                        iCounts = Convert.ToInt32(strReply);
                    }
                    catch
                    {
                        bInsertPoint = false;
                    }

                    // Append The Data.
                    if (bInsertPoint)
                    {
                        mDataList.Add(iCounts);
                        IncrementProgressBar();
                    }
                   
                }
                else if (cFirstCharacter == 'T')    // Temperature Setting Change Reply
                {
                    // TODO: Rewrite since documentation is incorrect.
                    strReply = strReply.Remove(0, 3);

                    try
                    {
                        miTemperatureSetting = -1 * Convert.ToInt32(strReply);
                    }
                    catch
                    {
                        // If some odd errors start showing up, do something with the error here.
                    }

                    mSaveFile.WriteLine("SetTemp, " + GetTimeString() + ", " + miTemperatureSetting.ToString("0.0"));
                    mSaveFile.Flush();
                    
                }
                else if (cFirstCharacter == 'F')    // Current System Temperature
                {
                    double dTemp = 0;
                    bool bUpdateTemperature = true;

                    strReply = strReply.Remove(0, 2);
                    try
                    {
                        dTemp = -1 * Convert.ToDouble(strReply);
                    }
                    catch
                    {
                        bUpdateTemperature = false;
                    }

                    // Update the GUI
                    if(bUpdateTemperature)
                        UpdateTemperature(dTemp);

                    // If the temperature has changed, write an entry to data file
                    if (bUpdateTemperature && (Math.Abs(dTemp - mdTemperature) > 0.3))
                    {
                        mdTemperature = dTemp;
                        mSaveFile.WriteLine("GotTemp, " + GetTimeString() + ", " + dTemp.ToString("0.0"));
                        mSaveFile.Flush();
                    }
                }
                else if (cFirstCharacter == 'E')    // Control of the box has been returned to the 2-button interface
                {
                    // Tell the threads to stop:
                    SendCommand("STOP", 0, false, 0, eCommandType.Termination);
                    mbOnline = false;
                    UpdateStatus();
                }
                else // The command is not implemented.  There is not a settings change associated with this item.
                {
                    // TODO: Re-enable this option
                    System.Windows.Forms.MessageBox.Show("The specified reply, " + strReply + " is not implemented for this unit.  Please verify the command structure.");
                }
            }
        }


        // Start a scan with the current settings
        public void Scan(int iNumberOfPoints)
        {
            mbScanning = true;
            // Note that we are starting a new data run, take the data points and note the end of the data run
            SendCommand("_SCAN_START", 0, false, 0, eCommandType.ScanStart);
            TakeData(iNumberOfPoints); 

            // Workaround to address a C=* being returned after data aquisition has ended.
            //SendCommand("SFTEMP", 1000, false, 0, eCommandType.DataDiscard);
            SendCommand("_SCAN_END", 0, false, 0, eCommandType.ScanEnd);
        }

        // TODO: Re-write this for either continuious or finite number of data points.
        private void TakeData(int iNumberOfPoints)
        {
            // Init datamembers:
            int iIntegration;

            // Determine the number of milliseconds we need to wait:
            iIntegration = Convert.ToInt32(mdIntegrationTime * 1000);

            // Now start taking data for the specified number of times.
            for (int i = 0; i < iNumberOfPoints; i++)
                SendCommand("SCOUNT", iIntegration + 100, true, 7, eCommandType.DataRequest);
        }

        // Send a command to the queue that starts temperature polling.
        protected void StartTemperaturePolling()
        {
            SendCommand("SFTEMP", 1000, true, 6, eCommandType.Repeat);
        }

        // Set the Integration Time
        private void SetIntegrationTime(double dTime)
        {
            // First we need to convert the double back into an integer
            int iTime = Convert.ToInt16(dTime / mdIntegrationStepSize);
            string strCommand = "SI" + PadString(iTime.ToString(), 4);
            SendCommand(strCommand);
        }

        // Set the name of the target object.
        public void SetTargetName(string TargetName)
        {
            mstrTargetName = TargetName;
        }

        // Set the Temperature
        private void SetTemperature(int iTemperature)
        {
            // Convert the temperature over to a positive integer.
            iTemperature = Math.Abs(iTemperature);
            string strCommand = "STEM" + PadString(iTemperature.ToString(), 2);
            SendCommand(strCommand);  
        }

        private void SetGain(int iGain)
        {
            // Convert the gain value over to values of 1, 2, or 3 for gain of 1, 10, or 100.
            iGain = Array.IndexOf(miarrGainValues, iGain) + 1; // Add one since arrays are zero indexed.
            SendCommand("SGAIN" + iGain, 0, false, 0);
        }

        public void SendCommand(string strCommand)
        {
            cCommandItem pCommand = new cCommandItem(strCommand, 0, false, 0, eCommandType.GeneralCommunication);
            mCommandQueue.Add(pCommand);
        }

        // Append strCommand to the queue with the default wait time as a eCommandType.Communication command
        public void SendCommand(string strCommand, int iTimeToWaitMS, bool bExpectReply, int iNumCharsToRead)
        {
            cCommandItem pCommand = new cCommandItem(strCommand, iTimeToWaitMS, bExpectReply, iNumCharsToRead, eCommandType.GeneralCommunication);
            mCommandQueue.Add(pCommand);
        }

        // Append strCommand to the queue, wait for iTimeToWait milliseconds (minimum 55 ms), and either expect or disregard a reply.
        public void SendCommand(string strCommand, int iTimeToWaitMS, bool bExpectReply, int iNumCharsToRead, eCommandType eCommType)
        {
            cCommandItem pCommand = new cCommandItem(strCommand, iTimeToWaitMS, bExpectReply, iNumCharsToRead, eCommType);
            mCommandQueue.Add(pCommand);
        }

        public void SendComment(string strComment)
        {
            if (mSaveFile != null)
            {
                mSaveFile.WriteLine("StartComment, " + GetTimeString());
                mSaveFile.WriteLine(strComment);
                mSaveFile.WriteLine("EndComment, " + GetTimeString());
            }

            SetCommandWindowText(strComment);
        }

        public void OpenFile(string FileName, bool bAppendToFile)
        {
            mstrSaveFile = FileName;

            if (mSaveFile != null)
            {
                mSaveFile.Flush();
                mSaveFile.Close();
            }

            mSaveFile = new System.IO.StreamWriter(mstrSaveFile, bAppendToFile);
            mSaveFile.WriteLine("SessionStart, " + GetTimeString());
        }

        public void CloseFile()
        {
            if (mSaveFile != null)
            {
                mSaveFile.Close();
                mstrSaveFile = "";

                mSaveFile = null;
            }
        }

        // Take an input string and pad the left side with zeros until it is iTotalLength long
        public string PadString(string strInput, int iTotalLength)
        {
            // Now, pad the string with zeros to make it the correct length.
            for (int i = strInput.Length; i < iTotalLength; i++)
            {
                strInput = "0" + strInput;
            }

            return strInput;
        }

        // #### Methods #### //
       
        // Set or Get the file name
        public string Savefile
        {
            get
            {
                return mstrSaveFile;
            }
        }

        // Set or get the current filter
        public string Filter
        {
            get
            {
                return mstrFilter;
            }
            set
            {
                mstrFilter = value;
            }
        }

        // Set or get the gain
        public int Gain
        {
            get
            {
                return miGain;
            }
            set
            {
                if (value != miGain)
                    SetGain(value);
            }
        }

        // Get the range of gain values
        public int[] GainValues
        {
            get
            {
                return miarrGainValues;
            }
        }

        // Set or Get the integration time
        public double IntegrationTime
        {
            get
            {
                return mdIntegrationTime;
            }
            set
            {
                if (mdIntegrationTimeMin <= value && mdIntegrationTimeMax >= value)
                {
                    if (mdIntegrationTime != value)
                        SetIntegrationTime(value);
                }
                else
                    System.Windows.Forms.MessageBox.Show("Integration Time is out of range.");
            }
        }

        // Set or get the temperature setting
        // We do not get the insturment temperature via this method, it is handled elsewhere.
        public int Temperature
        {
            get
            {
                return miTemperatureSetting;
            }
            set
            {
                if (miTemperatureMin <= value && miTemperatureMax >= value)
                {
                    if (miTemperatureSetting != value)
                    {
                        SetTemperature(value);
                        miTemperatureSetting = value;
                    }
                }
            }
           
        }

        // Get the maximum Temperature
        public double TemperatureMax
        {
            get
            {
                return (double) miTemperatureMax;
            }
        }

        // Get the minimum Temperature
        public double TemperatureMin
        {
            get
            {
                return (double)miTemperatureMin;
            }
        }

        public string TelescopeName
        {
            get
            {
                return mstrTelescopeName;
            }
            set
            {
                mstrTelescopeName = value;
            }
        }

        // Get the maximum Integration Time
        public double IntegrationTimeMax
        {
            get
            {
                return mdIntegrationTimeMax;
            }
        }

        // Get the minimum Temperature
        public double IntegrationTimeMin
        {
            get
            {
                return mdIntegrationTimeMin;
            }
        }

        // Get the Increment Size of the IntegrationTime
        public double IntegrationTimeStepSize
        {
            get
            {
                return mdIntegrationStepSize;
            }
        }

        // Get or Set the COMM port
        public string COMMPort
        {
            get
            {
                return mstrCommPort;
            }
            set
            {
                // TODO: Add error checking for valid COMM ports
                mstrCommPort = value;
            }
        }

        // Get or set the conditions
        public string Conditions
        {
            get
            {
                return mstrConditions;
            }
            set
            {
                mstrConditions = value;
            }
        }

        // Get or set the Observer(s) names
        public string ObserverName
        {
            get
            {
                return mstrObserverName;
            }
            set
            {
                    mstrObserverName = value;
            }
        }

        // Get the status of the unit
        public bool Online
        {
            get
            {
                return mbOnline;
            }
        }

        // A boolean to indicate whether or not a scan is in progress.
        public bool Scanning
        {
            get
            {
                return mbScanning;
            }
        }

        // A string to set/get the role of the current target object
        public ObjectRoles ObjectRole
        {
            get
            {
                return mObjectRole;
            }
            set
            {
                mObjectRole = value;
            }
        }
    }
}

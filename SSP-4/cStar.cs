using System;
using System.Collections.Generic;
using System.Text;

namespace SSP4
{
    class cStar
    {
        protected int miID;
        protected string mstrName;
        
        // TODO: Figure out if storing these as strings or decimal values is best.
        protected double mdRA;
        protected double mdDEC;

        public cStar()
        {
            miID = 0;
            mstrName = "";
            mdRA = 0;
            mdDEC = 0;
        }

        public cStar(int iStarID, string strName, double dRA, double dDEC)
        {
            miID = iStarID;
            mstrName = strName;
            mdRA = dRA;
            mdDEC = dDEC;
        }

        // ### Methods ###
        public int ID
        {
            get
            {
                return miID;
            }
        }

        public string Name
        {
            get
            {
                return mstrName;
            }
        }

        public double RA
        {
            get
            {
                return mdRA;
            }
            
        }

        public double DEC
        {
            get
            {
                return mdDEC;
            }
        }

        // Overrides
        public override string ToString()
        {
            return mstrName;
        }
    }


}

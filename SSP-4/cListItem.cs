using System;
using System.Collections.Generic;
using System.Text;

namespace SSP4
{
    class cListItem
    {
        object mpObject;

        public cListItem(object oObject)
        {
            mpObject = oObject;
        }

        public void Dispose()
        {
            System.GC.SuppressFinalize(this);
        }
    }
}

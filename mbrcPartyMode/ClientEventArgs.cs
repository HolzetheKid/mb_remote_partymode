using System;
using System.Linq;
using System.Collections.Generic;
using mbrcPartyMode.Helper;

namespace mbrcPartyMode
{

    public class ClientEventArgs : EventArgs
    {
        public ClientEventArgs(ConnectedClientAddress adr)
        {
            this.adr = adr;
        }

        public ConnectedClientAddress adr { get; set; }
    }

}
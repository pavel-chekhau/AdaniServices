﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CoreLib.Helpers {
   public interface ISender {
      void SendCommand(string command);
   }
}
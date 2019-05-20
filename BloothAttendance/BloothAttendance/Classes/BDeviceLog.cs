using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloothAttendance.Classes
{
    public class BDeviceLog : ICloneable
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public bool IsIn { get; set; }

        public object Clone()
        {
            return new BDeviceLog { Address = Address, Name = Name, Time = Time, IsIn = IsIn };
        }
    }
}

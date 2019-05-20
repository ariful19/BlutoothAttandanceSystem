using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloothAttendance.Classes
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Class { get; set; }
        public int Roll { get; set; }
        public string DeviceAddress { get; set; }
    }

    public   class TimeLog
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime Time { get; set; }
        public bool IsIn { get; set; }
    }
}

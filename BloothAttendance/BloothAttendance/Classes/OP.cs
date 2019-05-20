using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BloothAttendance.Classes
{
    public class OP
    {
        public static SQLiteConnection Conn { get { return new SQLiteConnection("Data Source=adb.db;Version=3;"); } }
    }
}

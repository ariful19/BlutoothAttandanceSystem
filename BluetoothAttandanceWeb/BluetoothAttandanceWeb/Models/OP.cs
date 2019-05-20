using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Web;

namespace BluetoothAttandanceWeb.Models
{
    public class OP
    {
        public static SQLiteConnection Conn { get { return new SQLiteConnection($"Data Source={AppDomain.CurrentDomain.BaseDirectory}App_Data\\adb.db;Version=3;"); } }

    }

    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Class { get; set; }
        public int Roll { get; set; }
        public string DeviceAddress { get; set; }
    }

    public class TimeLog
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime Time { get; set; }
        public bool IsIn { get; set; }
        public string DeviceAddress { get; set; }
    }
}
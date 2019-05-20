using BloothAttendance.Classes;
using Dapper;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BloothAttendance
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            tmr = new Timer
            {
                Interval = 1000 * 60
            };
            tmr.Tick += Tmr_Tick;
        }

        private async void Tmr_Tick(object sender, EventArgs e)
        {
            try
            {
                using (var conn = OP.Conn)
                {
                    var list = conn.Query("select timelog.*,student.DeviceAddress from timelog inner join student on student.id=timelog.studentId where time>date(@CurDate)", new { CurDate = DateTime.Now.ToString("yyyy-MM-dd") }).ToList();
                    if (list.Any())
                    {
                        var json = JsonConvert.SerializeObject(list);
                        var hc = new HttpClient();
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var res = await hc.PostAsync(url + "/api/SaveLog", content);

                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        private Timer tmr;
        private static List<BDeviceLog> list = new List<BDeviceLog>();
        private void E_OutOfRange(object sender, BluetoothWin32RadioOutOfRangeEventArgs e)
        {
            string address = e.Device.DeviceAddress.ToString();
            var dl = list.Find(o => o.Address == address);
            var copy = dl != null ? (BDeviceLog)dl.Clone() : null;
            if (dl != null)
            {
                list.Remove(dl);
                using (var conn = OP.Conn)
                {
                    var student = conn.QueryFirstOrDefault<Student>("select * from Student where DeviceAddress=@DeviceAddress", new { DeviceAddress = address });
                    if (student != null)
                    {
                        conn.Execute("INSERT INTO [TimeLog]		([StudentId]		,[Time]		,[IsIn])	VALUES	(@StudentId		,@Time		,@IsIn)", new { StudentId = student.Id, Time = DateTime.Now, IsIn = false });
                    }
                }
            }

            print();
            if (copy != null)
                dt.Rows.Add(copy.Address, copy.Name, false, copy.Time);


        }

        private void print()
        {
            dt.Rows.Clear();

            foreach (var item in list)
            {
                dt.Rows.Add(item.Address, item.Name, item.IsIn, item.Time);
            }
            FillAttandance();
        }

        private void E_InRange(object sender, BluetoothWin32RadioInRangeEventArgs e)
        {
            string address = e.Device.DeviceAddress.ToString();
            var dl = list.FirstOrDefault(o => o.Address == address);
            if (dl == null)
            {
                list.Add(new BDeviceLog
                {
                    Address = e.Device.DeviceAddress.ToString(),
                    IsIn = true,
                    Name = e.Device.DeviceName,
                    Time = DateTime.Now
                });
                using (var conn = OP.Conn)
                {
                    var student = conn.QueryFirstOrDefault<Student>("select * from Student where DeviceAddress=@DeviceAddress", new { DeviceAddress = address });
                    if (student != null)
                    {
                        conn.Execute("INSERT INTO [TimeLog]		([StudentId]		,[Time]		,[IsIn])	VALUES	(@StudentId		,@Time		,@IsIn)", new { StudentId = student.Id, Time = DateTime.Now, IsIn = true });
                    }
                }
            }

            print();


        }

        private DataTable dt;
        private DataTable dtAttendance;
        private bool search;
        private BluetoothClient bc;
        private void Form1_Load(object sender, EventArgs ev)
        {
            bc = new BluetoothClient();
            var r = BluetoothRadio.PrimaryRadio;
            var e = new BluetoothWin32Events(r);
            e.InRange += E_InRange;
            e.OutOfRange += E_OutOfRange;

            dt = new DataTable();
            dt.Columns.AddRange(new[] { new DataColumn("DeviceAddress"), new DataColumn("Name"), new DataColumn("IsIn"), new DataColumn("Time") });
            dgv.DataSource = dt;

            dtAttendance = new DataTable();
            dtAttendance.Columns.AddRange(new[] { new DataColumn("Name"), new DataColumn("In", typeof(DateTime)), new DataColumn("Out", typeof(DateTime)) });
            dgvAttendance.DataSource = dtAttendance;

            using (var conn = OP.Conn)
            {
                url = conn.QueryFirstOrDefault("select SettingValue from Settings where SettingKey='URL'").SettingValue;
                tbUrl.Text = url;
            }

            tmr.Start();
        }
        private async void FillAttandance()
        {
            using (var conn = OP.Conn)
            {
                var list = await conn.QueryAsync(@"select st.Name
	                                    ,min(tl.time) inTime
	                                    ,max(tl.time) outTime
                                    from student st
                                    INNER JOIN timeLog tl on st.id = tl.studentId
                                    where tl.time between date (@DateFrom)
		                                    and date (@DateTo) " +
                                            (cbClass.SelectedIndex > -1 ? " and st.class=" + cbClass.SelectedIndex : "") +
                                            @" group by st.id", new { DateFrom = DateTime.Now.ToString("yyyy-MM-dd"), DateTo = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") });
                dtAttendance.Rows.Clear();
                foreach (var item in list)
                {
                    dtAttendance.Rows.Add(item.Name, item.inTime, item.outTime);
                }
            }
        }
        private async void tglBtn_Click(object sender, EventArgs e)
        {
            search = !search;
            tglBtn.Text = search ? "Stop" : "Start";
            await Task.Run(() =>
            {
                while (search)
                {
                    bc.DiscoverDevices();
                }
            });
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (var conn = OP.Conn)
            {
                if (SelectedDevice == null)
                {
                    MessageBox.Show("Must Select the Device.");
                    return;
                }
                var first = conn.QueryFirstOrDefault("select * from Student where DeviceAddress=@DeviceAddress", new { DeviceAddress = SelectedDevice.Address });
                var student = new Student
                {
                    Class = cbClass.SelectedIndex,
                    DeviceAddress = SelectedDevice.Address,
                    Name = tbName.Text,
                    Roll = (int)tbRoll.Value
                };
                if (first != null)
                {
                    conn.Execute("update Student set Name=@Name,Roll=@Roll,Class=@Class where DeviceAddress=@DeviceAddress", student);
                }
                else
                {
                    conn.Execute("INSERT INTO [Student] ([Name] ,[Class] ,[Roll] ,[DeviceAddress]) VALUES (@Name ,@Class ,@Roll ,@DeviceAddress)", student);
                }
                MessageBox.Show("Saved Successfully!");
            }
        }

        private BDeviceLog SelectedDevice;
        private string url;

        private void dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count > 0)
            {
                var firstRow = dgv.SelectedRows[0];
                var dAddress = firstRow.Cells["DeviceAddress"].Value.ToString();
                SelectedDevice = list.Find(o => o.Address == dAddress);
            }
        }

        private void cbClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillAttandance();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            using (var conn = OP.Conn)
            {
                conn.Execute("update Settings set SettingValue=@SettingValue where SettingKey='URL'", new { SettingValue = url });
                var list = conn.Query<Student>("select * from student").ToList();
                var json = JsonConvert.SerializeObject(list);
                var hc = new HttpClient();
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var res = await hc.PostAsync(url + "/api/SaveStudents", content);
                if (res.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    MessageBox.Show("Sent Successfully!");
                }
            }
        }

        private void tbUrl_TextChanged(object sender, EventArgs e)
        {
            url = tbUrl.Text;
        }
    }


}

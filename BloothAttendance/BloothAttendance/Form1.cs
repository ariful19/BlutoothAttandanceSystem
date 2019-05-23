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
            catch (Exception)
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
                        conn.Execute("INSERT INTO [TimeLog] ([StudentId] ,[Time] ,[IsIn])	VALUES	(@StudentId ,@Time ,@IsIn)", new { StudentId = student.Id, Time = DateTime.Now, IsIn = false });
                    }
                }
            }

            print();
            //if (copy != null)
            //    dt.Rows.Add(copy.Address, copy.Name, false, copy.Time);


        }

        private void print()
        {
            //dt.Rows.Clear();

            //foreach (var item in list)
            //{
            //    dt.Rows.Add(item.Address, item.Name, item.IsIn, item.Time);
            //}
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
                        conn.Execute("INSERT INTO [TimeLog] ([StudentId] ,[Time] ,[IsIn])	VALUES	(@StudentId ,@Time ,@IsIn)", new { StudentId = student.Id, Time = DateTime.Now, IsIn = true });
                    }
                }
            }

            print();


        }

        //private DataTable dt;
        private DataTable dtAttendance;
        private DataTable dtAbsent;
        private bool search;
        private BluetoothClient bc;
        private void Form1_Load(object sender, EventArgs ev)
        {
            LoadForm();

        }

        private void LoadForm()
        {
            bc = new BluetoothClient();
            var r = BluetoothRadio.PrimaryRadio;
            var e = new BluetoothWin32Events(r);
            e.InRange += E_InRange;
            e.OutOfRange += E_OutOfRange;

            //dt = new DataTable();
            //dt.Columns.AddRange(new[] { new DataColumn("DeviceAddress"), new DataColumn("Name"), new DataColumn("IsIn"), new DataColumn("Time") });
            //dgv.DataSource = dt;

            dtAttendance = new DataTable();
            dtAttendance.Columns.AddRange(new[] { new DataColumn("ID"), new DataColumn("Roll"), new DataColumn("Name"), new DataColumn("In", typeof(DateTime)), new DataColumn("Out", typeof(DateTime)) });
            dgvAttendance.DataSource = dtAttendance;
            dgvAttendance.Columns["ID"].Visible = false;
            dgvAttendance.Columns["Roll"].Width = dgvAttendance.Columns["Roll"].GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true);

            dtAbsent = new DataTable();
            dtAbsent.Columns.AddRange(new[] { new DataColumn("ID"), new DataColumn("Roll"), new DataColumn("Name") });
            dgvAbsent.DataSource = dtAbsent;
            dgvAbsent.Columns[0].Visible = false;
            dgvAbsent.Columns["Roll"].Width = dgvAbsent.Columns["Roll"].GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true);

            using (var conn = OP.Conn)
            {
                url = conn.QueryFirstOrDefault("select SettingValue from Settings where SettingKey='URL'").SettingValue;
            }

            tmr = new Timer
            {
                Interval = 1000 * 60
            };
            tmr.Tick += Tmr_Tick;
            tmr.Start();

            //FillAttandance();
        }

        private async void FillAttandance()
        {
            using (var conn = OP.Conn)
            {
                var list = await conn.QueryAsync(@"select st.Id, st.Roll, st.Name
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
                    dtAttendance.Rows.Add(item.Id, item.Roll, item.Name, item.inTime, item.inTime == item.outTime ? null : item.outTime);
                }

                //fill absent

                var absentList = await conn.QueryAsync(@"SELECT st.Id, st.Roll, st.Name
                                        FROM Student st
                                        left outer JOIN timelog tl on st.id = tl.studentId
                                        where st.id not in (
                                         select studentId
                                         from timelog
                                         where time > date (@TheDate)
                                         ) " +
                                            (cbClass.SelectedIndex > -1 ? " and st.class=" + cbClass.SelectedIndex : "") +
                                            @"
                                            group by st.id", new { TheDate = DateTime.Now.ToString("yyyy-MM-dd") });
                dtAbsent.Rows.Clear();
                foreach (var item in absentList)
                {
                    dtAbsent.Rows.Add(item.Id, item.Roll, item.Name);
                }
            }
        }
        private async void tglBtn_Click(object sender, EventArgs e)
        {
            search = !search;
            if(search)FillAttandance();
            tglBtn.Text = search ? "Stop Taking Attendance" : "Start Taking Attendance";
            await Task.Run(() =>
            {
                while (search)
                {
                    bc.DiscoverDevices();
                }
            });
        }

      
        private string url;

        private void dgv_SelectionChanged(object sender, EventArgs e)
        {
            //if (dgv.SelectedRows.Count > 0)
            //{
            //    var firstRow = dgv.SelectedRows[0];
            //    var dAddress = firstRow.Cells["DeviceAddress"].Value.ToString();
            //    SelectedDevice = list.Find(o => o.Address == dAddress);
            //}
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


        private void dgvAbsent_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var dlg = new SetInOuttime() { StartPosition = FormStartPosition.CenterScreen };
            dlg.dtpOut.Enabled = false;
            var dlgRes = dlg.ShowDialog();
            if (dlgRes == DialogResult.OK)
            {
                if (dgvAbsent.SelectedRows.Count > 0)
                {
                    int studentId = int.Parse(dgvAbsent.SelectedRows[0].Cells["ID"].Value.ToString());
                    var time1 = dlg.dtpIn.Value;
                    var time2 = dlg.dtpOut.Value;
                    using (var conn = OP.Conn)
                    {
                        conn.Execute("INSERT INTO [TimeLog] ([StudentId] ,[Time] ,[IsIn])	VALUES	(@StudentId ,@Time ,@IsIn)", new { StudentId = studentId, Time = time1, IsIn = true });
                        conn.Execute("INSERT INTO [TimeLog] ([StudentId] ,[Time] ,[IsIn])	VALUES	(@StudentId ,@Time ,@IsIn)", new { StudentId = studentId, Time = time2, IsIn = false });
                    }
                    FillAttandance();
                }
            }
        }

        private void dgvAttendance_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var dlg = new SetInOuttime() { StartPosition = FormStartPosition.CenterScreen };
            var dlgRes = dlg.ShowDialog();
            if (dlgRes == DialogResult.OK)
            {
                if (dgvAttendance.SelectedRows.Count > 0)
                {
                    int studentId = int.Parse(dgvAttendance.SelectedRows[0].Cells["ID"].Value.ToString());
                    var time1 = dlg.dtpIn.Value;
                    var time2 = dlg.dtpOut.Value;
                    using (var conn = OP.Conn)
                    {
                        conn.Execute("delete from TimeLog where  studentId=@StudentId and Time>date(@Date)", new { StudentId = studentId, Date = DateTime.Now.ToString("yyyy-MM-dd") });
                        conn.Execute("INSERT INTO [TimeLog] ([StudentId] ,[Time] ,[IsIn])	VALUES	(@StudentId ,@Time ,@IsIn)", new { StudentId = studentId, Time = time1, IsIn = true });
                        conn.Execute("INSERT INTO [TimeLog] ([StudentId] ,[Time] ,[IsIn])	VALUES	(@StudentId ,@Time ,@IsIn)", new { StudentId = studentId, Time = time2, IsIn = false });
                    }
                    FillAttandance();
                }
            }
        }

        private void dgvAttendance_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (dgvAttendance.SelectedRows.Count > 0)
                {
                    int studentId = int.Parse(dgvAttendance.SelectedRows[0].Cells["ID"].Value.ToString());
                    using (var conn = OP.Conn)
                    {
                        conn.Execute("delete from TimeLog where  studentId=@StudentId and Time>date(@Date)", new { StudentId = studentId, Date = DateTime.Now.ToString("yyyy-MM-dd") });
                    }
                    FillAttandance();
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var running = search;
            if (search)
                tglBtn.PerformClick();
            new Register { }.Show();
            FillAttandance();
            if (!search && running)
                tglBtn.PerformClick();


        }
    }


}

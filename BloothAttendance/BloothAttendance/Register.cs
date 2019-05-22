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
    public partial class Register : Form
    {
        private DataTable dt;
        private BluetoothClient bc;
        private DataTable dtStudents;
        private string url;

        public Register()
        {
            InitializeComponent();
        }

        private void Register_Load(object sender, EventArgs ev)
        {
            bc = new BluetoothClient();
            var r = BluetoothRadio.PrimaryRadio;
            var e = new BluetoothWin32Events(r);
            e.InRange += E_InRange;
            e.OutOfRange += E_OutOfRange;
            StarBlutooth();

            dt = new DataTable();
            dt.Columns.AddRange(new[] { new DataColumn("DeviceAddress"), new DataColumn("Name"), new DataColumn("IsIn"), new DataColumn("Time") });
            dgv.DataSource = dt;

            dtStudents = new DataTable();
            dtStudents.Columns.AddRange(new[] { new DataColumn("ID"), new DataColumn("Roll"), new DataColumn("Name"), new DataColumn("DeviceAddress") });
            dgvStudents.DataSource = dtStudents;
            dgvStudents.Columns["ID"].Visible = false;
            dgvStudents.Columns["Roll"].Width = dgvStudents.Columns["Roll"].GetPreferredWidth(DataGridViewAutoSizeColumnMode.ColumnHeader, true);


            using (var conn = OP.Conn)
            {
                url = conn.QueryFirstOrDefault("select SettingValue from Settings where SettingKey='URL'").SettingValue;
                tbUrl.Text = url;
            }
            FillStudents();
        }

        private async void StarBlutooth()
        {
            await Task.Run(() =>
            {
                while (search)
                {
                    bc.DiscoverDevices();
                }
            });
        }

        private async void FillStudents()
        {
            using (var conn = OP.Conn)
            {
                var students = await conn.QueryAsync($"select [Id]	,[Name]	,[Class]	,[Roll]	,[DeviceAddress] from Student {(cbClass.SelectedIndex > -1 ? " where class=" + cbClass.SelectedIndex : "")}");
                dtStudents.Rows.Clear();
                foreach (var item in students)
                {
                    dtStudents.Rows.Add(item.Id, item.Roll, item.Name, item.DeviceAddress);
                }
            }
        }

        private void E_OutOfRange(object sender, BluetoothWin32RadioOutOfRangeEventArgs e)
        {
            string address = e.Device.DeviceAddress.ToString();
            var dl = list.Find(o => o.Address == address);
            var copy = dl != null ? (BDeviceLog)dl.Clone() : null;
            if (dl != null)
            {
                list.Remove(dl);
            }

            print();
            if (copy != null)
                dt.Rows.Add(copy.Address, copy.Name, false, copy.Time);
        }
        private static List<BDeviceLog> list = new List<BDeviceLog>();
        private bool search = true;
        private DataGridViewRow selectedRow = null;

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

            }

            print();
        }
        private void print()
        {
            dt.Rows.Clear();

            foreach (var item in list)
            {
                dt.Rows.Add(item.Address, item.Name, item.IsIn, item.Time);
            }
        }

        private void dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            if (dgv.SelectedRows.Count > 0)
            {
                var address = dgv.SelectedRows[0].Cells["DeviceAddress"].Value.ToString();
                tbDeviceAddress.Text = address;

                var row = dgvStudents.Rows.OfType<DataGridViewRow>().FirstOrDefault(o => o.Cells["DeviceAddress"].Value.ToString() == address);
                if (row != null)
                {
                    dgvStudents.ClearSelection();
                    row.Selected = true;
                }
            }
        }

        private void Register_FormClosing(object sender, FormClosingEventArgs e)
        {
            search = false;
        }

        private void dgvStudents_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvStudents.SelectedRows.Count > 0)
            {
                selectedRow = dgvStudents.SelectedRows[0];
                using (var conn = OP.Conn)
                {
                    var student = conn.QueryFirstOrDefault("select [Id]	,[Name]	,[Class]	,[Roll]	,[DeviceAddress] from Student where id=@Id", new { Id = selectedRow.Cells["ID"].Value });
                    cbClass.SelectedIndex = int.Parse(student.Class.ToString());
                    tbName.Text = student.Name;
                    tbDeviceAddress.Text = student.DeviceAddress;
                    tbRoll.Value = int.Parse(student.Roll.ToString());
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (var conn = OP.Conn)
            {
                if (selectedRow != null)
                {
                    var studentId = int.Parse(selectedRow.Cells["ID"].Value.ToString());
                    var student = new Student
                    {
                        Class = cbClass.SelectedIndex,
                        DeviceAddress = tbDeviceAddress.Text,
                        Name = tbName.Text,
                        Roll = (int)tbRoll.Value,
                        Id = studentId
                    };
                    conn.Execute("update Student set Name=@Name,Roll=@Roll,Class=@Class,DeviceAddress=@DeviceAddress where id=@Id", student);
                }
                else
                {
                    conn.Execute("INSERT INTO [Student] ([Name] ,[Class] ,[Roll] ,[DeviceAddress]) VALUES (@Name ,@Class ,@Roll ,@DeviceAddress)", new
                    {
                        Class = cbClass.SelectedIndex,
                        DeviceAddress = tbDeviceAddress.Text,
                        Name = tbName.Text,
                        Roll = (int)tbRoll.Value
                    });
                }
                MessageBox.Show("Saved Successfully!");
                FillStudents();
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            selectedRow = null;
            dgvStudents.ClearSelection();
            tbDeviceAddress.Text = tbName.Text = "";
            tbRoll.Value = 0;
            cbClass.SelectedItem = null;
        }

        private void cbClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillStudents();
        }

        private async void button1_ClickAsync(object sender, EventArgs e)
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

        private void Register_FormClosed(object sender, FormClosedEventArgs e)
        {
           // new Form1().Show();
        }
    }
}

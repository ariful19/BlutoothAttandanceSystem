using BluetoothAttandanceWeb.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace BluetoothAttandanceWeb.Controllers
{
    public class ValuesController : ApiController
    {
        [HttpGet]
        [Route("api/Salam")]
        public JsonResult<object> Salam()
        {
            using (var conn = OP.Conn)
            {
                var x = conn.QueryFirstOrDefault("select * from Student ");
                return Json(x);
            }
        }
        [HttpGet]
        [Route("api/GetAttendanceByClassId/{id}")]
        public async Task<JsonResult<object>> GetAttendanceByClassId(int id)
        {
            using (var conn = OP.Conn)
            {
                var list = await conn.QueryAsync(@"select st.roll, st.Name
	                                    ,min(tl.time) t1
	                                    ,max(tl.time) t2
                                    from student st
                                    INNER JOIN timeLog tl on st.id = tl.studentId
                                    where tl.time between date (@DateFrom)
		                                    and date (@DateTo) and st.class=" + id +
                                           @" group by st.id  order by st.roll ", new { DateFrom = DateTime.Now.ToString("yyyy-MM-dd"), DateTo = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") });
                return Json<object>(list.ToList());
            }
        }
        [HttpPost]
        [Route("api/SaveStudents")]
        public async Task<JsonResult<object>> SaveStudents(List<Student> students)
        {
            using (var conn = OP.Conn)
            {
                foreach (var item in students)
                {
                    var res = await conn.ExecuteAsync("update student set name=@Name,class=@Class,roll=@Roll where DeviceAddress=@DeviceAddress", new
                    {
                        item.Name,
                        item.Class,
                        item.Roll,
                        item.DeviceAddress
                    });
                    if (res == 0)
                    {
                        await conn.ExecuteAsync("INSERT INTO[Student]([Name],[Class],[Roll],[DeviceAddress]) VALUES (@Name, @Class, @Roll, @DeviceAddress)", new
                        {
                            item.Name,
                            item.Class,
                            item.Roll,
                            item.DeviceAddress
                        });
                    }
                }
                return Json<object>(new { ok = true });
            }
        }

        [HttpPost]
        [Route("api/SaveLog")]
        public async Task<JsonResult<object>> SaveLog(List<TimeLog> times)
        {
            using (var conn = OP.Conn)
            {
                foreach (var item in times)
                {
                    int studentId = (await conn.QueryFirstOrDefaultAsync<Student>(sql: "Select * from student where deviceaddress=@DeviceAddress", param: new { item.DeviceAddress })).Id;
                    var res = await conn.QueryAsync("select * from TimeLog  where Time=@Time and StudentId=@StudentId", new
                    {
                        StudentId = studentId,
                        item.Time
                    });
                    if (!res.Any())
                    {
                        await conn.ExecuteAsync("INSERT INTO[TimeLog](StudentId,Time,IsIn) VALUES (@StudentId,@Time,@IsIn)", new
                        {
                            StudentId = studentId,
                            item.Time,
                            item.IsIn
                        });
                    }
                }
                return Json<object>(new { ok = true });
            }
        }
    }
}

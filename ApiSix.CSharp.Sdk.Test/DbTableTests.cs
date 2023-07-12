using ApiSix.CSharp.model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ApiSix.CSharp.Sdk.Test
{
    public class DbTableTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void NomalTest()
        {
            var dt = new DbTable
            {
                Columns = new[] { "Id", "Name", "CreateTime" },
                Rows = new List<Object[]>
            {
                new Object[] { 123, "Stone", DateTime.Now },
                new Object[] { 456, "ApiSix", DateTime.Today }
            }
            };

            Assert.AreEqual(123, dt.Get<Int32>(0, "Id"));
            Assert.AreEqual(456, dt.Get<Int32>(1, "ID"));

            Assert.AreEqual("ApiSix", dt.Get<String>(1, "Name"));
            Assert.AreEqual(DateTime.Today, dt.Get<DateTime>(1, "CreateTime"));

            // 不存在的字段
            Assert.AreEqual(DateTime.MinValue, dt.Get<DateTime>(0, "Time"));

            Assert.False(dt.TryGet<DateTime>(1, "Time", out var time));

            var idx = dt.GetColumn("Name");
            Assert.AreEqual(1, idx);

            idx = dt.GetColumn("Time");
            Assert.AreEqual(-1, idx);

            // 迭代
            var i = 0;
            foreach (var row in dt)
            {
                if (i == 0)
                {
                    Assert.AreEqual(123, row["ID"]);
                    Assert.AreEqual("Stone", row["name"]);
                }
                else if (i == 1)
                {
                    Assert.AreEqual(456, row["ID"]);
                    Assert.AreEqual("ApiSix", row["name"]);
                    Assert.AreEqual(DateTime.Today, row["CreateTime"]);
                }
                i++;
            }
        }

        [Test]
        public void ToJson()
        {
            var db = new DbTable
            {
                Columns = new[] { "Id", "Name", "CreateTime" },
                Rows = new List<Object[]>
            {
                new Object[] { 123, "Stone", DateTime.Now },
                new Object[] { 456, "ApiSix", DateTime.Today }
            }
            };

            var json = db.ToJson();
            Assert.NotNull(json);
            Assert.True(json.Contains("\"Id\":123"));
            Assert.True(json.Contains("\"Name\":\"Stone\""));
            Assert.True(json.Contains("Id\":456"));
            Assert.True(json.Contains("\"Name\":\"ApiSix\""));
        }

        [Test]
        public void ToDictionary()
        {
            var db = new DbTable
            {
                Columns = new[] { "Id", "Name", "CreateTime" },
                Rows = new List<Object[]>
                {
                    new Object[] { 123, "Stone", DateTime.Now },
                    new Object[] { 456, "ApiSix", DateTime.Today }
                }
            };

            var list = db.ToDictionary();
            Assert.NotNull(list);
            Assert.AreEqual(2, list.Count);

            var dic = list[0];
            Assert.AreEqual(123, dic["Id"]);
            Assert.AreEqual("Stone", dic["Name"]);
        }

        [Test]
        public void BinaryTest()
        {
            var file = Path.GetTempFileName();

            var dt = new DbTable
            {
                Columns = new[] { "ID", "Name", "Time" },
                Types = new[] { typeof(Int32), typeof(String), typeof(DateTime) },
                Rows = new List<Object[]>
            {
                new Object[] { 11, "Stone", DateTime.Now.Trim() },
                new Object[] { 22, "石头", DateTime.Today },
                new Object[] { 33, "新科技", DateTime.UtcNow.Trim() }
            }
            };
            dt.SaveFile(file, true);

            Assert.True(File.Exists(file));

            var dt2 = new DbTable();
            dt2.LoadFile(file, true);

            Assert.AreEqual(3, dt2.Rows.Count);
            for (var i = 0; i < 3; i++)
            {
                var m = dt.Rows[i];
                var n = dt2.Rows[i];
                Assert.AreEqual(m[0], n[0]);
                Assert.AreEqual(m[1], n[1]);
                Assert.AreEqual(m[2], n[2]);
            }
        }

        [Test]
        public void BinaryVerTest()
        {
            var file = Path.GetTempFileName();

            var dt = new DbTable
            {
                Columns = new[] { "ID", "Name", "Time" },
                Types = new[] { typeof(Int32), typeof(String), typeof(DateTime) },
                Rows = new List<Object[]>
            {
                new Object[] { 11, "Stone", DateTime.Now.Trim() },
                new Object[] { 22, "石头", DateTime.Today },
                new Object[] { 33, "新科技", DateTime.UtcNow.Trim() }
            }
            };
            var pk = dt.ToPacket();

            // 修改版本
            pk[14]++;

            var ex = Assert.Throws<InvalidDataException>(() =>
            {
                var dt2 = new DbTable();
                dt2.Read(pk);
            });

            Assert.AreEqual("DbTable[ver=2]无法支持较新的版本[3]", ex.Message);
        }

        [Test]
        public void ModelsTest()
        {
            var list = new List<UserModel>
            {
                new UserModel { ID = 11, Name = "Stone", Time = DateTime.Now },
                new UserModel { ID = 22, Name = "石头", Time = DateTime.Today },
                new UserModel { ID = 33, Name = "新科技", Time = DateTime.UtcNow }
            };

            var dt = new DbTable();
            dt.WriteModels(list);

            Assert.NotNull(dt.Columns);
            Assert.AreEqual(3, dt.Columns.Length);
            Assert.AreEqual(nameof(UserModel.ID), dt.Columns[0]);
            Assert.AreEqual(nameof(UserModel.Name), dt.Columns[1]);
            Assert.AreEqual(nameof(UserModel.Time), dt.Columns[2]);

            Assert.NotNull(dt.Types);
            Assert.AreEqual(3, dt.Types.Length);
            Assert.AreEqual(typeof(Int32), dt.Types[0]);
            Assert.AreEqual(typeof(String), dt.Types[1]);
            Assert.AreEqual(typeof(DateTime), dt.Types[2]);

            Assert.NotNull(dt.Rows);
            Assert.AreEqual(3, dt.Rows.Count);
            Assert.AreEqual(11, dt.Rows[0][0]);
            Assert.AreEqual("石头", dt.Rows[1][1]);

            var list2 = dt.ReadModels<UserModel2>().ToList();
            Assert.NotNull(list2);
            Assert.AreEqual(3, list2.Count);
            for (var i = 0; i < list2.Count; i++)
            {
                var m = list[i];
                var n = list2[i];
                Assert.AreEqual(m.ID, n.ID);
                Assert.AreEqual(m.Name, n.Name);
                Assert.AreEqual(m.Time, n.Time);
            }
        }

        private class UserModel
        {
            public Int32 ID { get; set; }

            public String Name { get; set; }

            public DateTime Time { get; set; }
        }

        private class UserModel2
        {
            public Int32 ID { get; set; }

            public String Name { get; set; }

            public DateTime Time { get; set; }
        }
        DataTable GetTable()
        {
            var xml = @"<NewDataSet>
                  <Table>
                    <ID>1</ID>
                    <Name>管理员</Name>
                    <Enable>true</Enable>
                    <IsSystem>true</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>默认拥有全部最高权限，由系统工程师使用，安装配置整个系统</Remark>
                  </Table>
                  <Table>
                    <ID>2</ID>
                    <Name>高级用户</Name>
                    <Enable>true</Enable>
                    <IsSystem>false</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>业务管理人员，可以管理业务模块，可以分配授权用户等级</Remark>
                  </Table>
                  <Table>
                    <ID>3</ID>
                    <Name>普通用户</Name>
                    <Enable>true</Enable>
                    <IsSystem>false</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>普通业务人员，可以使用系统常规业务模块功能</Remark>
                  </Table>
                  <Table>
                    <ID>4</ID>
                    <Name>游客</Name>
                    <Enable>true</Enable>
                    <IsSystem>false</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>新注册默认属于游客</Remark>
                  </Table>
                </NewDataSet>
                ";

            var sch = @"<?xml version=""1.0"" encoding=""utf-16""?>
                <xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                  <xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true"">
                    <xs:complexType>
                      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
                        <xs:element name=""Table"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""ID"" type=""xs:long"" minOccurs=""0"" />
                              <xs:element name=""Name"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""Enable"" type=""xs:boolean"" minOccurs=""0"" />
                              <xs:element name=""IsSystem"" type=""xs:boolean"" minOccurs=""0"" />
                              <xs:element name=""Permission"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""Ex1"" type=""xs:int"" minOccurs=""0"" />
                              <xs:element name=""Ex2"" type=""xs:int"" minOccurs=""0"" />
                              <xs:element name=""Ex3"" type=""xs:double"" minOccurs=""0"" />
                              <xs:element name=""Ex4"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""Ex5"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""Ex6"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""CreateUser"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""CreateUserID"" type=""xs:int"" minOccurs=""0"" />
                              <xs:element name=""CreateIP"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""CreateTime"" type=""xs:dateTime"" minOccurs=""0"" />
                              <xs:element name=""UpdateUser"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""UpdateUserID"" type=""xs:int"" minOccurs=""0"" />
                              <xs:element name=""UpdateIP"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""UpdateTime"" type=""xs:dateTime"" minOccurs=""0"" />
                              <xs:element name=""Remark"" type=""xs:string"" minOccurs=""0"" />
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:choice>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                ";

            var ds = new DataSet();
            //ds.ReadXml(xml);
            ds.ReadXmlSchema(new StringReader(sch));

            using var reader = new StringReader(xml);
            ds.ReadXml(reader);

            return ds.Tables[0];
        }

        [Test]
        public void FromDataTable()
        {
            var table = GetTable();

            var dt = new DbTable();
            var rs = dt.Read(table);

            Assert.AreEqual(4, rs);
            Assert.AreEqual(4, dt.Rows.Count);
            Assert.AreEqual("ID,Name,Enable,IsSystem,Permission,Ex1,Ex2,Ex3,Ex4,Ex5,Ex6,CreateUser,CreateUserID,CreateIP,CreateTime,UpdateUser,UpdateUserID,UpdateIP,UpdateTime,Remark", dt.Columns.Join());
            Assert.AreEqual(typeof(Int64), dt.Types[0]);
            Assert.AreEqual(typeof(String), dt.Types[1]);
            Assert.AreEqual(typeof(Boolean), dt.Types[2]);
            Assert.AreEqual(typeof(DateTime), dt.Types[14]);

            var row = dt.GetRow(3);
            Assert.AreEqual(4L, dt.Rows[3][0]);
            Assert.AreEqual(4L, row["id"]);
            Assert.AreEqual("游客", row["name"]);
            Assert.AreEqual(false, row["IsSystem"]);
            Assert.AreEqual("2022-04-24T00:04:27+08:00".ToDateTime(), row["CreateTime"]);
        }

        [Test]
        public void ToDataTable()
        {
            var table = GetTable();
            var xml = table.DataSet.GetXml();
            var sch = table.DataSet.GetXmlSchema();

            var dt = new DbTable();
            var rs = dt.Read(table);

            var dt2 = dt.Write();//new DataTable("Table")
            var ds = new DataSet();
            ds.Tables.Add(dt2);
            var xml2 = ds.GetXml();
            var sch2 = ds.GetXmlSchema();

            Assert.AreEqual(xml, xml2);
            Assert.AreEqual(sch, sch2);
        }

        [Test]
        public void GetXml()
        {
            var table = GetTable();

            var dt = new DbTable();
            var rs = dt.Read(table);

            var xml = dt.GetXml();

            Assert.True(xml.Contains("<ID>1</ID>"));
            Assert.True(xml.Contains("<Name>管理员</Name>"));
            Assert.True(xml.Contains("<Remark>业务管理人员，可以管理业务模块，可以分配授权用户等级</Remark>"));
        }

        [Test]
        public void NextTest()
        {
            var buf = "Stone".GetBytes();

            var pk = new Packet(buf);
            pk.Append("NewLife".GetBytes());

            Assert.NotNull(pk.Next);
            Assert.AreEqual("StoneNewLife", pk.ToStr());

            var pk2 = pk.Slice(2, 6);
            Assert.AreEqual("oneNew", pk2.ToStr());

            var p = pk.IndexOf("eNe".GetBytes());
            Assert.AreEqual(4, p);

            Assert.AreEqual("StoneNewLife", pk.ToArray().ToStr());

            Assert.AreEqual("eNe", pk.ReadBytes(4, 3).ToStr());

            var arr = pk.ToSegment();
            Assert.AreEqual("StoneNewLife", arr.Array.ToStr());
            Assert.AreEqual(0, arr.Offset);
            Assert.AreEqual(5 + 7, arr.Count);

            var arrs = pk.ToSegments();
            Assert.AreEqual(2, arrs.Count);
            Assert.AreEqual("Stone", arrs[0].Array.ToStr());
            Assert.AreEqual("NewLife", arrs[1].Array.ToStr());

            var ms = pk.GetStream();
            Assert.AreEqual(0, ms.Position);
            Assert.AreEqual(5 + 7, ms.Length);
            Assert.AreEqual("StoneNewLife", ms.ToStr());

            ms = new MemoryStream();
            pk.CopyTo(ms);
            Assert.AreEqual(5 + 7, ms.Position);
            Assert.AreEqual(5 + 7, ms.Length);
            ms.Position = 0;
            Assert.AreEqual("StoneNewLife", ms.ToStr());

            ms = new MemoryStream();
            pk.CopyToAsync(ms).Wait();
            Assert.AreEqual(5 + 7, ms.Position);
            Assert.AreEqual(5 + 7, ms.Length);
            ms.Position = 0;
            Assert.AreEqual("StoneNewLife", ms.ToStr());

            var buf2 = new Byte[7];
            pk.WriteTo(buf2, 1, 5);
            Assert.AreEqual(0, buf2[0]);
            Assert.AreEqual(0, buf2[6]);
            Assert.AreEqual("Stone", buf2.ToStr(null, 1, 5));

            var pk3 = pk.Clone();
            Assert.AreNotEqual(pk.Data, pk3.Data);
            Assert.AreNotEqual(pk.Total, pk3.Total);
            Assert.AreNotEqual(pk.Count, pk3.Count);
            Assert.IsNull(pk3.Next);
        }

        [Test]
        public void LevelLogTests()
        {
            var p = "LevelLog\\";
            if (Directory.Exists(p.GetFullPath())) Directory.Delete(p.GetFullPath(), true);

            var log = new LevelLog(p, "{1}\\{0:yyyy_MM_dd}.log");
            log.Level = LogLevel.All;

            var logs = log.GetValue("_logs") as IDictionary<LogLevel, ILog>;
            Assert.NotNull(logs);
            Assert.AreEqual(5, logs.Count);

            log.Debug("debug");
            log.Info("info");
            log.Warn("warn");
            log.Error("error");
            log.Fatal("fatal");

            // 等待日志落盘
            Thread.Sleep(2000);

            var f = p + $"debug\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"info\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"warn\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"error\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"fatal\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));
        }
        [Test]
        public void TracerTests()
        {
            var counter = new PerfCounter();

            var p = "LevelLog\\";
            var log = new LevelLog(p, "{1}\\{0:yyyy_MM_dd}.log");
            log.Level = LogLevel.All;
            XTrace.Log = log;

            XTrace.WriteLine("IsHighResolution={0}", Stopwatch.IsHighResolution);
            XTrace.WriteLine("Frequency={0:n0}", Stopwatch.Frequency);
            var tickFrequency = typeof(CounterHelper).GetValue("tickFrequency");
            XTrace.WriteLine("tickFrequency={0:n0}", tickFrequency);

            var ts = counter.StartCount();
            var count = 10000;
            Thread.SpinWait(count);

            var usCost = counter.StopCount(ts);
            XTrace.WriteLine("Thread.SpinWait({0}) = {1}us", count, usCost);

            //Assert.True(usCost >= 1000);
        }
        [Test]
        public void SplitAsDictionaryTests()
        {
            var str = "IP=172.17.0.6,172.17.0.7,172.17.16.7;Port=3774";
            var dic = str.SplitAsDictionary("=", ";");

            Assert.AreEqual(2, dic.Count);
            //foreach (var item in dic)
            //{
            //    Assert.AreEqual("IP", item.Key);
            //}

            Assert.True(dic.ContainsKey("IP"));
            Assert.True(dic.ContainsKey("Ip"));
            Assert.True(dic.ContainsKey("ip"));
            Assert.True(dic.ContainsKey("iP"));

            var rules = dic.ToDictionary(e => e.Key, e => e.Value.Split(","));

            Assert.True(rules.ContainsKey("IP"));
            Assert.False(rules.ContainsKey("Ip"));
            Assert.False(rules.ContainsKey("ip"));
            Assert.False(rules.ContainsKey("iP"));
        }

    }
}
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ApiSix.CSharp.Sdk.Test
{
    public class CsvDbTests
    {
        private CsvDb<GeoArea> GetDb(String name)
        {
            var file = $"data/{name}.csv".GetFullPath();
            if (File.Exists(file)) File.Delete(file);

            var db = new CsvDb<GeoArea>((x, y) => x.Code == y.Code)
            {
                FileName = file
            };
            return db;
        }

        private GeoArea GetModel()
        {
            var model = new GeoArea
            {
                Code = Rand.Next(),
                Name = Rand.NextString(14),
            };

            return model;
        }

        private String[] GetHeaders()
        {
            var pis = typeof(GeoArea).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return pis.Select(e => e.Name).ToArray();
        }

        private Object[] GetValue(GeoArea model)
        {
            var pis = typeof(GeoArea).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            //return pis.Select(e => e.GetValue(model, null)).ToArray();
            var arr = new Object[pis.Length];
            for (var i = 0; i < pis.Length; i++)
            {
                arr[i] = pis[i].GetValue(model, null);
                if (pis[i].PropertyType == typeof(Boolean))
                    arr[i] = (Boolean)arr[i] ? "1" : "0";
                else if (pis[i].Name == "Code" && arr[i].ToString().Length > 9)
                    arr[i] = "\t" + arr[i];
            }
            return arr;
        }

        [Test]
        public void InsertTest()
        {
            var db = GetDb("Insert");

            var model = GetModel();
            db.Add(model);

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.AreEqual(2, lines.Length);

            Assert.AreEqual(GetHeaders().Join(","), lines[0]);
            Assert.AreEqual(GetValue(model).Join(","), lines[1]);
        }

        [Test]
        public void InsertsTest()
        {
            var db = GetDb("Inserts");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.AreEqual(list.Count + 1, lines.Length);

            Assert.AreEqual(GetHeaders().Join(","), lines[0]);
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(GetValue(list[i]).Join(","), lines[i + 1]);
            }
        }

        [Test]
        public void GetAllTest()
        {
            var db = GetDb("GetAll");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 把文件读出来
            var list2 = db.FindAll();
            Assert.AreEqual(list.Count, list2.Count);

            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(GetValue(list[i]).Join(","), GetValue(list2[i]).Join(","));
            }

            // 高级查找
            var list3 = db.FindAll(e => e.Code  >= 100 && e.Code < 1000);
            var list4 = list.Where(e => e.Code  >= 100 && e.Code < 1000).ToList();
            Assert.AreEqual(list4.Select(e => e.Code), list3.Select(e => e.Code));
        }

        [Test]
        public void GetCountTest()
        {
            var db = GetDb("GetCount");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.AreEqual(list.Count + 1, lines.Length);
            Assert.AreEqual(list.Count, db.FindCount());
        }

        [Test]
        public void LargeInsertsTest()
        {
            var db = GetDb("LargeInserts");

            var list = new List<GeoArea>();
            var count = 100_000;
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.AreEqual(list.Count + 1, lines.Length);

            Assert.AreEqual(GetHeaders().Join(","), lines[0]);
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(GetValue(list[i]).Join(","), lines[i + 1]);
            }
        }

        [Test]
        public void InsertTwoTimesTest()
        {
            var db = GetDb("InsertTwoTimes");

            // 第一次插入
            var list = new List<GeoArea>();
            {
                var count = Rand.Next(2, 100);
                for (var i = 0; i < count; i++)
                {
                    list.Add(GetModel());
                }

                db.Add(list);
            }

            // 第二次插入
            {
                var list2 = new List<GeoArea>();
                var count = Rand.Next(2, 100);
                for (var i = 0; i < count; i++)
                {
                    list2.Add(GetModel());
                }

                db.Add(list2);

                list.AddRange(list2);
            }

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.AreEqual(list.Count + 1, lines.Length);

            Assert.AreEqual(GetHeaders().Join(","), lines[0]);
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(GetValue(list[i]).Join(","), lines[i + 1]);
            }
        }

        [Test]
        public void DeletesTest()
        {
            var db = GetDb("Deletes");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 随机删除一个
            var idx = Rand.Next(list.Count);
            var rs = db.Remove(list[idx]);
            Assert.AreEqual(1, rs);

            list.RemoveAt(idx);
            Assert.AreEqual(list.Count, db.FindCount());

            // 随机抽几个，删除
            var list2 = new List<GeoArea>();
            for (var i = 0; i < list.Count; i++)
            {
                if (Rand.Next(2) == 1) list2.Add(list[i]);
            }

            var rs2 = db.Remove(list2);
            Assert.AreEqual(list2.Count, rs2);
            Assert.AreEqual(list.Count - list2.Count, db.FindCount());
        }

        [Test]
        public void UpdateTest()
        {
            var db = GetDb("Update");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 随机改一个
            var idx = Rand.Next(list.Count);
            var model = db.Find(list[idx]);
            Assert.NotNull(model);

            model.ParentCode = Rand.Next();
            var rs = db.Update(model);
            Assert.True(rs);

            var model2 = db.Find(list[idx]);
            Assert.NotNull(model2);
            Assert.AreEqual(model.ParentCode, model2.ParentCode);
        }

        [Test]
        public void WriteTest()
        {
            var db = GetDb("Write");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 再次覆盖写入
            list.Clear();
            for (var i = 0; i < 10; i++)
            {
                list.Add(GetModel());
            }
            db.Write(list, false);

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.AreEqual(list.Count + 1, lines.Length);
        }

        [Test]
        public void ClearTest()
        {
            var db = GetDb("Clear");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 清空
            db.Clear();

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.True(lines.Length == 1);
        }

        [Test]
        public void SetTest()
        {
            var db = GetDb("Set");

            var list = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);

            // 设置新的
            var model = GetModel();
            db.Set(model);

            // 把文件读出来
            var lines = File.ReadAllLines(db.FileName.GetFullPath());
            Assert.AreEqual(list.Count + 1 + 1, lines.Length);
        }





        [Test]
        public void CsvFileTestMemory()
        {
            var ms = new MemoryStream();

            var list = new List<Object[]>
            {
                new Object[] { 1234, "Stone", true, DateTime.Now },
                new Object[] { 5678, "NewLife", false, DateTime.Today }
            };

            {
                using var csv = new CsvFile(ms, true);
                csv.Separator = ',';
                csv.Encoding = Encoding.UTF8;

                csv.WriteLine(new[] { "Code", "Name", "Enable", "CreateTime" });
                csv.WriteAll(list);
            }

            var txt = ms.ToArray().ToStr();
            var lines = txt.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(3, lines.Length);
            Assert.AreEqual("Code,Name,Enable,CreateTime", lines[0]);
            Assert.AreEqual($"1234,Stone,1,{((DateTime)list[0][3]).ToFullString()}", lines[1]);
            Assert.AreEqual($"5678,NewLife,0,{((DateTime)list[1][3]).ToFullString()}", lines[2]);

            {
                ms.Position = 0;
                using var csv = new CsvFile(ms);
                var headers = csv.ReadLine();
                var all = csv.ReadAll();

                Assert.AreEqual(4, headers.Length);
                Assert.AreEqual("Code", headers[0]);
                Assert.AreEqual("Name", headers[1]);

                Assert.AreEqual(2, all.Length);
            }
        }
        [Test]
        public void CsvFileTestFile()
        {
            var file = "data/test.csv";

            var list = new List<Object[]>
            {
                new Object[] { 1234, "Stone", true, DateTime.Now },
                new Object[] { 5678, "NewLife", false, DateTime.Today }
            };

            {
                using var csv = new CsvFile(file, true);
                csv.Separator = ',';
                csv.Encoding = Encoding.UTF8;

                csv.WriteLine(new[] { "Code", "Name", "Enable", "CreateTime" });
                csv.WriteAll(list);
            }

            var lines = File.ReadAllLines(file.GetFullPath());
            Assert.AreEqual(3, lines.Length);
            Assert.AreEqual("Code,Name,Enable,CreateTime", lines[0]);
            Assert.AreEqual($"1234,Stone,1,{((DateTime)list[0][3]).ToFullString()}", lines[1]);
            Assert.AreEqual($"5678,NewLife,0,{((DateTime)list[1][3]).ToFullString()}", lines[2]);

            {
                using var csv = new CsvFile(file);
                var headers = csv.ReadLine();
                var all = csv.ReadAll();

                Assert.AreEqual(4, headers.Length);
                Assert.AreEqual("Code", headers[0]);
                Assert.AreEqual("Name", headers[1]);

                Assert.AreEqual(2, all.Length);
            }
        }


        [Test]
        public void MemoryQueueTest()
        {
            XTrace.WriteLine("MemoryQueueTests.Test1");

            var q = new MemoryQueue<String>();

            Assert.True(q.IsEmpty);
            Assert.AreEqual(0, q.Count);

            q.Add("test");
            q.Add("newlife", "stone");

            Assert.False(q.IsEmpty);
            Assert.AreEqual(3, q.Count);

            var s1 = q.TakeOne();
            Assert.AreEqual("test", s1);

            var ss = q.Take(3).ToArray();
            Assert.AreEqual(2, ss.Length);

            XTrace.WriteLine("begin TokeOneAsync");
            ThreadPool.QueueUserWorkItem(s =>
            {
                Thread.Sleep(1100);
                XTrace.WriteLine("add message");
                q.Add("delay");
            });

            var s2 = q.TakeOneAsync(15, default).Result;
            XTrace.WriteLine("end TokeOneAsync");
            Assert.AreEqual("delay", s2);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void MemoryCacheTestBigSave(Boolean compressed)
        {
            var mc = new MemoryCache();
            var mcl = new MemoryCache();

            for (var i = 0; i < 500_000; i++)
            {
                var ga = new GeoArea { Code = Rand.Next(100000, 999999), Name = Rand.NextString(8) };
                mc.Set(ga.Name, ga);
            }

            if (compressed)
            {
                mc.Save("data/bigsave.gz", true);
                mcl.Load("data/bigsave.gz", true);
            }
            else
            {
                mc.Save("data/bigsave.cache", false);
                mcl.Load("data/bigsave.cache", false);
            }
        }
    }
    /// <summary>地理区域</summary>
    public class GeoArea
    {
        #region 属性
        /// <summary>编码</summary>
        public Int32 Code { get; set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>父级</summary>
        public Int32 ParentCode { get; set; }

        /// <summary>中心</summary>
        public String Center { get; set; }

        /// <summary>边界</summary>
        public String Polyline { get; set; }

        /// <summary>级别</summary>
        public String Level { get; set; }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"{Code} {Name}";
    }
}
using NUnit.Framework;
using System;
using System.IO;

namespace ApiSix.CSharp.Sdk.Test
{
    public class BinaryTests
    {
        [Test]
        public void Normal()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary();
            Assert.AreEqual(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.AreEqual(10, pk.Total);
            Assert.AreEqual("000004D20553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream() };
            var model2 = bn2.Read<MyModel>();
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        [Test]
        public void EncodeInt()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { EncodeInt = true, };
            Assert.AreEqual(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.AreEqual(8, pk.Total);
            Assert.AreEqual("D2090553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true };
            var model2 = bn2.Read<MyModel>();
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        [Test]
        public void IsLittleEndian()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { IsLittleEndian = true, };
            Assert.AreEqual(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.AreEqual(10, pk.Total);
            Assert.AreEqual("D20400000553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), IsLittleEndian = true };
            var model2 = bn2.Read<MyModel>();
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        [Test]
        public void UseFieldSize()
        {
            var model = new MyModelWithFieldSize { Name = "Stone" };
            var bn = new Binary { UseFieldSize = true, };
            Assert.AreEqual(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.AreEqual(6, pk.Total);
            Assert.AreEqual("0553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), UseFieldSize = true };
            var model2 = bn2.Read<MyModelWithFieldSize>();
            Assert.AreEqual(model.Length, model2.Length);
            Assert.AreEqual(model.Name, model2.Name);
        }

        [Test]
        public void SizeWidth()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { SizeWidth = 2, };
            Assert.AreEqual(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.AreEqual(11, pk.Total);
            Assert.AreEqual("000004D2000553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), SizeWidth = 2 };
            var model2 = bn2.Read<MyModel>();
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        [Test]
        public void UseProperty()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { UseProperty = false, };
            Assert.AreEqual(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.AreEqual(10, pk.Total);
            Assert.AreEqual("000004D20553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), UseProperty = false };
            var model2 = bn2.Read<MyModel>();
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        [Test]
        public void IgnoreMembers()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary();
            bn.IgnoreMembers.Add("Code");
            Assert.AreEqual(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.AreEqual(6, pk.Total);
            Assert.AreEqual("0553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream() };
            bn2.IgnoreMembers.Add("Code");
            var model2 = bn2.Read<MyModel>();
            Assert.AreEqual(0, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        [Test]
        public void Fast()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.AreEqual(8, pk.Total);
            Assert.AreEqual("D2090553746F6E65", pk.ToHex());
            Assert.AreEqual("0gkFU3RvbmU=", pk.ToArray().ToBase64());

            var model2 = Binary.FastRead<MyModel>(pk.GetStream());
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);

            var ms = new MemoryStream();
            Binary.FastWrite(model, ms);
            Assert.AreEqual("D2090553746F6E65", ms.ToArray().ToHex());
        }

        private class MyModel
        {
            public Int32 Code { get; set; }

            public String Name { get; set; }
        }

        [Test]
        public void Accessor()
        {
            var model = new MyModelWithAccessor { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.AreEqual(10, pk.Total);
            Assert.AreEqual("D20400000553746F6E65", pk.ToHex());
            Assert.AreEqual("0gQAAAVTdG9uZQ==", pk.ToArray().ToBase64());

            var model2 = Binary.FastRead<MyModelWithAccessor>(pk.GetStream());
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);

            var ms = new MemoryStream();
            Binary.FastWrite(model, ms);
            Assert.AreEqual("D20400000553746F6E65", ms.ToArray().ToHex());
        }

        private class MyModelWithAccessor : IAccessor
        {
            public Int32 Code { get; set; }

            public String Name { get; set; }

            public Boolean Read(Stream stream, Object context)
            {
                var reader = new BinaryReader(stream);
                Code = reader.ReadInt32();
                Name = reader.ReadString();

                return true;
            }

            public Boolean Write(Stream stream, Object context)
            {
                var writer = new BinaryWriter(stream);
                writer.Write(Code);
                writer.Write(Name);

                return true;
            }
        }

        [Test]
        public void MemberAccessor()
        {
            var model = new MyModelWithMemberAccessor { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.AreEqual(8, pk.Total);
            Assert.AreEqual("D2090553746F6E65", pk.ToHex());
            Assert.AreEqual("0gkFU3RvbmU=", pk.ToArray().ToBase64());

            var model2 = Binary.FastRead<MyModelWithMemberAccessor>(pk.GetStream());
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);

            var ms = new MemoryStream();
            Binary.FastWrite(model, ms);
            Assert.AreEqual("D2090553746F6E65", ms.ToArray().ToHex());
        }

        private class MyModelWithMemberAccessor : IMemberAccessor
        {
            public Int32 Code { get; set; }

            public String Name { get; set; }

            public Boolean Read(IFormatterX formatter, AccessorContext context)
            {
                var bn = formatter as Binary;

                switch (context.Member.Name)
                {
                    case "Code": Code = bn.Read<Int32>(); break;
                    case "Name": Name = bn.Read<String>(); break;
                }
                //Code = bn.Read<Int32>();
                //Name = bn.Read<String>();

                return true;
            }
            public Boolean Write(IFormatterX formatter, AccessorContext context)
            {
                var bn = formatter as Binary;

                switch (context.Member.Name)
                {
                    case "Code": bn.Write(Code); break;
                    case "Name": bn.Write(Name); break;
                }

                return true;
            }
        }

        [Test]
        public void FieldSize()
        {
            var model = new MyModelWithFieldSize { Name = "Stone" };
            Assert.AreEqual(0, model.Length);

            var bn = new Binary { EncodeInt = true, UseFieldSize = true };
            bn.Write(model);
            var pk = new Packet(bn.GetBytes());
            Assert.AreEqual(5, model.Length);
            Assert.AreEqual(6, pk.Total);
            Assert.AreEqual("0553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true, UseFieldSize = true };
            var model2 = bn2.Read<MyModelWithFieldSize>();
            Assert.AreEqual(model.Length, model2.Length);
            Assert.AreEqual(model.Name, model2.Name);
        }

        private class MyModelWithFieldSize
        {
            public Byte Length { get; set; }

            [FieldSize(nameof(Length))]
            public String Name { get; set; }
        }

        [Test]
        public void MemberAccessorAtt()
        {
            var model = new MyModelWithMemberAccessorAtt { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.AreEqual(8, pk.Total);
            Assert.AreEqual("D2090553746F6E65", pk.ToHex());

            var model2 = Binary.FastRead<MyModelWithMemberAccessorAtt>(pk.GetStream());
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        private class MyModelWithMemberAccessorAtt
        {
            public Int32 Code { get; set; }

            [MyAccessor]
            public String Name { get; set; }
        }

        private class MyAccessorAttribute : AccessorAttribute
        {
            public override Boolean Read(IFormatterX formatter, AccessorContext context)
            {
                Assert.AreEqual("Name", context.Member.Name);

                var v = context.Value as MyModelWithMemberAccessorAtt;

                var bn = formatter as Binary;
                v.Name = bn.Read<String>();

                return true;
            }

            public override Boolean Write(IFormatterX formatter, AccessorContext context)
            {
                Assert.AreEqual("Name", context.Member.Name);

                var v = context.Value as MyModelWithMemberAccessorAtt;

                var bn = formatter as Binary;
                bn.Write(v.Name);

                return true;
            }
        }

        [Test]
        public void FixedString()
        {
            var model = new MyModelWithFixed { Code = 1234, Name = "Stone" };

            var bn = new Binary { EncodeInt = true };
            bn.Write(model);
            var pk = bn.GetPacket();
            Assert.AreEqual(10, pk.Total);
            Assert.AreEqual("D20953746F6E65000000", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true };
            var model2 = bn2.Read<MyModelWithFixed>();
            Assert.AreEqual(model.Code, model2.Code);
            Assert.AreEqual(model.Name, model2.Name);
        }

        private class MyModelWithFixed
        {
            public Int32 Code { get; set; }

            [FixedString(8)]
            public String Name { get; set; }
        }

        [Test]
        public void ReadDateTime()
        {
            var dt = DateTime.Now;
            var n1 = dt.ToInt();

            var pk = Binary.FastWrite(dt);

            var dt2 = Binary.FastRead<DateTime>(pk.GetStream());
            var n2 = dt2.ToInt();

            Assert.AreEqual(dt.Trim(), dt2);
            Assert.AreEqual(n1, n2);
        }

        [Test]
        public void ReadDateTime2()
        {
            var dt = new DateTime(2038, 12, 31);
            //var n1 = dt.ToInt();

            var pk = Binary.FastWrite(dt);

            var dt2 = Binary.FastRead<DateTime>(pk.GetStream());
            //var n2 = dt2.ToInt();

            Assert.AreEqual(dt.Trim(), dt2);
            //Assert.AreEqual(n1, n2);
        }
    }
}
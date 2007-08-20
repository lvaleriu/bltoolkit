using System;

#if FW2
using System.Collections.Generic;
#endif

using NUnit.Framework;

using BLToolkit.DataAccess;
using BLToolkit.Mapping;

namespace DataAccess
{
	[TestFixture]
	public class XmlTypeTest
	{
		public class TestObject
		{
			[MapField("DataTypeID")]
			public int       ID;

			[MapField("Xml_")]
			public string    Xml;
		}

		public abstract class TestAccessor : DataAccessor
		{
#if FW2
			[SqlQuery("Select * FROM DataTypeTest")]
			public abstract List<TestObject> LoadAll();
#endif

#if ORACLE
			[SqlQuery("SELECT * FROM DataTypeTest WHERE DataTypeID=:pID")]
#else
			[SqlQuery("SELECT * FROM DataTypeTest WHERE DataTypeID=@ID")]
#endif
			public abstract TestObject       LoadById(int ID); 

		}

		[Test]
		public void Test()
		{
			TestAccessor ta = (TestAccessor)DataAccessor.CreateInstance(typeof(TestAccessor));
			TestObject   o  = ta.LoadById(2);

			Assert.IsNotNull(o);
			Assert.IsNotNull(o.Xml);

#if FW2
			List<TestObject> lst = ta.LoadAll();
			Assert.IsNotEmpty(lst);
			Assert.IsNotNull(lst[0].Xml);
#endif

		}
	}
}
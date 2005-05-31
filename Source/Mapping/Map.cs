/*
 * File:    Map.cs
 * Created: 5/1/2003
 * Author:  Igor Tkachev
 *          mailto:it@rsdn.ru
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Reflection;

namespace Rsdn.Framework.Data.Mapping
{
	/// <summary>
	/// The <b>Map</b> is a helper class 
	/// that is used to perform relational data mapping to business entities.
	/// </summary>
	/// <remarks>
	/// The <b>Map</b> class is used by the <see cref="DbManager"/> class 
	/// to perform the mapping operations. However, it can be easily used independently.
	/// </remarks>
	public class Map
	{
		static char[] _trimArray = new char[0];

		/// <summary>
		/// Maps the source value to one of enumeration values.
		/// </summary>
		/// <remarks>
		/// The method scans the <see cref="MapValueAttribute">MapValue</see> attributes of the provided 
		/// enumeration type and returns an associated enumeration for the provided source value.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/map[@name="ToValue(object,Type)"]/*' />
		/// <param name="sourceValue">A source value.</param>
		/// <param name="type">An enumeration type.</param>
		/// <returns>One of enumeration values.</returns>
		public static object ToValue(object sourceValue, Type type)
		{
			try
			{
				object[] attributes = MapDescriptor.GetValueAttributes(type); // GetCustomAttributes(type);

				return BaseMemberMapper.MapFrom(type, (Attribute[])attributes, sourceValue, true);
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps an enumeration to its associated value.
		/// </summary>
		/// <remarks>
		/// The method scans the <see cref="MapValueAttribute">MapValue</see> attributes of the provided 
		/// enumeration and returns its associated value.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/map[@name="FromValue(object)"]/*' />
		/// <param name="sourceValue">One of enumeration values.</param>
		/// <returns>Mapped value.</returns>
		public static object FromValue(object sourceValue)
		{
			try
			{
				Type type = sourceValue.GetType();

				object[] attributes = MapDescriptor.GetValueAttributes(type);

				return BaseMemberMapper.MapTo((Attribute[])attributes, sourceValue, true);
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the source value to one of enumeration values.
		/// </summary>
		/// <remarks>
		/// The method scans the <see cref="MapValueAttribute">MapValue</see> attributes of the provided 
		/// enumeration type and returns an associated enumeration for the provided source value.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/map[@name="ToValue(object,Type)"]/*' />
		/// <param name="sourceValue">A source value.</param>
		/// <param name="type">An enumeration type.</param>
		/// <returns>One of enumeration values.</returns>
		public static Enum ToEnum(object sourceValue, Type type)
		{
			return (Enum)ToValue(sourceValue, type);
		}

		/// <summary>
		/// Maps an enumeration to its associated value.
		/// </summary>
		/// <remarks>
		/// The method scans the <see cref="MapValueAttribute">MapValue</see> attributes of the provided 
		/// enumeration and returns its associated value.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/map[@name="FromValue(object)"]/*' />
		/// <param name="sourceValue">One of enumeration values.</param>
		/// <returns>Mapped value.</returns>
		public static object FromEnum(Enum sourceValue)
		{
			return FromValue(sourceValue);
		}

		/// <summary>
		/// Checks an object if it equals <i>null</i> value.
		/// </summary>
		/// <remarks>
		/// The following table contains a list of types and values that are considered as null values:
		/// <list type="table">
		/// <listheader><term>Type</term><description>Value</description></listheader>
		/// <item><term>Object</term><description>null</description></item>
		/// <item><term>String</term><description><see cref="String.Length">Length</see> == 0</description></item>
		/// <item><term>DateTime</term><description><see cref="DateTime.MinValue">DateTime.MinValue</see></description></item>
		/// <item><term>Int16</term><description>0</description></item>
		/// <item><term>Int32</term><description>0</description></item>
		/// <item><term>Int64</term><description>0</description></item>
		/// </list>
		/// </remarks>
		/// <param name="value">A tested object.</param>
		/// <returns>True, if the object is null.</returns>
		public static bool IsNull(object value)
		{
			return
				value == null ||
				value is String   && ((String)  value).TrimEnd(_trimArray).Length == 0   ||
				value is DateTime && ((DateTime)value) == DateTime.MinValue ||
				value is Int16    && ((Int16)   value) == 0 ||
				value is Int32    && ((Int32)   value) == 0 ||
				value is Int64    && ((Int64)   value) == 0;
		}

		private static object MapInternal(MapDescriptor md, MapInitializingData data)
		{
			object dest = md.CreateInstanceEx(data);

			if (!data.StopMapping)
			{
				MapInternal(data.DataSource, data.SourceData, md, dest);
			}

			return dest;
		}

		internal static void MapInternal(
			IMapDataSource   source,
			object           sourceData,
			IMapDataReceiver receiver,
			object           receiverData)
		{
			if (receiverData is ISupportInitialize)
				((ISupportInitialize)receiverData).BeginInit();

			if (receiverData is IMapSettable)
			{
				IMapSettable mapReceiver = receiverData as IMapSettable;

				for (int i = 0; i < source.FieldCount; i++)
				{
					string name = source.GetFieldName(i);
					object o    = source.GetFieldValue(i, sourceData);

					if (mapReceiver.SetField(name, o) == false)
					{
						int idx = receiver.GetOrdinal(name);

						if (idx >= 0)
						{
							receiver.SetFieldValue(idx, name, receiverData, o);
						}
					}
				}
			}
			else
			{
				for (int i = 0; i < source.FieldCount; i++)
				{
					string name = source.GetFieldName(i);

					// -----------------------------------
					if (name.Length > 0)
					{
						int idx  = receiver.GetOrdinal(name);

						if (idx >= 0)
						{
							object o = source.GetFieldValue(i, sourceData);

							receiver.SetFieldValue(idx, name, receiverData, o);
						}
					}
				}
			}
		
			if (receiverData is ISupportInitialize)
				((ISupportInitialize)receiverData).EndInit();
		}

		private static IMapDataSource GetDataSource(object obj)
		{
			if (obj is DataRow)
				return new DataRowReader(obj as DataRow);

			if (obj is DataTable)
				return new DataRowReader((obj as DataTable).Rows[0]);

			if (obj is IDataReader)
				return new DataReaderSource(obj as IDataReader);

			return MapDescriptor.GetDescriptor(obj.GetType());
		}

		private static IMapDataReceiver GetDataReceiver(object obj)
		{
			if (obj is DataRow)
				return new DataRowReader(obj as DataRow);

			if (obj is DataTable)
			{
				DataTable dt = obj as DataTable;
				DataRow   dr = dt.NewRow();

				dt.Rows.Add(dr);

				return new DataRowReader(dr);
			}

			return MapDescriptor.GetDescriptor(obj.GetType());
		}

		internal static void HandleException(Exception ex)
		{
			if (ex is RsdnDataException || ex is ArgumentNullException)
			{
				throw ex;
			}
			else
			{
				throw new RsdnMapException(ex.Message, ex);
			}
		}

		/// <summary>
		/// Maps an object to another object.
		/// </summary>
		/// <remarks>
		/// The <paramref name="source"/> and <paramref name="dest"/> parameters can be <see cref="DataRow"/>, 
		/// <see cref="DataTable"/>, or business entity. 
		/// In case the <paramref name="dest"/> is a <see cref="DataTable"/>, 
		/// new <see cref="DataRow"/> is created and populated from the <paramref name="source"/>.
		/// In case the <paramref name="source"/> is a <see cref="DataTable"/>, 
		/// the first row is taken as a source object.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/map[@name="ToObject(object,object)"]/*' />
		/// <param name="source">The source object.</param>
		/// <param name="dest">The destination object.</param>
		/// <returns>The destination object.</returns>
		public static object ToObject(object source, object dest)
		{
			try
			{
				MapInternal(GetDataSource(source), source, GetDataReceiver(dest), dest);

				return dest;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps an object to a business entity.
		/// </summary>
		/// <remarks>
		/// The <paramref name="source"/> parameter can be <see cref="DataRow"/>, 
		/// <see cref="DataTable"/>, or business entity. 
		/// In case the <paramref name="source"/> is a <see cref="DataTable"/>, 
		/// the first row is taken as a source object.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/map[@name="ToObject(object,Type)"]/*' />
		/// <param name="source">The source object.</param>
		/// <param name="type">Type of the business object.</param>
		/// <returns>A business object.</returns>
		public static object ToObject(object source, Type type)
		{
			return ToObject(source, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static object ToObject(
			object source,
			Type   type,
			params object[] parameters)
		{
			try
			{
				return MapInternal(
					MapDescriptor.GetDescriptor(type),
					new MapInitializingData(GetDataSource(source), source, parameters));
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="DataRow"/> to a business object.
		/// </summary>
		/// <remarks>
		/// The <paramref name="dest"/> parameter can be <see cref="DataRow"/>, 
		/// <see cref="DataTable"/>, or business entity. 
		/// In case the <paramref name="dest"/> is a <see cref="DataTable"/>, 
		/// new <see cref="DataRow"/> is created and populated from the <paramref name="source"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             DataTable table = new DataTable();
		///             
		///             table.Columns.Add("ID",   typeof(int));
		///             table.Columns.Add("Name", typeof(string));
		///             
		///             table.Rows.Add(new object[] { 1, "Example" });
		///             table.AcceptChanges();   
		///             table.Rows[0].Delete();
		///             
		///             DataRow[] rows = table.Select(null, null, DataViewRowState.Deleted);
		///             
		///             BizEntity entity = new BizEntity();
		///             
		///             Map.ToObject(rows[0], DataRowVersion.Original, entity);
		///             
		///             Console.WriteLine("ID  : {0}", entity.ID);
		///             Console.WriteLine("Name: {0}", entity.Name);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="dataRow">An instance of the <see cref="DataRow"/>.</param>
		/// <param name="version">One of the <see cref="DataRowVersion"/> values 
		/// that specifies the desired row version. 
		/// Possible values are Default, Original, Current, and Proposed.</param>
		/// <param name="dest">An object to be populated.</param>
		/// <returns>A business object.</returns>
		public static object ToObject(DataRow dataRow, DataRowVersion version, object dest)
		{
			try
			{
				MapInternal(new DataRowReader(dataRow, version), dataRow, GetDataReceiver(dest), dest);

				return dest;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="DataRow"/> to a business object.
		/// </summary>
		/// <remarks>
		/// The <paramref name="dest"/> parameter can be <see cref="DataRow"/>, 
		/// <see cref="DataTable"/>, or business entity. 
		/// In case the <paramref name="dest"/> is a <see cref="DataTable"/>, 
		/// new <see cref="DataRow"/> is created and populated from the <paramref name="source"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             DataTable table = new DataTable();
		///             
		///             table.Columns.Add("ID",   typeof(int));
		///             table.Columns.Add("Name", typeof(string));
		///             
		///             table.Rows.Add(new object[] { 1, "Example" });
		///             table.AcceptChanges();   
		///             table.Rows[0].Delete();
		///             
		///             DataRow[] rows = table.Select(null, null, DataViewRowState.Deleted);
		///             
		///             BizEntity entity = (BizEntity)Map.ToObject(
		///                 rows[0], DataRowVersion.Original, typeof(BizEntity));
		///                 
		///             Console.WriteLine("ID  : {0}", entity.ID);
		///             Console.WriteLine("Name: {0}", entity.Name);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="type">A business object type.</param>
		/// <param name="dataRow">An instance of the <see cref="DataRow"/>.</param>
		/// <param name="version">One of the <see cref="DataRowVersion"/> values 
		/// that specifies the desired row version. 
		/// Possible values are Default, Original, Current, and Proposed.</param>
		/// <returns>A business object.</returns>
		public static object ToObject(DataRow dataRow, DataRowVersion version, Type type)
		{
			return ToObject(dataRow, version, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataRow"></param>
		/// <param name="version"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static object ToObject(
			DataRow         dataRow,
			DataRowVersion  version,
			Type            type,
			params object[] parameters)
		{
			try
			{
				return MapInternal(
					MapDescriptor.GetDescriptor(type),
					new MapInitializingData(new DataRowReader(dataRow, version), dataRow, parameters));
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="IList"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="IList"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Collections;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             DataTable table = new DataTable();
		///             
		///             table.Columns.Add("ID",   typeof(int));
		///             table.Columns.Add("Name", typeof(string));
		///             
		///             table.Rows.Add(new object[] { 1, "Example" });
		///             
		///             ArrayList array = new ArrayList();
		///             
		///             Map.ToList(table, array, typeof(BizEntity));
		///             
		///             Console.WriteLine("ID  : {0}", ((BizEntity)array[0]).ID);
		///             Console.WriteLine("Name: {0}", ((BizEntity)array[0]).Name);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="sourceTable">An instance of the <see cref="DataTable"/>.</param>
		/// <param name="list">An instance of the <see cref="IList"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An <see cref="IList"/>.</returns>
		public static IList ToList(
			DataTable sourceTable,
			IList     list,
			Type      type)
		{
			return ToList(sourceTable, list, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceTable"></param>
		/// <param name="list"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IList ToList(
			DataTable sourceTable,
			IList     list,
			Type      type,
			params object[] parameters)
		{
			try
			{
				DataRowReader       rr   = new DataRowReader(null);
				MapDescriptor       md   = MapDescriptor.GetDescriptor(type);
				MapInitializingData data = new MapInitializingData(rr, null, parameters);

				foreach (DataRow dr in sourceTable.Rows)
				{
					if (dr.RowState != DataRowState.Deleted)
					{
						rr.DataRow = dr;
						data.SetSourceData(dr);
						list.Add(MapInternal(md, data));
					}
				}

				return list;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="IList"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="IList"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Collections;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             DataTable table = new DataTable();
		///             
		///             table.Columns.Add("ID",   typeof(int));
		///             table.Columns.Add("Name", typeof(string));
		///             
		///             table.Rows.Add(new object[] { 1, "Example" });
		///             table.AcceptChanges();
		///             table.Rows[0].Delete();
		///             
		///             ArrayList array = new ArrayList();
		///             
		///             Map.ToList(table, DataRowVersion.Original, array, typeof(BizEntity));
		///                 
		///             Console.WriteLine("ID  : {0}", ((BizEntity)array[0]).ID);
		///             Console.WriteLine("Name: {0}", ((BizEntity)array[0]).Name);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="sourceTable">An instance of the <see cref="DataTable"/>.</param>
		/// <param name="version">One of the <see cref="DataRowVersion"/> values 
		/// that specifies the desired row version. 
		/// Possible values are Default, Original, Current, and Proposed.</param>
		/// <param name="list">An instance of the <see cref="IList"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An <see cref="IList"/>.</returns>
		public static IList ToList(
			DataTable      sourceTable,
			DataRowVersion version,
			IList          list,
			Type           type)
		{
			return ToList(sourceTable, version, list, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceTable"></param>
		/// <param name="version"></param>
		/// <param name="list"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IList ToList(
			DataTable       sourceTable,
			DataRowVersion  version,
			IList           list,
			Type            type,
			params object[] parameters)
		{
			try
			{
				DataRowReader       rr   = new DataRowReader(null, version);
				MapDescriptor       md   = MapDescriptor.GetDescriptor(type);
				MapInitializingData data = new MapInitializingData(rr, null, parameters);

				foreach (DataRow dr in sourceTable.Rows)
				{
					rr.DataRow = dr;
					data.SetSourceData(dr);
					list.Add(MapInternal(md, data));
				}

				return list;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="ArrayList"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="ArrayList"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Collections;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             DataTable table = new DataTable();
		///             
		///             table.Columns.Add("ID",   typeof(int));
		///             table.Columns.Add("Name", typeof(string));
		///             
		///             table.Rows.Add(new object[] { 1, "Example" });
		///             
		///             ArrayList array = Map.ToList(table, typeof(BizEntity));
		///             
		///             Console.WriteLine("ID  : {0}", ((BizEntity)array[0]).ID);
		///             Console.WriteLine("Name: {0}", ((BizEntity)array[0]).Name);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="dataTable">An instance of the <see cref="DataTable"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An array of business objects.</returns>
		public static ArrayList ToList(DataTable dataTable, Type type)
		{
			ArrayList arrayList = new ArrayList();

			ToList(dataTable, arrayList, type);

			return arrayList;
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="ArrayList"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="ArrayList"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Collections;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             DataTable table = new DataTable();
		///             
		///             table.Columns.Add("ID",   typeof(int));
		///             table.Columns.Add("Name", typeof(string));
		///             
		///             table.Rows.Add(new object[] { 1, "Example" });
		///             table.AcceptChanges();
		///             table.Rows[0].Delete();
		///             
		///             ArrayList array = Map.ToList(
		///                 table, DataRowVersion.Original, typeof(BizEntity));
		///                 
		///             Console.WriteLine("ID  : {0}", ((BizEntity)array[0]).ID);
		///             Console.WriteLine("Name: {0}", ((BizEntity)array[0]).Name);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="dataTable">An istance of the <see cref="DataTable"/>.</param>
		/// <param name="version">One of the <see cref="DataRowVersion"/> values 
		/// that specifies the desired row version. 
		/// Possible values are Default, Original, Current, and Proposed.</param>
		/// <param name="type">Type of the business object.</param>
		/// <returns>An array of business objects.</returns>
		public static ArrayList ToList(
			DataTable dataTable, DataRowVersion version, Type type)
		{
			ArrayList arrayList = new ArrayList();

			ToList(dataTable, version, arrayList, type);

			return arrayList;
		}

		/// <summary>
		/// Maps the <see cref="IList"/> to the <see cref="DataTable"/>.
		/// </summary>
		/// <remarks>
		/// The method populates the <see cref="DataTable"/> from the provided <see cref="IList"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Collections;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             ArrayList array  = new ArrayList();
		///             BizEntity entity = new BizEntity();
		///             
		///             entity.ID   = 1;
		///             entity.Name = "Example";
		///             
		///             array.Add(entity);
		///             
		///             DataTable table = Map.ToList(array);
		///             
		///             Console.WriteLine("ID  : {0}", table.Rows[0]["ID"]);
		///             Console.WriteLine("Name: {0}", table.Rows[0]["Name"]);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="list">The resultant array.</param>
		/// <returns>An array of business objects.</returns>
		public static DataTable ToList(IList list)
		{
			return ToList(list, new DataTable());
		}

		/// <summary>
		/// Maps the <see cref="IList"/> to the <see cref="DataTable"/>.
		/// </summary>
		/// <remarks>
		/// The method populates the <see cref="DataTable"/> from the provided <see cref="IList"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Collections;
		/// using System.Data;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class BizEntity
		///     {
		///         public int    ID;
		///         public string Name;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             DataTable table = new DataTable();
		///             
		///             table.Columns.Add("ID",   typeof(int));
		///             table.Columns.Add("Name", typeof(string));
		///             
		///             ArrayList array  = new ArrayList();
		///             BizEntity entity = new BizEntity();
		///             
		///             entity.ID   = 1;
		///             entity.Name = "Example";
		///             
		///             array.Add(entity);
		///             
		///             Map.ToList(array, table);
		///             
		///             Console.WriteLine("ID  : {0}", table.Rows[0]["ID"]);
		///             Console.WriteLine("Name: {0}", table.Rows[0]["Name"]);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="list">A source array.</param>
		/// <param name="dataTable">A destination <see cref="DataTable"/>.</param>
		/// <returns>A <see cref="DataTable"/> object.</returns>
		public static DataTable ToList(
			IList     list,
			DataTable dataTable)
		{
			try
			{
				DataRowReader rr = new DataRowReader(null);

				foreach (object o in list)
				{
					rr.DataRow = dataTable.NewRow();
				
					MapInternal(MapDescriptor.GetDescriptor(o.GetType()), o, rr, rr.DataRow);
				
					dataTable.Rows.Add(rr.DataRow);
				}

				return dataTable;	
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="IDataReader"/> to the <see cref="IList"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="IList"/>.
		/// </remarks>
		/// <example>
		/// The following example demonstrates how to use the method.
		/// <code>
		/// using System;
		/// using System.Data;
		/// using System.Collections;
		/// 
		/// using Rsdn.Framework.Data.Mapping;
		/// 
		/// namespace Example
		/// {
		///     public class Category
		///     {
		///         [MapField(Name = "CategoryID")]
		///         public int    ID;
		///         
		///         public string CategoryName;
		///         
		///         [MapField(IsNullable = true)]
		///         public string Description;
		///     }
		///     
		///     class Test
		///     {
		///         static void Main()
		///         {
		///             using (DbManager   db = new DbManager())
		///             using (IDataReader dr = db.ExecuteReader(@"
		///                 SELECT
		///                     CategoryID,
		///                     CategoryName,
		///                     Description
		///                 FROM Categories
		///                 
		///                 SELECT
		///                     CategoryID,
		///                     CategoryName
		///                 FROM Categories"))
		///             {
		///                 ArrayList al = Map.ToList(dr, typeof(Category));
		///                 
		///                 Print(al);
		///                         
		///                 if (dr.NextResult())
		///                 {
		///                     Map.ToList(dr, al, typeof(Category));
		///                     
		///                     Print(al);
		///                 }
		///             }
		///         }
		///         
		///         static void Print(ArrayList al)
		///         {
		///             foreach (Category c in al)
		///             {
		///                 Console.WriteLine(
		///                     "{0,2}  {1,-15} {2}", c.ID, c.CategoryName, c.Description);
		///             }
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		/// <param name="reader">An instance of the <see cref="IDataReader"/>.</param>
		/// <param name="list">An instance of the <see cref="IList"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An <see cref="IList"/>.</returns>
		public static IList ToList(IDataReader reader, IList list, Type type)
		{
			return ToList(reader, list, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="list"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IList ToList(
			IDataReader     reader,
			IList           list,
			Type            type,
			params object[] parameters)
		{
			try
			{
				if (reader.Read())
				{
					DataReaderSource    drs  = new DataReaderSource(reader);
					MapDescriptor       md   = MapDescriptor.GetDescriptor(type);
					MapInitializingData data = new MapInitializingData(drs, reader, parameters);

					do
					{
						list.Add(MapInternal(md, data));
					}
					while (reader.Read());
				}

				return list;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="IDataReader"/> to the <see cref="ArrayList"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="ArrayList"/>.
		/// See the <see cref="ToList(IDataReader,IList,Type)"/> method to find an example.
		/// </remarks>
		/// <param name="reader">An instance of the <see cref="IDataReader"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An array of business objects.</returns>
		public static ArrayList ToList(
			IDataReader reader,
			Type type)
		{
			ArrayList arrayList = new ArrayList();

			ToList(reader, arrayList, type);

			return arrayList;
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="IDictionary"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="IDictionary"/>.
		/// </remarks>
		/// <param name="sourceTable">An instance of the <see cref="DataTable"/>.</param>
		/// <param name="dictionary"><see cref="IDictionary"/> object to be populated.</param>
		/// <param name="keyFieldName">The field name that is used as a key to populate <see cref="IDictionary"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An instance of the <see cref="IDictionary"/>.</returns>
		public static IDictionary ToDictionary(
			DataTable   sourceTable,
			IDictionary dictionary,
			string      keyFieldName,
			Type        type)
		{
			return ToDictionary(sourceTable, dictionary, keyFieldName, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceTable"></param>
		/// <param name="dictionary"></param>
		/// <param name="keyFieldName"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IDictionary ToDictionary(
			DataTable       sourceTable,
			IDictionary     dictionary,
			string          keyFieldName,
			Type            type,
			params object[] parameters)
		{
			try
			{
				DataRowReader       rr   = new DataRowReader(null);
				MapDescriptor       md   = MapDescriptor.GetDescriptor(type);
				MapInitializingData data = new MapInitializingData(rr, null, parameters);

				foreach (DataRow dr in sourceTable.Rows)
				{
					if (dr.RowState != DataRowState.Deleted)
					{
						rr.DataRow = dr;
						data.SetSourceData(dr);
						dictionary.Add(dr[keyFieldName], MapInternal(md, data));
					}
				}

				return dictionary;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="IDictionary"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="IDictionary"/>.
		/// </remarks>
		/// <param name="sourceTable">An instance of the <see cref="DataTable"/>.</param>
		/// <param name="version">One of the <see cref="DataRowVersion"/> values 
		/// that specifies the desired row version. 
		/// Possible values are Default, Original, Current, and Proposed.</param>
		/// <param name="dictionary"><see cref="IDictionary"/> object to be populated.</param>
		/// <param name="keyFieldName">The field name that is used as a key to populate <see cref="IDictionary"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An instance of the <see cref="IDictionary"/>.</returns>
		public static IDictionary ToDictionary(
			DataTable      sourceTable,
			DataRowVersion version,
			IDictionary    dictionary,
			string         keyFieldName,
			Type           type)
		{
			return ToDictionary(sourceTable, version, dictionary, keyFieldName, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceTable"></param>
		/// <param name="version"></param>
		/// <param name="dictionary"></param>
		/// <param name="keyFieldName"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IDictionary ToDictionary(
			DataTable       sourceTable,
			DataRowVersion  version,
			IDictionary     dictionary,
			string          keyFieldName,
			Type            type,
			params object[] parameters)
		{
			try
			{
				DataRowReader       rr   = new DataRowReader(null, version);
				MapDescriptor       md   = MapDescriptor.GetDescriptor(type);
				MapInitializingData data = new MapInitializingData(rr, null, parameters);

				foreach (DataRow dr in sourceTable.Rows)
				{
					rr.DataRow = dr;
					data.SetSourceData(dr);
					dictionary.Add(dr[keyFieldName], MapInternal(md, data));
				}

				return dictionary;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="Hashtable"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="Hashtable"/>.
		/// </remarks>
		/// <param name="dataTable">An instance of the <see cref="DataTable"/>.</param>
		/// <param name="keyFieldName">The field name that is used as a key to populate <see cref="IDictionary"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An instance of the <see cref="Hashtable"/> class.</returns>
		public static Hashtable ToDictionary(
			DataTable dataTable,
			string    keyFieldName,
			Type      type)
		{
			Hashtable hash = new Hashtable();

			ToDictionary(dataTable, hash, keyFieldName, type);

			return hash;
		}

		/// <summary>
		/// Maps the <see cref="DataTable"/> to the <see cref="Hashtable"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="Hashtable"/>.
		/// </remarks>
		/// <param name="dataTable">An instance of the <see cref="DataTable"/>.</param>
		/// <param name="version">One of the <see cref="DataRowVersion"/> values 
		/// that specifies the desired row version. 
		/// Possible values are Default, Original, Current, and Proposed.</param>
		/// <param name="keyFieldName">The field name that is used as a key to populate <see cref="IDictionary"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An instance of the <see cref="Hashtable"/> class.</returns>
		public static Hashtable ToDictionary(
			DataTable      dataTable,
			DataRowVersion version,
			string         keyFieldName,
			Type           type)
		{
			Hashtable ht = new Hashtable();

			ToDictionary(dataTable, version, ht, keyFieldName, type);

			return ht;
		}

		/// <summary>
		/// Maps the <see cref="IDataReader"/> to the <see cref="Hashtable"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="Hashtable"/>.
		/// </remarks>
		/// <param name="reader">An instance of the <see cref="IDataReader"/>.</param>
		/// <param name="keyFieldName">The field name that is used as a key to populate <see cref="Hashtable"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An instance of the <see cref="Hashtable"/>.</returns>
		public static Hashtable ToDictionary(
			IDataReader reader,
			string      keyFieldName,
			Type        type)
		{
			Hashtable ht = new Hashtable();

			ToDictionary(reader, ht, keyFieldName, type, (object[])null);

			return ht;
		}

		/// <summary>
		/// Maps the <see cref="IDataReader"/> to the <see cref="IDictionary"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="IDictionary"/>.
		/// </remarks>
		/// <param name="reader">An instance of the <see cref="IDataReader"/>.</param>
		/// <param name="dictionary"><see cref="IDictionary"/> object to be populated.</param>
		/// <param name="keyFieldName">The field name that is used as a key to populate <see cref="IDictionary"/>.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An instance of the <see cref="IDictionary"/>.</returns>
		public static IDictionary ToDictionary(
			IDataReader reader,
			IDictionary dictionary,
			string      keyFieldName,
			Type        type)
		{
			return ToDictionary(reader, dictionary, keyFieldName, type, (object[])null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="dictionary"></param>
		/// <param name="keyFieldName"></param>
		/// <param name="type"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static IDictionary ToDictionary(
			IDataReader     reader,
			IDictionary     dictionary,
			string          keyFieldName,
			Type            type,
			params object[] parameters)
		{
			try
			{
				if (reader.Read())
				{
					DataReaderSource    drs  = new DataReaderSource(reader);
					MapDescriptor       md   = MapDescriptor.GetDescriptor(type);
					MapInitializingData data = new MapInitializingData(drs, reader, parameters);

					do
					{
						dictionary.Add(reader[keyFieldName], MapInternal(md, data));
					}
					while (reader.Read());
				}

				return dictionary;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// Maps the <see cref="IDataReader"/> to the <see cref="IDictionary"/>.
		/// </summary>
		/// <remarks>
		/// The method creates objects of the provided type and adds them to the <see cref="IDictionary"/>.
		/// </remarks>
		/// <param name="reader">An instance of the <see cref="IDataReader"/>.</param>
		/// <param name="dictionary"><see cref="IDictionary"/> object to be populated.</param>
		/// <param name="type">A type of the objects to be created.</param>
		/// <returns>An instance of the <see cref="Hashtable"/> class.</returns>
		public static Hashtable ToDictionary(
			IDataReader reader,
			IDictionary dictionary,
			Type        type)
		{
			Hashtable hash = new Hashtable();

			ToDictionary(reader, hash, type);

			return hash;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceTable"></param>
		/// <param name="targetTable"></param>
		/// <returns></returns>
		public static DataTable ToDataTable(
			DataTable sourceTable,
			DataTable targetTable)
		{
			try
			{
				DataRowReader sr = new DataRowReader(null);
				DataRowReader tr = new DataRowReader(null);

				foreach (DataRow sourceRow in sourceTable.Rows)
				{
					DataRow targetRow = targetTable.NewRow();

					sr.DataRow = sourceRow;
					tr.DataRow = targetRow;
					MapInternal(sr, sourceRow, tr, targetRow);

					targetTable.Rows.Add(targetRow);
				}

				return targetTable;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="table"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static DataTable ToDataTable(
			IDataReader     reader,
			DataTable       table,
			params object[] parameters)
		{
			try
			{
				if (reader.Read())
				{
					DataReaderSource drs = new DataReaderSource(reader);
					DataRowReader    tr  = new DataRowReader(null);

					do
					{
						DataRow row = table.NewRow();

						tr.DataRow = row;
						MapInternal(drs, reader, tr, parameters);
						table.Rows.Add(row);
					} 
					while (reader.Read());
				}

				return table;
			}
			catch (Exception ex)
			{
				HandleException(ex);
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public static MapDescriptor Descriptor(Type type)
		{
			return MapDescriptor.GetDescriptor(type);
		}
	}
}

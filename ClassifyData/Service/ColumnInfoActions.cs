using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Vidyano.Service.ClientOperations;
using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
	public class ColumnInfoActions : PersistentObjectActionsReference<ClassifyDataEntityModelContainer, ColumnInfo>
	{
		/// <summary>
		/// Maps to <see cref="ColumnInfo"/>
		/// </summary>
		private const string infoFormat = @"use [{0}];
select 
	  schema_name(t.schema_id) [Schema]
	, object_name(t.object_id) [Table]
	, c.name                   [Column]
	, ty.name                  [Type]
	, t_id.value               [InformationTypeId]
	, t_name.value             [InformationTypeName]
	, l_id.value               [SensitivityLabelId]
	, l_name.value             [SensitivityLabelName]
	, d.value                  [Description]
from sys.tables                   t
inner join sys.columns            c                on c.object_id     = t.object_id
inner join sys.types              ty               on ty.user_type_id = c.user_type_id
left join sys.extended_properties t_id             on c.object_id     = t_id.major_id   and c.column_id = t_id.minor_id          and t_id.name          = 'sys_information_type_id'
left join sys.extended_properties t_name           on c.object_id     = t_name.major_id and c.column_id = t_name.minor_id        and t_name.name        = 'sys_information_type_name'
left join sys.extended_properties l_id             on c.object_id     = l_id.major_id   and c.column_id = l_id.minor_id          and l_id.name          = 'sys_sensitivity_label_id'
left join sys.extended_properties l_name           on c.object_id     = l_name.major_id and c.column_id = l_name.minor_id        and l_name.name        = 'sys_sensitivity_label_name'
left join sys.extended_properties d                on c.object_id     = d.major_id      and c.column_id = d.minor_id             and d.name             = 'description'
";

		private const string selectFormat = infoFormat + " where schema_name(t.schema_id) = @p1 collate database_default and object_name(t.object_id) = @p2 collate database_default and c.name = @p0 collate database_default";

		private const string upsertExtendedProperty = @"
declare @tableid  int = object_id(@schemaname + '.' + @tablename);
declare @columnid int = (select [column_id] from sys.columns where [name] = @columnname and [object_id] = @tableid);

if exists 
	(select null
		from sys.extended_properties
		where [major_id] = @tableid
		and [minor_id] = @columnid
		and [name]     = @name
	)
	begin

		execute sys.sp_updateextendedproperty
			@name       = @name, 
			@value      = @value, 
			@level0type = N'Schema', 
			@level0name = @schemaname, 
			@level1type = N'Table',  
			@level1name = @tablename, 
			@level2type = N'Column', 
			@level2name = @columnname;
	end
else
	begin

		exec sys.sp_addextendedproperty  
			@name       = @name, 
			@value      = @value, 
			@level0type = N'Schema', 
			@level0name = @schemaname, 
			@level1type = N'Table',  
			@level1name = @tablename, 
			@level2type = N'Column', 
			@level2name = @columnname
	end";

		private const string tablesFormat = @"use [{0}];
			select
				sum(case when a.count = 0 then 0 else 1 end) Count
			  , count(0) Total
			from (
				select sum(case when t_id.name is null then 0 else 1 end) count
				from sys.tables t
				inner join sys.columns c               on c.object_id = t.object_id
				left join sys.extended_properties t_id on c.object_id = t_id.major_id
													  and c.column_id = t_id.minor_id
													  and t_id.name = 'sys_information_type_id'
				group by t.object_id
				) a";
		private const string columnsFormat = @"use [{0}];
			select sum(case when t_id.name is null then 0 else 1 end) [Count], count(0) [Total]
			from sys.tables t
			inner join sys.columns c on c.object_id = t.object_id
			left join sys.extended_properties t_id on c.object_id = t_id.major_id
												  and c.column_id = t_id.minor_id
												  and t_id.name = 'sys_information_type_id'";

		public override void OnConstruct(Query query, PersistentObject parent)
		{
			base.OnConstruct(query, parent);

			query.EnableSelectAll = true;
		}

		// ReSharper disable once RedundantOverriddenMember
		public override void OnBulkConstruct(PersistentObject obj, QueryResultItem[] selectedItems)
		{
			base.OnBulkConstruct(obj, selectedItems);
		}

		protected override ColumnInfo LoadEntity(PersistentObject obj, bool forRefresh = false)
		{
			var database = obj.Parent.ObjectId;
			var ids = obj.ObjectId.Split(';');
			return Context.Database.SqlQuery<ColumnInfo>(string.Format(selectFormat, database), ids[0], ids[1], ids[2]).First();
		}

		public override void OnSave(PersistentObject obj)
		{
			if (!CheckRules(obj))
				return;

		    var changedProperties = Empty<string>.Array.Select(_ => new { name = default(string), value = default(string) }).ToList();

			var informationTypeIdAttr = obj["InformationTypeId"];
			var sensitivityLabelIdAttr = obj["SensitivityLabelId"];
		    var descriptionAttr = obj["Description"];

		    if (informationTypeIdAttr.IsValueChanged)
		    {
		        var informationTypeId = (string)informationTypeIdAttr ?? string.Empty;
		        var informationTypeName = informationTypeId.Length > 0 ? informationTypeIdAttr.Options.First(o => o.StartsWith(informationTypeId + "=")).Split('=')[1] : string.Empty;
		        changedProperties.Add(new { name = "sys_information_type_id",   value = informationTypeId   });
		        changedProperties.Add(new { name = "sys_information_type_name", value = informationTypeName });
		    }

		    if (sensitivityLabelIdAttr.IsValueChanged)
		    {
		        var sensitivityLabelId = (string)sensitivityLabelIdAttr ?? string.Empty;
		        var sensitivityLabelName = sensitivityLabelId.Length > 0 ? sensitivityLabelIdAttr.Options.First(o => o.StartsWith(sensitivityLabelId + "=")).Split('=')[1] : string.Empty;
		        changedProperties.Add(new { name = "sys_sensitivity_label_id",   value = sensitivityLabelId   });
		        changedProperties.Add(new { name = "sys_sensitivity_label_name", value = sensitivityLabelName });
		    }

            if (descriptionAttr.IsValueChanged)
		    {
		        var description = (string)obj["Description"] ?? string.Empty;
                changedProperties.Add(new { name = "description", value = description });
		    }

		    var ids = obj.ObjectId.Split(';');

			var columnName = ids[0];
			var schemaName = ids[1];
			var tableName = ids[2];

			var sql = $@"use [{obj.Parent.ObjectId}];
{upsertExtendedProperty}";

			if (Context.Database.Connection.State == ConnectionState.Closed)
				Context.Database.Connection.Open();


		    foreach (var extendedproperty in changedProperties)
			{
				using (IDbCommand cmd = Context.Database.Connection.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.AddParameterWithValue("@name", extendedproperty.name);
					cmd.AddParameterWithValue("@value", extendedproperty.value);

					cmd.AddParameterWithValue("@schemaname", schemaName);
					cmd.AddParameterWithValue("@tablename", tableName);
					cmd.AddParameterWithValue("@columnname", columnName);
					cmd.ExecuteNonQuery();
				}
			}

			Manager.Current.QueueClientOperation(new RefreshOperation("ClassifyData.Database", obj.Parent.ObjectId));
		}


		public IEnumerable<ColumnInfo> ForDatabase(CustomQueryArgs args)
		{
			var database = args.Parent.ObjectId;
			return Context.Database.SqlQuery<ColumnInfo>(string.Format(infoFormat, database)).ToArray();
		}

		public static void SetInfo(ClassifyDataEntityModelContainer context, PersistentObject databaseObj)
		{
			var tablesInfo = context.Database.SqlQuery<CountTotal>(string.Format(tablesFormat, databaseObj.ObjectId)).First();
			databaseObj["Tables"].SetOriginalValue(string.Format("{0} / {1}", tablesInfo.Count, tablesInfo.Total));
			var columnInfo = context.Database.SqlQuery<CountTotal>(string.Format(columnsFormat, databaseObj.ObjectId)).First();
			databaseObj["Columns"].SetOriginalValue(string.Format("{0} / {1}", columnInfo.Count, columnInfo.Total));
		}

		private sealed class CountTotal
		{
			public int Count { get; set; }

			public int Total { get; set; }
		}
	}
}
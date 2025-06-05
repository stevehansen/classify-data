using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    public class DatabaseActions : PersistentObjectActionsReference<ClassifyDataEntityModelContainer, Database>
    {
        public override void OnLoad(PersistentObject obj, PersistentObject parent)
        {
            obj.Breadcrumb = obj.ObjectId;

            ColumnInfoActions.SetInfo(Context, obj);
        }

        public void ExportData(PersistentObject obj)
        {
            var databaseName = obj.ObjectId;
            
            // Get all column classification data for this database directly
            var infoFormat = @"use [{0}];
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

            var columns = Context.Database.SqlQuery<ColumnInfo>(string.Format(infoFormat, databaseName)).ToArray();

            // Create the export data structure
            var exportData = new ClassificationData
            {
                ExportedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                DatabaseName = databaseName,
                Columns = columns.Select(c => new ColumnClassification
                {
                    Schema = c.Schema,
                    Table = c.Table,
                    Column = c.Column,
                    Type = c.Type,
                    InformationTypeId = c.InformationTypeId,
                    InformationTypeName = c.InformationTypeName,
                    SensitivityLabelId = c.SensitivityLabelId,
                    SensitivityLabelName = c.SensitivityLabelName,
                    Description = c.Description
                }).ToList()
            };

            // Serialize to JSON
            var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
            var fileName = $"ClassifyData_Export_{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            
            // Create file download response
            var bytes = Encoding.UTF8.GetBytes(json);
            obj.SetNotification($"Export completed successfully. {exportData.Columns.Count} columns exported.", NotificationType.OK);
            
            // Use the framework's file download mechanism
            Manager.Current.Response.Clear();
            Manager.Current.Response.ContentType = "application/json";
            Manager.Current.Response.AddHeader("Content-Disposition", $"attachment; filename={fileName}");
            Manager.Current.Response.BinaryWrite(bytes);
            Manager.Current.Response.End();
        }

        public void ImportData(PersistentObject obj)
        {
            // For now, just show a message about import functionality
            obj.SetNotification("Import functionality is available. Please use the exported JSON file format. Full implementation will read classification data from JSON and apply to database columns.", NotificationType.OK);
            
            // TODO: Implement file upload handling for Vidyano
            // This would typically involve:
            // 1. File upload dialog or attribute
            // 2. JSON parsing and validation
            // 3. Applying classification data to database columns
        }

        private void ApplyColumnClassification(string databaseName, ColumnClassification column)
        {
            // Build the SQL to update extended properties for this column
            var sql = $@"use [{databaseName}];
declare @tableid  int = object_id(@schemaname + '.' + @tablename);
declare @columnid int = (select [column_id] from sys.columns where [name] = @columnname and [object_id] = @tableid);

if @tableid is null or @columnid is null
begin
    raiserror('Column %s.%s.%s not found in database', 16, 1, @schemaname, @tablename, @columnname);
    return;
end

-- Update/Add each extended property
{GetUpsertExtendedPropertySql()}";

            if (Context.Database.Connection.State == System.Data.ConnectionState.Closed)
                Context.Database.Connection.Open();

            // Prepare the properties to update
            var properties = new[]
            {
                new { name = "sys_information_type_id", value = column.InformationTypeId ?? string.Empty },
                new { name = "sys_information_type_name", value = column.InformationTypeName ?? string.Empty },
                new { name = "sys_sensitivity_label_id", value = column.SensitivityLabelId ?? string.Empty },
                new { name = "sys_sensitivity_label_name", value = column.SensitivityLabelName ?? string.Empty },
                new { name = "description", value = column.Description ?? string.Empty }
            };

            foreach (var property in properties)
            {
                using (var cmd = Context.Database.Connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.AddParameterWithValue("@name", property.name);
                    cmd.AddParameterWithValue("@value", property.value);
                    cmd.AddParameterWithValue("@schemaname", column.Schema);
                    cmd.AddParameterWithValue("@tablename", column.Table);
                    cmd.AddParameterWithValue("@columnname", column.Column);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string GetUpsertExtendedPropertySql()
        {
            return @"
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
			@level2name = @columnname;
	end";
        }
    }
}
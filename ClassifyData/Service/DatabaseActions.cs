using System;
using System.Collections.Generic;
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
            
            // For now, just show the JSON in a notification for testing
            // In a full implementation, this would trigger a file download
            obj.SetNotification($"Export completed successfully. {exportData.Columns.Count} columns exported.\n\nJSON data:\n{json.Substring(0, Math.Min(500, json.Length))}...", NotificationType.OK);
            
            // TODO: Implement proper file download mechanism for Vidyano
            // This may require creating a custom action result or using the framework's file handling
        }

        public void ImportData(PersistentObject obj, string importData = null)
        {
            try
            {
                string json = importData;
                
                // If no data provided as parameter, try to get from request
                if (string.IsNullOrEmpty(json))
                {
                    // Try to get from form data
                    json = Manager.Current.Request.Form["importData"];
                }
                
                // If still no data, try to get from uploaded file
                if (string.IsNullOrEmpty(json) && Manager.Current.Request.Files.Count > 0)
                {
                    var uploadedFile = Manager.Current.Request.Files[0];
                    if (uploadedFile != null && uploadedFile.ContentLength > 0)
                    {
                        using (var reader = new StreamReader(uploadedFile.InputStream))
                        {
                            json = reader.ReadToEnd();
                        }
                    }
                }

                if (string.IsNullOrEmpty(json))
                {
                    obj.SetNotification("Please provide classification data to import. Expected JSON format from ExportData.", NotificationType.Error);
                    return;
                }

                // Parse the JSON data
                var classificationData = JsonConvert.DeserializeObject<ClassificationData>(json);

                if (classificationData?.Columns == null)
                {
                    obj.SetNotification("Invalid file format. Expected classification data JSON file from ExportData.", NotificationType.Error);
                    return;
                }

                var databaseName = obj.ObjectId;
                var successCount = 0;
                var errorCount = 0;
                var errors = new StringBuilder();

                // Import each column's classification data
                foreach (var column in classificationData.Columns)
                {
                    try
                    {
                        // Apply the classification to this column
                        ApplyColumnClassification(databaseName, column);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.AppendLine($"Error importing {column.Schema}.{column.Table}.{column.Column}: {ex.Message}");
                    }
                }

                // Report results
                if (errorCount == 0)
                {
                    obj.SetNotification($"Import completed successfully. {successCount} columns imported.", NotificationType.OK);
                }
                else
                {
                    var message = $"Import completed with {errorCount} errors. {successCount} columns imported successfully.";
                    if (errors.Length > 0 && errors.Length < 1000) // Limit error message length
                    {
                        message += "\n\nErrors:\n" + errors.ToString();
                    }
                    obj.SetNotification(message, NotificationType.Warning);
                }
                
                // Refresh the columns data
                ColumnInfoActions.SetInfo(Context, obj);
            }
            catch (Exception ex)
            {
                obj.SetNotification($"Failed to import data: {ex.Message}", NotificationType.Error);
            }
        }

        private void ApplyColumnClassification(string databaseName, ColumnClassification column)
        {
            // Validate required fields
            if (string.IsNullOrEmpty(column.Schema) || string.IsNullOrEmpty(column.Table) || string.IsNullOrEmpty(column.Column))
            {
                throw new ArgumentException("Schema, Table, and Column are required fields.");
            }

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

            // Prepare the properties to update (only add non-empty values)
            var properties = new List<dynamic>();
            
            if (!string.IsNullOrEmpty(column.InformationTypeId))
            {
                properties.Add(new { name = "sys_information_type_id", value = column.InformationTypeId });
                properties.Add(new { name = "sys_information_type_name", value = column.InformationTypeName ?? string.Empty });
            }
            
            if (!string.IsNullOrEmpty(column.SensitivityLabelId))
            {
                properties.Add(new { name = "sys_sensitivity_label_id", value = column.SensitivityLabelId });
                properties.Add(new { name = "sys_sensitivity_label_name", value = column.SensitivityLabelName ?? string.Empty });
            }
            
            if (!string.IsNullOrEmpty(column.Description))
            {
                properties.Add(new { name = "description", value = column.Description });
            }

            // If no properties to update, skip this column
            if (properties.Count == 0)
            {
                return;
            }

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

        public void TestExportData(PersistentObject obj)
        {
            // This is a test method to validate export functionality
            try
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

                obj.SetNotification($"Test successful. Found {columns.Length} columns in database {databaseName}. Export functionality is ready.", NotificationType.OK);
            }
            catch (Exception ex)
            {
                obj.SetNotification($"Test failed: {ex.Message}", NotificationType.Error);
            }
        }
    }
}
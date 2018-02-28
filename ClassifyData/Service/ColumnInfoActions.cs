using System.Collections.Generic;
using System.Linq;
using Vidyano.Service.ClientOperations;
using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    public class ColumnInfoActions : PersistentObjectActionsReference<ClassifyDataEntityModelContainer, ColumnInfo>
    {
        private const string infoFormat = "use [{0}]; select schema_name(t.schema_id) [Schema], object_name(t.object_id) [Table], c.name [Column]\r\n\t, t_id.value [InformationTypeId], t_name.value [InformationTypeName], l_id.value [SensitivityLabelId], l_name.value [SensitivityLabelName]\r\n\tfrom sys.tables t\r\n\tinner join sys.columns c on c.object_id = t.object_id\r\n\tleft join sys.extended_properties t_id on c.object_id = t_id.major_id and c.column_id = t_id.minor_id and t_id.name = \'sys_information_type_id\'\r\n\tleft join sys.extended_properties t_name on t_id.major_id = t_name.major_id and t_id.minor_id = t_name.minor_id and t_name.name = \'sys_information_type_name\'\r\n\tleft join sys.extended_properties l_id on t_id.major_id = l_id.major_id and t_id.minor_id = l_id.minor_id and l_id.name = \'sys_sensitivity_label_id\'\r\n\tleft join sys.extended_properties l_name on t_id.major_id = l_name.major_id and t_id.minor_id = l_name.minor_id and l_name.name = \'sys_sensitivity_label_name\'";
        private const string selectFormat = infoFormat + " where schema_name(t.schema_id) = @p1 collate database_default and object_name(t.object_id) = @p2 collate database_default and c.name = @p0 collate database_default";
        private const string addExtendedPropertyFormat = "exec sp_addextendedproperty @name = N'{0}', @value = {1}, @level0type = N'Schema', @level0name = @p1, @level1type = N'Table',  @level1name = @p2, @level2type = N'Column', @level2name = @p0;";
        private static readonly string setExtendedPropertiesFormat = "use [{0}];" + string.Format(addExtendedPropertyFormat, "sys_information_type_id", "@p3") + string.Format(addExtendedPropertyFormat, "sys_information_type_name", "@p4") + string.Format(addExtendedPropertyFormat, "sys_sensitivity_label_id", "@p5") + string.Format(addExtendedPropertyFormat, "sys_sensitivity_label_name", "@p6");
        private static readonly string updateExtendedPropertiesFormat = setExtendedPropertiesFormat.Replace("sp_addextendedproperty", "sp_updateextendedproperty");
        private const string tablesFormat = "use [{0}]; select sum(case when a.count = 0 then 0 else 1 end) Count, count(0) Total from (\r\nselect sum(case when t_id.name is null then 0 else 1 end) count\r\n\tfrom sys.tables t\r\n\tinner join sys.columns c on c.object_id = t.object_id\r\n\tleft join sys.extended_properties t_id on c.object_id = t_id.major_id and c.column_id = t_id.minor_id and t_id.name = \'sys_information_type_id\'\r\n\tgroup by t.object_id\r\n) a";
        private const string columnsFormat = "use [{0}]; select sum(case when t_id.name is null then 0 else 1 end) [Count], count(0) [Total]\r\n\tfrom sys.tables t\r\n\tinner join sys.columns c on c.object_id = t.object_id\r\n\tleft join sys.extended_properties t_id on c.object_id = t_id.major_id and c.column_id = t_id.minor_id and t_id.name = \'sys_information_type_id\'";

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
            var informationTypeIdAttr = obj["InformationTypeId"];
            var sensitivityLabelIdAttr = obj["SensitivityLabelId"];

            if (!CheckRules(obj))
                return;

            var entity = LoadEntity(obj);

            var sql = string.Format(entity.InformationTypeId == null ? setExtendedPropertiesFormat : updateExtendedPropertiesFormat, obj.Parent.ObjectId);
            var ids = obj.ObjectId.Split(';');
            var informationTypeId = (string)informationTypeIdAttr ?? string.Empty;
            var informationTypeName = informationTypeId.Length > 0 ? informationTypeIdAttr.Options.First(o => o.StartsWith(informationTypeId + "=")).Split('=')[1] : string.Empty;
            var sensitivityLabelId = (string)sensitivityLabelIdAttr ?? string.Empty;
            var sensitivityLabelName = sensitivityLabelId.Length > 0 ? sensitivityLabelIdAttr.Options.First(o => o.StartsWith(sensitivityLabelId + "=")).Split('=')[1] : string.Empty;
            Context.Database.ExecuteSqlCommand(sql, ids[0], ids[1], ids[2], informationTypeId, informationTypeName, sensitivityLabelId, sensitivityLabelName);

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
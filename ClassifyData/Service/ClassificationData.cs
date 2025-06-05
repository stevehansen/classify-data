using System;
using System.Collections.Generic;

namespace ClassifyData.Service
{
    /// <summary>
    /// Data transfer object for exporting/importing classification data
    /// </summary>
    public class ClassificationData
    {
        public string ExportedAt { get; set; }
        public string DatabaseName { get; set; }
        public List<ColumnClassification> Columns { get; set; } = new List<ColumnClassification>();
    }

    /// <summary>
    /// Represents classification data for a single column
    /// </summary>
    public class ColumnClassification
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Column { get; set; }
        public string Type { get; set; }
        public string InformationTypeId { get; set; }
        public string InformationTypeName { get; set; }
        public string SensitivityLabelId { get; set; }
        public string SensitivityLabelName { get; set; }
        public string Description { get; set; }
    }
}
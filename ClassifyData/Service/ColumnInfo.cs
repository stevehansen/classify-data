using System.ComponentModel.DataAnnotations;

namespace ClassifyData.Service
{
    public class ColumnInfo
    {
        [Key]
        public string Schema { get; set; }

        [Key]
        public string Table { get; set; }

        [Key]
        public string Column { get; set; }

		public string Type { get; set; }

		public string InformationTypeId { get; set; }

        public string InformationTypeName { get; set; }

        public string SensitivityLabelId { get; set; }

        public string SensitivityLabelName { get; set; }
    }
}
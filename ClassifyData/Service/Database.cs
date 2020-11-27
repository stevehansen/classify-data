using System.ComponentModel.DataAnnotations;

namespace ClassifyData.Service
{
    public class Database
    {
        [Key]
        public string Name { get; set; }
    }
}
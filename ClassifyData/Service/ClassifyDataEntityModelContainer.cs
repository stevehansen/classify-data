using System.Data.Entity;

namespace ClassifyData.Service
{
    public partial class ClassifyDataEntityModelContainer : DbContext
    {
        static ClassifyDataEntityModelContainer()
        {
            System.Data.Entity.Database.SetInitializer<ClassifyDataEntityModelContainer>(null);
        }
    }
}
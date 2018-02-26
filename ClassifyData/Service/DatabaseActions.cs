using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    public class DatabaseActions : PersistentObjectActionsReference<ClassifyDataEntityModelContainer, Database>
    {
        public override void OnLoad(PersistentObject obj, PersistentObject parent)
        {
            obj.Breadcrumb = obj.ObjectId;
        }
    }
}
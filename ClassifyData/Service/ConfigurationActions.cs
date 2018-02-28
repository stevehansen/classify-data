using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    public class ConfigurationActions : PersistentObjectActionsReference<ClassifyDataEntityModelContainer, object>
    {
        public override void OnLoad(PersistentObject obj, PersistentObject parent)
        {
            obj["ConnectionString"].SetOriginalValue(ClassifyDataEntityModelContainer.ConnectionString);

            var exception = ClassifyDataEntityModelContainer.Exception;
            if (exception != null)
                obj.AddNotification(exception, NotificationType.Warning);
        }

        public override void OnSave(PersistentObject obj)
        {
            if (!CheckRules(obj))
                return;

            ClassifyDataEntityModelContainer.ConnectionString = (string)obj["ConnectionString"];
        }
    }
}
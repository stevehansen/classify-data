using System;
using System.Collections.Generic;
using System.Linq;
using Vidyano.Service;
using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    public class ClassifyDataAdvanced : Advanced
    {
        public override void OnBuildProgramUnits(List<ProgramUnit> programUnits)
        {
            base.OnBuildProgramUnits(programUnits);

            var server = programUnits.FirstOrDefault(pu => pu.Name == "Server");
            if (server != null)
            {
                var databasePo = Manager.Current.GetPersistentObject("Database");

                using (var context = new ClassifyDataEntityModelContainer())
                {
                    var databases = context.Database.SqlQuery<string>("select name from sys.databases where database_id > 4 and state = 0 order by name");
                    foreach (var database in databases)
                        server.AddPersistentObjectItem(database, database, databasePo, database);
                }
            }
        }
    }
}
using System.Data;

namespace ClassifyData.Service
{
	internal static class DataBaseExtensions {
		public static void AddParameterWithValue(this IDbCommand cmd, string name, object value)
		{
			var p = cmd.CreateParameter();
			p.ParameterName = name;
			p.Value = value;
			cmd.Parameters.Add(p);
		}
	}
}
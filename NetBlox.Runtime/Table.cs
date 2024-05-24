using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Runtime
{
	public class Table
	{
		public Table? MetaTable { get; set; }
		private Dictionary<RuntimeObject, RuntimeObject> data = [];

		public RuntimeObject this[string k]
		{
			get
			{
				if (MetaTable == null)
				{
					for (int i = 0; i < data.Count; i++)
					{
						var kvp = data.ElementAt(i);
						if (kvp.Key.Type == RuntimeType.String && kvp.Key.Value != null)
							if ((string)kvp.Key.Value == k)
								return kvp.Value;
					}
				}
				else
				{
					var ind = MetaTable["__index"];
					if (ind.IsNil)
					{
						for (int i = 0; i < data.Count; i++)
						{
							var kvp = data.ElementAt(i);
							if (kvp.Key.Type == RuntimeType.String && kvp.Key.Value != null)
								if ((string)kvp.Key.Value == k)
									return kvp.Value;
						}
					}
					else
					{
						var f = ind.EnsureFunction();
						return f.Invoke();
					}
				}
				return RuntimeObject.GetNil();
			}
			set
			{

			}
		}
		public RuntimeObject this[RuntimeObject ro]
		{
			get
			{
				if (MetaTable == null)
				{
					if (data.TryGetValue(ro, out var o)) return o;
					else return RuntimeObject.GetNil();
				}
				else
				{
					var ind = MetaTable["__index"];
					if (ind.IsNil)
					{
						if (data.TryGetValue(ro, out var o)) return o;
						else return RuntimeObject.GetNil();
					}
					else
					{
						var f = ind.EnsureFunction();
						return f.Invoke();
					}
				}
			}
			set
			{

			}
		}
	}
}

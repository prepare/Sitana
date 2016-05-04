﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Sitana.Framework.Diagnostics;
using Sitana.Framework.Ui.Controllers;
using Sitana.Framework.Content;
using Sitana.Framework.Xml;

namespace Sitana.Framework.Ui.DefinitionFiles
{
    public class DefinitionFile : ContentLoader.AdditionalType
    {
        public readonly Type Class;
        public readonly string Anchor;

        Dictionary<string, object> _values = new Dictionary<string, object>();

        bool _locked = false;

        /// <summary>
        /// Registers additional type in ContentLoader
        /// </summary>
        public static void Register()
        {
            RegisterType(typeof(DefinitionFile), Load, true);
        }

        /// <summary>
        /// Loads content object
        /// </summary>
        /// <param name="name">name of resource</param>
        /// <param name="contentLoader">content loader to load additional resources and files</param>
        /// <returns></returns>
        public static DefinitionFile Load(String path)
        {
            XNode node = XFileEx.FromPath(path);
            return DefinitionFile.LoadFile(node);
        }

        public DefinitionFile(Type type, string anchor)
        {
			if (type == null)
			{
				throw new Exception("Type is null!");
			}

            Anchor = anchor;
            Class = type;
        }

        public List<string> Keys
        {
            get
            {
                return new List<string>(_values.Keys);
            }
        }

        public void Lock()
        {
            _locked = true;
        }

        public object this[string id]
        {
            get
            {
                object value;
                _values.TryGetValue(id, out value);

                return value;
            }

            set
            {
                if ( _locked )
                {
                    throw new Exception("Modifying values is not allowed.");
                }

                if (value == null)
                {
                    _values.Remove(id);
                }
                else
                {
                    _values[id] = value;
                }
            }
        }

        public static Type GetType(XNode node)
        {
            string ns = node.Namespace;
            string cl = node.Tag;

			string name = "";

            if ( ns.StartsWith("namespace:"))
            {
                string[] vals = ns.Substring(10).Split(',');
                name = String.Format("{0}.{1},{2}", vals[0], cl, vals[1]);

                return Type.GetType(name);
            }

			throw new Exception(string.Format("[Sitana] Cannot find type: {0}", name));
        }

        public static DefinitionFile LoadFile(XNode node)
        {
			try
			{
	            Type type = GetType(node);

				if(type == null)
				{
                    throw new Exception($"Unknown type: {node.Tag}");
				}

	            MethodInfo method = type.GetTypeInfo().GetDeclaredMethod("Parse");

	            DefinitionFile file = null;

	            if (method != null)
	            {
	                if (method.ReturnType == typeof(DefinitionFile))
	                {
	                    file = (DefinitionFile)method.Invoke(null, new object[] { node, null });
	                }
	                else
	                {
	                    file = new DefinitionFile(type, node.Owner.Name);
	                    method.Invoke(null, new object[] { node, file });
                        file["Style"] = node.Attribute("Style");
	                }
	            }

	            return file;
			}
            catch(TargetInvocationException exc)
            {
                throw DefinitionResolver.PrepareException(exc);
            }
			catch(Exception ex)
			{
				ConsoleEx.WriteLine(ConsoleEx.Error, $"{ex}.");
				throw ex;
			}
        }

        public static DefinitionFile CreateFile(Type type, XNode attributesNode)
        {
            DefinitionFile file = null;
            MethodInfo method = type.GetTypeInfo().GetDeclaredMethod("Parse");

            if (method != null)
            {
                file = new DefinitionFile(type, "");
                method.Invoke(null, new object[] { attributesNode, file });

                file["Style"] = attributesNode.Attribute("Style");
            }

            return file;
        }

        public IDefinitionClass CreateInstance(UiController controller, object context)
        {
            IDefinitionClass obj = (IDefinitionClass)Activator.CreateInstance(Class);
            if (obj.Init(controller, context, this))
            {
                return obj;
            }
            return null;
        }

        public bool HasKey(string name)
        {
            return _values.ContainsKey(name) && _values[name] != null;
        }
    }
}

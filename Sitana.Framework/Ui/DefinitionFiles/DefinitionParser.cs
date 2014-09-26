﻿using Sitana.Framework.Content;
using Sitana.Framework.Diagnostics;
using Sitana.Framework.Ui.DefinitionFiles;
using Microsoft.Xna.Framework;
using System;
using System.Globalization;

namespace Sitana.Framework.Essentials.Ui.DefinitionFiles
{
    public class DefinitionParser
    {
        public static bool EnableCheckMode = false;

        readonly XNode _node;

        public DefinitionParser(XNode node)
        {
            _node = node;
        }

        Exception Error(string id, string format, params object[] args)
        {
            string message = _node.NodeError(String.Format("{1}: {0}", String.Format(format, args), id));

            if ( EnableCheckMode )
            {
                ConsoleEx.WriteLine(message);
                return null;
            }
            else
            {
                return new Exception(message);
            }
        }

        public string Value(string attribute)
        {
            return _node.Attribute(attribute);
        }

        

        object ParseMethodOrField(string name)
        {
            if (name.StartsWith("{"))
            {
                name = name.Trim('{', '}');

                int index = name.IndexOf('(');

                if (index > 0)
                {
                    string[] elements = name.Substring(index + 1).Trim(')').Split(',');

                    object[] parameters;
                    if (elements[0].IsNullOrWhiteSpace())
                    {
                        parameters = new object[0];
                    }
                    else
                    {
                        parameters = new object[elements.Length];
                    }

                    for (int idx = 0; idx < parameters.Length; ++idx)
                    {
                        parameters[idx] = ParseParameter(name, elements[idx]);
                    }

                    name = name.Substring(0, index);

                    return new MethodName() { Name = name, Parameters = parameters };
                }


                index = name.IndexOf('[');
                {
                    string[] elements = name.Substring(index + 1).Trim(']').Split(',');
                    object[] parameters = null;

                    if ( index > 0 )
                    {
                        parameters = new object[elements.Length];
                    }

                    if (parameters != null)
                    {
                        for (int idx = 0; idx < parameters.Length; ++idx)
                        {
                            parameters[idx] = ParseParameter(name, elements[idx]);
                        }
                    }

                    if (index > 0)
                    {
                        name = name.Substring(0, index);
                    }

                    return new FieldName() { Name = name, Parameters = parameters };
                }


            }

            return null;
        }

        object ParseParameter(string methodDef, string val)
        {
            if (val.StartsWith("$"))
            {
                val = val.Substring(1).Trim();
                return new ReflectionParameter(val);
            }

            if ( val.StartsWith("\'"))
            {
                val = val.Trim('\'');
                return val;
            }

            val = val.Trim();

            int intVal;
            if (int.TryParse(val, out intVal))
            {
                return intVal;
            }

            Exception ex = Error("Unable to solve parameter type in method: {0}.", methodDef);
            if (ex != null) throw ex;

            return val;
        }

        public object ParseString(string name)
        {
            name = Value(name);
            object method = ParseMethodOrField(name);

            if (method != null)
            {
                return method;
            }

            return name;
        }

        public object ParseColor(string id)
        {
            string name = Value(id);
            object method = ParseMethodOrField(name);

            if (method != null)
            {
                return method;
            }

            if (name.IsNullOrEmpty())
            {
                return null;
            }

            int r, g, b, a;

            if (name.StartsWith("#"))
            {
                if (name.Length == 7 || name.Length == 9)
                {
                    int color;
                    if (int.TryParse(name.Replace("#", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color))
                    {
                        a = (color >> 24) & 0xff;
                        r = (color >> 16) & 0xff;
                        g = (color >> 8) & 0xff;
                        b = color & 0xff;

                        if (name.Length == 7)
                        {
                            a = 255;
                        }

                        return Color.FromNonPremultiplied(r, g, b, a);
                    }
                }
            }

            string[] vals = name.Replace(" ", "").Split(',');

            if (vals.Length == 3)
            {
                if (Int32.TryParse(vals[0], out r) && Int32.TryParse(vals[1], out g) && Int32.TryParse(vals[2], out b))
                {
                    return new Color(r, g, b);
                }
            }

            if (vals.Length == 4)
            {
                if (Int32.TryParse(vals[0], out r) && Int32.TryParse(vals[1], out g) && Int32.TryParse(vals[2], out b) && Int32.TryParse(vals[3], out a))
                {
                    return Color.FromNonPremultiplied(r, g, b, a);
                }
            }

            Exception ex = Error(id, "Invalid format. Color formats are: '#aarrggbb' '#rrggbb' 'r,g,b' 'r,g,b,a'.");
            if ( ex != null) throw ex;

            return null;
        }

        public object ParseBoolean(string id, bool defaultValue = false)
        {
            string name = Value(id);
            object method = ParseMethodOrField(name);

            if (method != null)
            {
                return method;
            }

            if (name.IsNullOrEmpty())
            {
                return defaultValue;
            }

            bool value;

            if(bool.TryParse(name, out value))
            {
                return value;
            }

            Exception ex = Error(id, "Invalid format. Expected true or false or Method/Property name.");
            if (ex != null) throw ex;

            return defaultValue;
        }

        public object ParseNinePatchImage(string id)
        {
            string name = Value(id);
            object method = ParseMethodOrField(name);

            if (method != null)
            {
                return method;
            }

            try
            {
                return ContentLoader.Current.Load<NinePatchImage>(name);
            }
            catch(Exception ex)
            {
                if (!EnableCheckMode) throw ex;
                return null;
            }
        }

        public T ParseEnum<T>(string id, T defaultValue) where T: struct
        {
            string name = Value(id);

            if (name.IsNullOrEmpty())
            {
                return defaultValue;
            }

            T value;
            if (Enum.TryParse<T>(name, true, out value))
            {
                return value;
            }

            Exception ex = Error(id, "Error while parsing enumeration: {0}.", typeof(T).FullName);
            if (ex != null) throw ex;

            return defaultValue;
        }

        public object ParseMargin(string id)
        {
            string name = Value(id);
            object method = ParseMethodOrField(name);

            if (method != null)
            {
                return method;
            }

            if (name.IsNullOrEmpty())
            {
                return new Margin(0);
            }

            string[] elements = name.Replace(" ", "").Split(',');

            if ( elements.Length == 1 )
            {
                int value;
                if ( int.TryParse(elements[0], out value))
                {
                    return new Margin(value);
                }
            }
            else if ( elements.Length == 4 )
            {
                int left, top, right, bottom;
                if ( int.TryParse(elements[0], out left) &&
                     int.TryParse(elements[1], out top) &&
                     int.TryParse(elements[2], out right) &&
                     int.TryParse(elements[3], out bottom))
                {
                    return new Margin(left, top, right, bottom);
                }
            }

            Exception ex = Error(id, "Margin format is 'left,top,right,bottom' or 'all'.");
            if (ex != null) throw ex;

            return new Margin(0);
        }

        public object ParseInt(string name)
        {
            return ParseInt(name, 0);
        }

        public object ParseInt(string id, int defaultValue)
        {
            string name = Value(id);
            object method = ParseMethodOrField(name);

            if (method != null)
            {
                return method;
            }

            if (name.IsNullOrEmpty())
            {
                return defaultValue;
            }

            int value;

            if (int.TryParse(name, out value))
            {
                return value;
            }

            Exception ex = Error(id, "Invalid format. Expected Integer.");
            if (ex != null) throw ex;

            return defaultValue;
        }

        public object ParseLength(string id)
        {
            string name = Value(id);
            object method = ParseMethodOrField(name);

            if (method != null)
            {
                return method;
            }

            if ( name.IsNullOrEmpty())
            {
                return new Length(true);
            }

            name = name.Replace(" ", "");

            bool percent = name.EndsWith("%");

            name = name.TrimEnd('%');

            int length;
            
            if ( int.TryParse(name, out length))
            {
                return new Length(length, percent);
            }

            if ( name.ToLowerInvariant() == "auto" )
            {
                return new Length(true);
            }

            Exception ex = Error(id, "Length format is integer with optional '%' sign at the end.");
            if (ex != null) throw ex;

            return new Length(true);
        }
    }
}
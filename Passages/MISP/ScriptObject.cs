﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public interface ScriptAsString
    {
        String AsString(int depth);
    }

    public class ScriptObject : ScriptAsString
    {
        public virtual Object GetProperty(String name) { return null; }
        public virtual Object GetLocalProperty(String name) { return null; }
        public virtual void SetProperty(String name, Object value) { }
        public virtual void DeleteProperty(String name) { }
        public virtual ScriptList ListProperties() { return new ScriptList(); }
        public virtual void ClearProperties() { }
        
        public Object this[String key]
        {
            get { return GetProperty(key); }
            set { SetProperty(key, value); }
        }

        public String gsp(String key)
        {
            var prop = GetProperty(key);
            if (!(prop is String)) return "";
            return prop as String;
        }

        public int gip(String key)
        {
            var prop = GetProperty(key);
            var i = prop as int?;
            if (i.HasValue) return i.Value;
            return 0;
        }

        public ScriptList _children
        {
            get
            {
                var children = this["@children"] as ScriptList;
                if (children == null)
                {
                    children = new ScriptList();
                    this["@children"] = children;
                }
                return children;
            }
        }

        public Object _child(int n)
        {
            return _children[n];
        }

        public String AsString(int depth)
        {
            if (depth < 0) return this.GetType().Name;
            else return "SO{" + String.Join(", ", (this as ScriptObject).ListProperties().Select((o) =>
            {
                return o.ToString() + ": " + ScriptObject.AsString(this.GetProperty(o.ToString()), depth - 1);
            })) + " }";
        }

        public static String AsString(Object obj, int depth = 0)
        {
            if (obj == null) return "null";
            if (obj is ScriptAsString) return (obj as ScriptAsString).AsString(depth);
            return obj.ToString();
        }
    }

    public class ExtendableReflectionScriptObject : ScriptObject
    {
        private Dictionary<String, Object> properties = new Dictionary<string, object>();
        override public object GetProperty(string name)
        {
            var field = this.GetType().GetField(name);
            if (field != null) return field.GetValue(this);
            if (properties.ContainsKey(name)) return properties[name];
            return null;
        }

        public override object GetLocalProperty(string name)
        {
            return GetProperty(name);
        }

        override public void DeleteProperty(String name)
        {
            if (properties.ContainsKey(name)) properties.Remove(name);
            else if (this.GetType().GetField(name) != null)
                throw new ScriptError("This property cannot be removed from objects of type " + this.GetType().Name + ".", null);
        }

        override public void SetProperty(string name, object value)
        {
            var field = this.GetType().GetField(name);
            if (field != null)
            {
                if (field.FieldType == typeof(bool))
                    field.SetValue(this, (value != null));
                else
                    field.SetValue(this, value);
            }
            else
                properties.Upsert(name, value);
        }

        override public ScriptList ListProperties()
        {
            var r = new ScriptList();
            r.AddRange(this.GetType().GetFields().Select((info) => info.Name));
            r.AddRange(properties.Select((p) => p.Key));
            return r;
        }
    }

    public class GenericScriptObject : ScriptObject
    {
        public Dictionary<String, Object> properties = new Dictionary<string, object>();

        public GenericScriptObject() { }

        public GenericScriptObject(ScriptObject cloneFrom)
        {
            foreach (var str in cloneFrom.ListProperties())
                SetProperty(str as String, cloneFrom.GetProperty(str as String));
        }

        public GenericScriptObject(params Object[] args)
        {
            if (args.Length % 2 != 0) throw new InvalidProgramException("Generic Script Object must be initialized with pairs");
            for (int i = 0; i < args.Length; i += 2)
                SetProperty(ScriptObject.AsString(args[i]), args[i + 1]);
        }

        

        private object GetInheritedProperty(string name, List<ScriptObject> inheritanceStack)
        {
            if (inheritanceStack.Contains(this)) return null;
            if (properties.ContainsKey(name)) return properties[name];
            inheritanceStack.Add(this);
            if (properties.ContainsKey("@base"))
            {
                var @base = properties["@base"] as GenericScriptObject;
                if (@base == null) return null;
                else return @base.GetInheritedProperty(name, inheritanceStack);
            }
            else
                return null;
        }

        override public object GetProperty(string name)
        {
            //name = name.ToLowerInvariant();
            if (properties.ContainsKey(name)) return properties[name];
            else return GetInheritedProperty(name, new List<ScriptObject>());
        }

        public override object GetLocalProperty(string name)
        {
            //name = name.ToLowerInvariant();
            if (properties.ContainsKey(name)) return properties[name];
            else return null;
        }

        override public void SetProperty(string Name, Object Value)
        {
            properties.Upsert(Name, Value);
        }

        override public void DeleteProperty(String Name)
        {
            //Name = Name.ToLowerInvariant();
            if (properties.ContainsKey(Name)) properties.Remove(Name);
        }

        override public ScriptList ListProperties()
        {
            return new ScriptList(properties.Select((p) => { return p.Key; }));
        }

        public override void ClearProperties()
        {
            properties.Clear();
        }

        public ScriptObject AddFunction(String name, String comment, Func<Context, ScriptList, Object> func,
            params ScriptObject[] arguments)
        {
            var r = Function.MakeSystemFunction(name, new ScriptList(arguments), comment, func);
            SetProperty(name, r);
            return r;
        }
    }


}

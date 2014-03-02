// This file is GPL
namespace resgenEx
{
    using System;
    using System.Collections.Generic;
    using System.Resources;
    using System.Reflection;
    using System.Runtime.Serialization;

    class ResourceItem : ISerializable
    {
        string _name;
        string _value;

        string _metadata_comment;
        string _metadata_originalSource;
        string _metadata_originalValue;
        
        /*
        string _po_translator_comments;
        string _po_extracted_comments;
        string _po_references;
        string _po_flags;
        string _po_previous_untranslated_string;
         */ 

        readonly AssemblyName[] _assemblyname = new AssemblyName[0];


        public string Name { get { return _name; } }
        public string Value { get { return _value; } }

        public string Metadata_Comment        { get { return _metadata_comment; } }
        public string Metadata_OriginalSource { get { return _metadata_originalSource; } }
        public string Metadata_OriginalValue  { get { return _metadata_originalValue; } }


        #region ISerializable Members

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // this is needed to support the resx resource writer, so just cast to 
            // ResXDataNode and use its serialize
            ((ISerializable)ToResXDataNode()).GetObjectData(info, context);
        }

        #endregion


        public static implicit operator ResXDataNode(ResourceItem item)
        {
            return item.ToResXDataNode();
        }

        /// <summary>
        /// May return null.
        /// </summary>
        public virtual ResXDataNode ToResXDataNode()
        {
            ResXDataNode result = null;
            
            if (!String.IsNullOrEmpty(_name)) {
                result = new ResXDataNode(_name, _value);
                if (!String.IsNullOrEmpty(Metadata_Comment)) result.Comment = Metadata_Comment;
            }
            return result;
        }


        public ResourceItem(string name, string value, string comment, string originalSourceFile, string originalValue)
            : this(name, value, comment, originalSourceFile)
        {
            _metadata_originalValue = originalValue;
        }

        public ResourceItem(string name, string value, string comment, string originalSourceFile) : this(name, value, comment)
        {
            _metadata_originalSource = originalSourceFile;
        }

        public ResourceItem(string name, string value, string comment)
        {
            _name = name;
            _value = value;
            _metadata_comment = comment;
        }

        /// <summary>
        /// Adapts or casts the data into a ResourceItem. Does not return null, but may throw exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">if the type is not known</exception>
        public static ResourceItem Get(string name, object value)
        {
            if (value is ResXDataNode) {
                return new ResourceItem((ResXDataNode)value);
            } else if (value is ResourceItem) {
                return (ResourceItem)value;
            } else if (value is string) {
                return new ResourceItem(name, (string)value, String.Empty);
            } else if (value is byte[]) {
                throw new InvalidOperationException("Binary data not handled for resource coversion");
            } else {
                throw new InvalidOperationException("Object not handled for resource coversion: " + (value == null ? "null" : value.ToString()));
            }
        }

        public ResourceItem(ResXDataNode data)
        {
            if (data == null) throw new ArgumentNullException();

            if (data != null) {
                _name = data.Name;
                _value = data.GetValue(_assemblyname) as String;
                _metadata_comment = data.Comment;
            }
        }
    }
}

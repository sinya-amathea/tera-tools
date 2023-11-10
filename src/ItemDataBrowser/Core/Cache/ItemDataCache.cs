using System.Xml;
using ItemDataBrowser.Core.Xml;
using ItemDataBrowser.Objects;
using Sinya.Tera.Shared.Schema;
using static System.String;

namespace ItemDataBrowser.Core.Cache
{
    internal class ItemDataCache : BaseXmlListProvider<ItemData>
    {
        internal readonly List<PropertyMapping> PropertyCache = new();

        public ItemDataCache() 
            : base("/i:ItemData/i:Item", "ItemData", "ItemData-?????.xml")
        {
        }

        protected override void SetNamespaces(XmlNamespaceManager namespaces)
        {
            namespaces.AddNamespace("i", "https://vezel.dev/novadrop/dc/ItemData");
        }

        protected override bool TryParse(XmlNode node, out ItemData? data)
        {
            try
            {
                var item = new ItemData();

                foreach (var mapping in PropertyCache)
                {
                    var attr = node.Attributes?[mapping.AttributeName];

                    if (attr == null)
                        continue;

                    object? value = null;

                    if (mapping.Property.PropertyType == typeof(bool))
                    {
                        value = attr.Value.ToLower() == "true";
                    }
                    else if (mapping.Property.PropertyType == typeof(bool?) && !IsNullOrWhiteSpace(attr.Value))
                    {
                        value = (bool?)(attr.Value.ToLower() == "true");
                    }
                    else if (mapping.Property.PropertyType == typeof(int?) && !IsNullOrWhiteSpace(attr.Value))
                    {
                        if (Int32.TryParse(attr.Value, out int iValue))
                            value = (int?)iValue;
                    }
                    else if (mapping.Property.PropertyType == typeof(float?) && !IsNullOrWhiteSpace(attr.Value))
                    {
                        if (Single.TryParse(attr.Value, out float iValue))
                            value = (float?)iValue;
                    }
                    else
                    {
                        value = Convert.ChangeType(attr.Value, mapping.Property.PropertyType);
                    }

                    mapping.Property.SetValue(item, value);
                }

                data = item;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                data = null;
                return false;
            }
        }

        protected override void Initialize()
        {
            Console.WriteLine("[Cache] Create property <=> attribute cache");

            var properties = typeof(ItemData).GetProperties();

            foreach (var propertyInfo in properties)
            {
                var attrName = char.ToLower(propertyInfo.Name[0]) + propertyInfo.Name.Substring(1);

                PropertyCache.Add(new PropertyMapping
                {
                    AttributeName = attrName,
                    Property = propertyInfo
                });
            }
        }
    }
}

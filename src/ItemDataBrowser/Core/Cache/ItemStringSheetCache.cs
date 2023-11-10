using System.Xml;
using ItemDataBrowser.Core.Xml;
using Sinya.Tera.Shared.Schema;
using static System.String;

namespace ItemDataBrowser.Core.Cache
{
    internal class ItemStringSheetCache : BaseXmlListProvider<StringSheetItem>
    {
        public ItemStringSheetCache() 
            : base("/i:StrSheet_Item/i:String", "StrSheet_Item", "StrSheet_Item-?????.xml")
        {
        }

        protected override void SetNamespaces(XmlNamespaceManager namespaces)
        {
            namespaces.AddNamespace("i", "https://vezel.dev/novadrop/dc/StrSheet_Item");
        }

        protected override bool TryParse(XmlNode node, out StringSheetItem? data)
        {
            try
            {
                var idAttrVal = node.Attributes?["id"]?.Value;
                var nameAttrVal = node.Attributes?["string"]?.Value ?? Empty;
                var toolTipAttrVal = node.Attributes?["toolTip"]?.Value;

                if (IsNullOrWhiteSpace(idAttrVal) || !Int32.TryParse(idAttrVal, out int idValue))
                {
                    data = null;
                    return false;
                }

                data = new StringSheetItem
                {
                    Id = idValue,
                    Name = nameAttrVal,
                    ToolTip = toolTipAttrVal
                };
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                data = null;
                return false;
            }
        }

        protected override void Initialize() { }
    }
}

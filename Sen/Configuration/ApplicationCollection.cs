using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.Configuration
{
    [ConfigurationCollection(typeof(Application),
        AddItemName = "App",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ApplicationCollection
        : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Application();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return new Application(element);
        }

        public Application this[int index]
        {
            get
            {
                return (Application)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }
    }
}

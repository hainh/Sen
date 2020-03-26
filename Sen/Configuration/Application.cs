using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.Configuration
{
    public class Application
        : ConfigurationElement
    {
        private ConfigurationElement element;

        public Application(ConfigurationElement element)
        {
            this.element = element;
        }

        public Application()
        {
        }

        [ConfigurationProperty("Name", DefaultValue = "", IsRequired = false)]
        public string Name
        {
            get
            {
                return (string)this["Name"];
            }
            set
            {
                this["Name"] = value;
            }
        }

        [ConfigurationProperty("BaseDir", IsRequired = true)]
        public string BaseDir
        {
            get
            {
                return (string)this["BaseDir"];
            }
            set
            {
                this["BaseDir"] = value;
            }
        }

        [ConfigurationProperty("Type", IsRequired = true)]
        public string Type
        {
            get
            {
                return (string)this["Type"];
            }
            set
            {
                this["Type"] = value;
            }
        }

        [ConfigurationProperty("Assembly", IsRequired = true)]
        public string Assembly
        {
            get
            {
                return (string)this["Assembly"];
            }
            set
            {
                this["Assembly"] = value;
            }
        }

        [ConfigurationProperty("Settings", IsDefaultCollection = true)]
        public Settings Settings
        {
            get
            {
                Settings settings = (Settings)base["Settings"];
                return settings;
            }
        }
    }
}

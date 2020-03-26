using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senla.Server.Configuration
{
    public class SenlaSection
        : ConfigurationSection
    {
        const string Applications_String = "Applications";
        const string UseSocketThreadForHandlers_String = "UseSocketThreadForHandlers";
        const string SocketThreadBreakoutIntervalMs_String = "SocketThreadBreakoutIntervalMs";
        const string HandlerThreadBreakoutIntervalMs_String = "HandlerThreadBreakoutIntervalMs";

        [ConfigurationProperty(Applications_String)]
        public ApplicationCollection Applications
        {
            get
            {
                var applications = (ApplicationCollection)base[Applications_String];
                return applications;
            }
        }

        [ConfigurationProperty(UseSocketThreadForHandlers_String, DefaultValue = false, IsRequired = true)]
        public bool UseSocketThreadForHandlers
        {
            get
            {
                return (bool)this[UseSocketThreadForHandlers_String];
            }
            set
            {
                this[UseSocketThreadForHandlers_String] = value;
            }
        }

        [ConfigurationProperty(SocketThreadBreakoutIntervalMs_String, DefaultValue = 50, IsRequired = true)]
        [IntegerValidator(ExcludeRange = false, MaxValue = int.MaxValue, MinValue = 1)]
        public int SocketThreadBreakoutIntervalMs
        {
            get
            {
                return (int)this[SocketThreadBreakoutIntervalMs_String];
            }
            set
            {
                this[SocketThreadBreakoutIntervalMs_String] = value;
            }
        }

        [ConfigurationProperty(HandlerThreadBreakoutIntervalMs_String, DefaultValue = 50, IsRequired = true)]
        [IntegerValidator(ExcludeRange = false, MaxValue = int.MaxValue, MinValue = 1)]
        public int HandlerThreadBreakoutIntervalMs
        {
            get
            {
                return (int)this[HandlerThreadBreakoutIntervalMs_String];
            }
            set
            {
                this[HandlerThreadBreakoutIntervalMs_String] = value;
            }
        }
    }
}

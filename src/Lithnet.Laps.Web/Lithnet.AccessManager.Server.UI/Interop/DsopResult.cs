using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    public class DsopResult
    {
        public ReadOnlyDictionary<string, object> Attributes { get; private set; }

        public string Name { get; private set; }

        public string AdsPath { get; private set; }

        public string ObjectClass { get; private set; }

        public string Upn { get; private set; }

        internal DsopResult(DsSelection item, string[] attributesRequested, int attributeCount)
        {
            this.Upn = item.Upn;
            this.ObjectClass = item.ObjectClass;
            this.Name = item.Name;
            this.AdsPath = item.AdsPath;

            if (attributeCount > 0)
            {
                Dictionary<string, object> list = new Dictionary<string, object>();

                var vobj = Marshal.GetObjectsForNativeVariants(item.Attributes, attributeCount);

                for (int i = 0; i < attributeCount; i++)
                {
                    list.Add(attributesRequested[i], vobj[i]);
                }
                
                this.Attributes = new ReadOnlyDictionary<string, object>(list);
            }
        }
    }
}

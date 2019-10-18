using System.Configuration;

namespace Lithnet.Laps.Web
{
    public class TargetCollection : ConfigurationElementCollection
    {
        public TargetElement this[int index]
        {
            get => (TargetElement) this.BaseGet(index);
            set
            {
                if (this.BaseGet(index) != null)
                {
                    this.BaseRemoveAt(index);
                }

                this.BaseAdd(index, value);
            }
        }

        public void Add(TargetElement reader)
        {
            this.BaseAdd(reader);
        }

        public void Clear()
        {
            this.BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new TargetElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TargetElement) element).Name;
        }

        public void Remove(TargetElement reader)
        {
            this.BaseRemove(reader.Name);
        }

        public void RemoveAt(int index)
        {
            this.BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            this.BaseRemove(name);
        }
    }
}
using System.Configuration;

namespace Lithnet.Laps.Web
{
    public class ReaderCollection : ConfigurationElementCollection
    {
        public ReaderElement this[int index]
        {
            get => (ReaderElement) this.BaseGet(index);
            set
            {
                if (this.BaseGet(index) != null)
                {
                    this.BaseRemoveAt(index);
                }

                this.BaseAdd(index, value);
            }
        }

        public void Add(ReaderElement reader)
        {
            this.BaseAdd(reader);
        }

        public void Clear()
        {
            this.BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ReaderElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ReaderElement) element).Principal;
        }

        public void Remove(ReaderElement reader)
        {
            this.BaseRemove(reader.Principal);
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
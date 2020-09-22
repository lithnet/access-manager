using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.UI
{
    public class UiOptionsSnapshotProvider<TOptions> : IOptions<TOptions>, IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        private readonly TOptions options;
    
        public UiOptionsSnapshotProvider(TOptions options)
        {
            this.options = options;
        }
        
        public TOptions Value => options;

        public virtual TOptions Get(string name) => options;
    }
}


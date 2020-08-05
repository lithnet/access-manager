using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    public class AgentAppPathProvider :IAppPathProvider
    {
        public AgentAppPathProvider()
        {
            this.AppPath = Environment.CurrentDirectory;
        }

        public string AppPath { get; set; }

        public string TemplatesPath { get; set; }
        
        public string ScriptsPath { get; set; }
        
        public string WwwRootPath { get; set; }
        
        public string ImagesPath { get; set; }
        
        public string ConfigFile { get; set; }
        
        public string HostingConfigFile { get; set; }
        
        public string GetRelativePath(string file, string basePath)
        {
            throw new NotImplementedException();
        }

        public string GetFullPath(string file, string basePath)
        {
            throw new NotImplementedException();
        }
    }
}

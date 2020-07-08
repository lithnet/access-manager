using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager
{
    public interface IAppPathProvider
    {
        string AppPath { get; } 

        string TemplatesPath { get; } 

        string ScriptsPath { get; } 

        string WwwRootPath { get; } 

        string ImagesPath { get; } 

        string ConfigFile { get; } 

        string GetRelativePath(string file, string basePath);

        string GetFullPath(string file, string basePath);
    }
}
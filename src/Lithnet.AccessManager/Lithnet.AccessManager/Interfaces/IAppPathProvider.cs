﻿namespace Lithnet.AccessManager
{
    public interface IAppPathProvider
    {
        string AppPath { get; } 

        string TemplatesPath { get; } 

        string ScriptsPath { get; } 

        string LogoPath { get; }

        string ConfigFile { get; }

        string DbPath { get; }

        string HostingConfigFile { get; }

        string GetRelativePath(string file, string basePath);

        string GetFullPath(string file, string basePath);
    }
}
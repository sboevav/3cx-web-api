using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WebAPI.config;

public class ConfigurationService
{
    public string AppPath { get; private set; }
    public string ConfPort { get; private set; }
    public string ConfUser { get; private set; }
    public string ConfPass { get; private set; }

    public ConfigurationService(string filePath)
    {
        ReadConfiguration(filePath);
    }

    private void ReadConfiguration(string filePath)
    {
        var content = File.ReadAllLines(filePath);
        var iniContent = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);

        Dictionary<string, string> currentSection = null;
        foreach (var line in content)
        {
            var s = line.Trim();
            if (s.StartsWith("["))
            {
                var sectionName = s.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[0];
                currentSection = iniContent[sectionName] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }
            else if (currentSection != null && !string.IsNullOrWhiteSpace(s) && !s.StartsWith("#") && !s.StartsWith(";"))
            {
                var res = s.Split('=').Select(x => x.Trim()).ToArray();
                currentSection[res[0]] = res[1];
            }
        }

        AppPath = iniContent["General"]["AppPath"];
        ConfPort = iniContent["ConfService"]["ConfPort"];
        ConfUser = iniContent["ConfService"]["confUser"];
        ConfPass = iniContent["ConfService"]["confPass"];
    }
}
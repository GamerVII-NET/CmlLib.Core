﻿using System.Text.Json;
using CmlLib.Core.Files;
using CmlLib.Core.Internals;
using CmlLib.Core.Rules;

namespace CmlLib.Core.Version;

public static class JsonLibraryParser
{
    public static MLibrary? Parse(JsonElement element)
    {
        var name = element.GetPropertyValue("name");
        if (string.IsNullOrEmpty(name))
            return null;
        
        // rules
        IReadOnlyCollection<LauncherRule> rules;
        if (element.TryGetProperty("rules", out var rulesProp))
            rules = JsonRulesParser.Parse(rulesProp);
        else
            rules = Array.Empty<LauncherRule>();

        // forge serverreq, clientreq
        var isServerRequired = element
            .GetPropertyOrNull("serverreq")?
            .GetBoolean() ?? 
            true; // default value is true
        
        var isClientRequired = element
            .GetPropertyOrNull("clientreq")?
            .GetBoolean() ?? 
            true; // default value is true
            
        // artifact
        MFileMetadata? artifact = null;
        var artifactProp = element.GetPropertyOrNull("artifact") ?? 
                           element.GetPropertyOrNull("downloads")?.GetPropertyOrNull("artifact");
        if (artifactProp.HasValue)
            artifact = artifactProp.Value.Deserialize<MFileMetadata>();

        // classifiers
        IReadOnlyDictionary<string, MFileMetadata>? classifiers = null;
        var classifiersProp = element.GetPropertyOrNull("classifies") ?? 
                              element.GetPropertyOrNull("downloads")?.GetPropertyOrNull("classifiers");
        if (classifiersProp.HasValue)
            classifiers = classifiersProp.Value.Deserialize<Dictionary<string, MFileMetadata>>();

        // natives
        IReadOnlyDictionary<string, string>? natives = null;
        var nativesProp = element.GetPropertyOrNull("natives");
        if (nativesProp.HasValue)
            natives = nativesProp.Value.Deserialize<Dictionary<string, string>>();

        // some libraries (forge, optifine, fabric) lack 'artifacts' or 'classifiers' property;
        // instead they have metadata properties directly
        if (artifact == null && classifiers == null)
        {
            artifact = element.Deserialize<MFileMetadata>();
        }

        return new MLibrary(name)
        {
            Artifact = artifact,
            Classifiers = classifiers,
            Natives = natives,
            Rules = rules,
            IsClientRequired = isClientRequired,
            IsServerRequired = isServerRequired
        };
    }
}

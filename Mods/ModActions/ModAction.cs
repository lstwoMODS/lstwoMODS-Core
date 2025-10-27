using System;

namespace lstwoMODS_Core.Hacks.ModActions;

[AttributeUsage(AttributeTargets.Method)]
public class ModAction : Attribute
{
    public string Name;
    public string Tooltip = "";
}
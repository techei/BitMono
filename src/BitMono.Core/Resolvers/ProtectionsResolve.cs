﻿namespace BitMono.Core.Resolvers;

public class ProtectionsResolve
{
    public List<IProtection> FoundProtections { get; set; }
    public List<string> DisabledProtections { get; set; }
    public List<string> UnknownProtections { get; set; }
}
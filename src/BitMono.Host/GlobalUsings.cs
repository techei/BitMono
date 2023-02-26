﻿global using Autofac;
global using Autofac.Core;
global using Autofac.Extensions.DependencyInjection;
global using BitMono.API.Configuration;
global using BitMono.API.Ioc;
global using BitMono.Host.Configurations;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Serilog;
global using System;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.IO;
global using System.Reflection;
global using System.Text.RegularExpressions;
global using BitMono.API;
global using BitMono.API.Analyzing;
global using BitMono.API.Pipeline;
global using BitMono.API.Resolvers;
global using BitMono.Core;
global using BitMono.Core.Configuration;
global using BitMono.Core.Injection;
global using BitMono.Core.Renaming;
global using BitMono.Core.Resolvers;
global using BitMono.Shared.Models;
global using Serilog.Enrichers.Sensitive;
global using Module = Autofac.Module;
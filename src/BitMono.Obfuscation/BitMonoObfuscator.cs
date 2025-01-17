﻿using BitMono.Utilities.Runtime;

#pragma warning disable CS8604
#pragma warning disable CS8602
namespace BitMono.Obfuscation;

public class BitMonoObfuscator
{
    private readonly ProtectionContext _context;
    private readonly IEnumerable<IMemberResolver> _memberResolvers;
    private readonly ProtectionsSort _protectionsSort;
    private readonly IDataWriter _dataWriter;
    private readonly ObfuscationAttributeResolver _obfuscationAttributeResolver;
    private readonly ObfuscateAssemblyAttributeResolver _obfuscateAssemblyAttributeResolver;
    private readonly Shared.Models.Obfuscation _obfuscation;
    private readonly IInvokablePipeline _invokablePipeline;
    private readonly MembersResolver _memberResolver;
    private readonly ProtectionExecutionNotifier _protectionExecutionNotifier;
    private readonly ProtectionsNotifier _protectionsNotifier;
    private readonly ObfuscationAttributesStripper _obfuscationAttributesStripper;
    private readonly ObfuscationAttributesStripNotifier _obfuscationAttributesStripNotifier;
    private readonly ProtectionParametersFactory _protectionParametersFactory;
    private readonly ILogger _logger;
    private PEImageBuildResult _imageBuild;
    private long _startTime;

    public BitMonoObfuscator(
        ProtectionContext context,
        IEnumerable<IMemberResolver> memberResolvers,
        ProtectionsSort protectionsSortResult,
        IDataWriter dataWriter,
        ObfuscationAttributeResolver obfuscationAttributeResolver,
        ObfuscateAssemblyAttributeResolver obfuscateAssemblyAttributeResolver,
        Shared.Models.Obfuscation obfuscation,
        ILogger logger)
    {
        _context = context;
        _memberResolvers = memberResolvers;
        _protectionsSort = protectionsSortResult;
        _dataWriter = dataWriter;
        _obfuscationAttributeResolver = obfuscationAttributeResolver;
        _obfuscateAssemblyAttributeResolver = obfuscateAssemblyAttributeResolver;
        _obfuscation = obfuscation;
        _logger = logger.ForContext<BitMonoObfuscator>();
        _invokablePipeline = new InvokablePipeline();
        _memberResolver = new MembersResolver();
        _protectionExecutionNotifier = new ProtectionExecutionNotifier(_logger);
        _protectionsNotifier = new ProtectionsNotifier(_obfuscation, _logger);
        _obfuscationAttributesStripper = new ObfuscationAttributesStripper(_obfuscation,
            _obfuscationAttributeResolver, _obfuscateAssemblyAttributeResolver);
        _obfuscationAttributesStripNotifier = new ObfuscationAttributesStripNotifier(_logger);
        _protectionParametersFactory = new ProtectionParametersFactory(_memberResolver, _memberResolvers);
    }

    public async Task ProtectAsync()
    {
        _context.ThrowIfCancellationRequested();

        _invokablePipeline.OnFail += OnFail;

        await _invokablePipeline.InvokeAsync(OutputProtectionsAsync);
        await _invokablePipeline.InvokeAsync(StartTimeCounterAsync);
        await _invokablePipeline.InvokeAsync(OutputFrameworkInformationAsync);
        await _invokablePipeline.InvokeAsync(ResolveDependenciesAsync);
        await _invokablePipeline.InvokeAsync(ExpandMacrosAsync);
        await _invokablePipeline.InvokeAsync(RunProtectionsAsync);
        await _invokablePipeline.InvokeAsync(OptimizeMacrosAsync);
        await _invokablePipeline.InvokeAsync(StripObfuscationAttributesAsync);
        await _invokablePipeline.InvokeAsync(CreatePEImageAsync);
        await _invokablePipeline.InvokeAsync(OutputPEImageBuildErrorsAsync);
        await _invokablePipeline.InvokeAsync(WriteModuleAsync);
        await _invokablePipeline.InvokeAsync(PackAsync);
        await _invokablePipeline.InvokeAsync(OutputElapsedTimeAsync);
    }

    private Task<bool> OutputProtectionsAsync()
    {
        _protectionsNotifier.Notify(_protectionsSort);
        return Task.FromResult(true);
    }
    private Task<bool> StartTimeCounterAsync()
    {
        _startTime = Stopwatch.GetTimestamp();
        return Task.FromResult(true);
    }
    private Task<bool> OutputFrameworkInformationAsync()
    {
        _logger.Information(RuntimeUtilities.GetFrameworkInformation().ToString());
        return Task.FromResult(true);
    }
    private Task<bool> ResolveDependenciesAsync()
    {
        _logger.Information("Starting resolving dependencies...");
        var assemblyResolve = AssemblyResolver.Resolve(_context.BitMonoContext.ReferencesData, _context);
        foreach (var reference in assemblyResolve.ResolvedReferences)
        {
            _logger.Information("Successfully resolved dependency: {0}", reference.FullName);
        }
        foreach (var reference in assemblyResolve.BadImageReferences)
        {
            _logger.Warning("Failed to resolve dependency, this is not a .NET File: {0}", reference.FullName);
        }
        foreach (var reference in assemblyResolve.FailedToResolveReferences)
        {
            _logger.Warning("Failed to resolve dependency: {0}", reference.FullName);
        }
        if (assemblyResolve.Succeed == false)
        {
            if (_obfuscation.FailOnNoRequiredDependency)
            {
                _logger.Fatal("Please, specify needed dependencies, or set in obfuscation.json FailOnNoRequiredDependency to false");
                return Task.FromResult(false);
            }
        }
        return Task.FromResult(true);
    }
    private Task<bool> ExpandMacrosAsync()
    {
        foreach (var method in _context.Module.FindMembers().OfType<MethodDefinition>())
        {
            if (method.CilMethodBody is { } body)
            {
                body.Instructions.ExpandMacros();
            }
        }
        return Task.FromResult(true);
    }
    private async Task<bool> RunProtectionsAsync()
    {
        foreach (var protection in _protectionsSort.SortedProtections)
        {
            _context.ThrowIfCancellationRequested();

            await protection.ExecuteAsync(CreateProtectionParameters(protection));
            _protectionExecutionNotifier.Notify(protection);
        }
        foreach (var pipeline in _protectionsSort.Pipelines)
        {
            _context.ThrowIfCancellationRequested();

            await pipeline.ExecuteAsync(CreateProtectionParameters(pipeline));
            _protectionExecutionNotifier.Notify(pipeline);

            foreach (var phase in pipeline.PopulatePipeline())
            {
                _context.ThrowIfCancellationRequested();

                await phase.ExecuteAsync(CreateProtectionParameters(phase));
                _protectionExecutionNotifier.Notify(phase);
            }
        }
        return true;
    }
    private Task<bool> OptimizeMacrosAsync()
    {
        foreach (var method in _context.Module.FindMembers().OfType<MethodDefinition>())
        {
            if (method.CilMethodBody is { } body)
            {
                body.Instructions.OptimizeMacros();
            }
        }
        return Task.FromResult(true);
    }
    private Task<bool> StripObfuscationAttributesAsync()
    {
        var obfuscationAttributesStrip = _obfuscationAttributesStripper.Strip(_context, _protectionsSort);
        _obfuscationAttributesStripNotifier.Notify(obfuscationAttributesStrip);
        return Task.FromResult(true);
    }
    private Task<bool> CreatePEImageAsync()
    {
        _imageBuild = _context.PEImageBuilder.CreateImage(_context.Module);
        return Task.FromResult(true);
    }
    private Task<bool> OutputPEImageBuildErrorsAsync()
    {
        if (_obfuscation.OutputPEImageBuildErrors)
        {
            if (_imageBuild.DiagnosticBag.HasErrors)
            {
                _logger.Warning("{0} error(s) were registered while building the PE", _imageBuild.DiagnosticBag.Exceptions.Count);
                foreach (var exception in _imageBuild.DiagnosticBag.Exceptions)
                {
                    _logger.Error(exception, exception.GetType().Name);
                }
            }
            else
            {
                _logger.Information("No one error were registered while building the PE");
            }
        }
        return Task.FromResult(true);
    }
    private async Task<bool> WriteModuleAsync()
    {
        try
        {
            var memoryStream = new MemoryStream();
            var fileBuilder = new ManagedPEFileBuilder();
            fileBuilder
                .CreateFile(_imageBuild.ConstructedImage)
                .Write(memoryStream);
            await _dataWriter.WriteAsync(_context.BitMonoContext.OutputFile, memoryStream.ToArray());
            _logger.Information("Protected module`s saved in {0}", _context.BitMonoContext.OutputDirectoryName);
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "An error occured while writing the module!");
            return false;
        }
        return true;
    }
    private async Task<bool> PackAsync()
    {
        foreach (var packer in _protectionsSort.Packers)
        {
            _context.ThrowIfCancellationRequested();

            await packer.ExecuteAsync(CreateProtectionParameters(packer));
            _protectionExecutionNotifier.Notify(packer);
        }
        return true;
    }
    private Task<bool> OutputElapsedTimeAsync()
    {
        var elapsedTime = StopwatchUtilities.GetElapsedTime(_startTime, Stopwatch.GetTimestamp());
        _logger.Information("Since obfuscation elapsed: {0}", elapsedTime.ToString());
        return Task.FromResult(true);
    }
    private void OnFail()
    {
        _logger.Fatal("Obfuscation stopped! Something went wrong!");
    }
    private ProtectionParameters CreateProtectionParameters(IProtection target)
    {
        return _protectionParametersFactory.Create(target, _context.Module);
    }
}
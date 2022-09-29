<p align="center">
  <img src="https://raw.githubusercontent.com/sunnamed434/BitMono/main/BitMonoLogo.png" alt="BitMono" width="180" /><br>
  Free open-source protector for Mono, empty decompilers? bits? crashes?!<br>
  All this and even more right here
</p>

## Features
* Breaks decompilers (crash when analyzing types, no code, seems to C++ application)
* Strings encryption
* **[BitDotNet](https://github.com/0x59R11/BitDotNet)** (most of bit took from there)
* **[BitMethodDotnet](https://github.com/sunnamed434/BitMethodDotnet)** 
* Invisible types
* Call to calli
* FieldsHiding
* ObjectReturnType
* NoNamespaces
* FullRenamer

## Wiki 
Click **[here](https://github.com/sunnamed434/BitMono/wiki)** to open wiki about protections etc.

## Quick Start
`BitMono.CLI <path to PE file>/drag-and-drop/first file in Base directory or BitMono.GUI`

## Ignore renaming
To make sure class is in ignore make as shown in example
```cs
using System;
using System.Xml.Serialization;
using Newtonsoft.Json;

[Serializable] // Marking as serializable
class ProductModel
{
    [XmlAttribute("Product Name")] // Marking as Xml attribute
    string Name { get; set; }
    [JsonProperty("Product Description")] // Or marking as Json Property
    string Description { get; set; }
    [XmlAttribute("Product Price")]
    double Price { get; set; }
}
```

To make sure method is in ignore make as shown in example
```cs
using System.Runtime.CompilerServices;

class MyClass
{
    [MethodImpl(MethodImplOptions.NoInlining)] // Add this attribute to ignore renaming of method
    void MyMethod()
    {
        // potential critical code used to be here
    }
}
```

## Excluding of Having issues with third-parties (API/Libraries)
Open `config.json`

Add to `CriticalMethods`, `CriticalInterfaces` or `CriticalBaseTypes` your potential critical things if you have it. 
<br>There is already supporting all `Unity` methods and a few third-party frameworks (`RocketMod`, `rust-oxide-umod`, `OpenMod`)

```json
{
  "Watermark": true,
  "Logging": {
    "LogsFile": "logs.txt"
  },
  "Protections": [ // By default protections is already configured
    {
      "Name": "StringsEncryption",
      "Enabled": false
    },
    {
      "Name": "FieldsHiding",
      "Enabled": true
    },
    {
      "Name": "CallToCalli",
      "Enabled": false
    },
    {
      "Name": "ObjectReturnType",
      "Enabled": false
    },
    {
      "Name": "MethodsBreak",
      "Enabled": false
    },
    {
      "Name": "BitDotNet",
      "Enabled": false
    },
  ],
  "CriticalMethods": [
    // Unity, here is only a few in config you will see all methods that supports Unity
    "Awake",
    "OnEnable",
    "Reset",
    "Start",
    "FixedUpdate",
    // other tons of Unity methods
  ],
  "CriticalInterfaces": [
    // RocketMod
    "IRocketPlugin",
    "IRocketPluginConfiguration",
    "IDefaultable",

    // OpenMod
    "IOpenModPlugin"
  ],
  "CriticalBaseTypes": [
    // RocketMod
    "RocketPlugin",

    // OpenMod
    "OpenModUnturnedPlugin",
    "OpenModUniversalPlugin",

    // rust-oxide-umod
    "RustPlugin"
  ],
  "Tips": [
    "[Tip]: Mark your method with attribute [MethodImpl(MethodImplOptions.NoInlining)] to ignore renaming of your method!",
    "[Tip]: Open config.json and set FileWatermark to 'true', to disable watermarking of your file!"
  ]
}
```

Credits
-------
**[0x59R11](https://github.com/0x59R11)** for his investigation in big part of **[BitDotNet](https://github.com/0x59R11/BitDotNet)** that breaks files for mono executables!
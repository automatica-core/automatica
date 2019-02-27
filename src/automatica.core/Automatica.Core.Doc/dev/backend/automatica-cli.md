# Automatica-CLI
A command line interface tool to generate/create/push Automatica.Core.Drivers/Automatica.Core.Rules.

## Install
`dotnet tool install automatica-cli`

## Usage
You can see all available commands using
`automatica-cli --help`

Here are the few important ones

### New project
Create a new Automatic.Core project using the `GenerateProject` command.

```
GenerateProject <Type> <ShortName> <FullName> [<OverwriteExisting>] -options - Generates a new Automatica driver or rule project

    Option                   Description
    Type* (-T)               The type of the new project. Either driver or rule [Default='driver']
    ShortName* (-S)          The name of the new project
    FullName* (-F)           The name of the new project
    OverwriteExisting (-O)   Option to overwrite an existing destination folder [Default='False']
    WorkingDirectory (-W)    The working directory, if not set the current working directory will be used
```

### Pack
To pack your Automatica.Core project use the `Pack` command,
```
Pack -options - Generates a new Automatica driver or rule project

    Option                  Description
    Version* (-V)           The package version
    Configuration (-C)      The package configuration [Default='Release']
    WorkingDirectory (-W)   The working directory, if not set the current working directory will be used
```
The generated package can be pushed to the [Automatica.Store](https://store.automaticacore.com).

### Push
TODO:

To get your api-key please to to [developer.automaticacore.com](https://developer.automaticacore.com)
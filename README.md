# Overview

**PhotoArchiver** organizes and archives your photos and videos on the storage machine where it runs, at home or anywhere in the cloud.

A web interface allows you to easily transfer your files with a simple drag and drop of your files to your web browser window.

Photos and videos are organized based on the date they were taken. Date taken is extracted from various file metadata. (e.g. EXIF)

![PhotoArchiver web user interface](Documentation/screenshot.png "PhotoArchiver web user interface")

# How to use

## Build, deploy and run

In order to build and run **PhotoArchiver**, you need .NET Core to be installed on the hosting machine. (SDK 3.1 or higher)

After you cloned this repository, go to the `PhotoArchiver` directory and run:

```
dotnet publish -c Release
```

This will build **PhotoArchiver** in `Release` configuration and produce the directory `bin/Release/netcoreapp<version>/publish` containing everything needed to run it.

Deploy the content of this directory to the machine where you want to store your photos and videos, and run the following command (on the hosting machine):

```
dotnet PhotoArchiver.dll
```

Describing how to start an application for long term use is beyond the scope of this document, and depends on the platform it runs on.

## Configuration

In the `publish` directory, there should be a file named `appsettings.json` that contains the **PhotoArchiver** runtime configuration, as follow:

```
{
    "AppSettings": {
        "AllowedExtensions": [
            "png",
            "jpg",
            ...
        ]
    }
}
```

In the `AppSettings` section, you must add the following properties:

```
"TempAbsolutePath": "/path/to/temp/directory",
```

This is a temporary directory required for **PhotoArchiver** to store transferred files for processing before moving them to there final storage location.

```
"TargetAbsolutePath": "/path/to/files/storage"
```

This is the root directory where organized photos and videos are eventually stored.

## Listening end point

By default **PhotoArchiver** listens on all the network interface on port 5000.
If you want to change it, modify the code, in file `Program.cs` at line 28, where it looks like `.UseUrls("http://*:5000")`.
Change the number `5000` by whatever you want. Note that you have to do this before *publish*ing.

Sorry for those unfamiliar with technical details.

# For developers

## Interface

The project `PhotoArchiver.Contracts` provide the `IDateTakenExtractor` interface, which is defined as follow:

```cs
public interface IDateTakenExtractor
{
    bool ExtractDateTaken(Stream stream, out DateTime dateTaken);
}
```

Any class inheriting from the `IDateTakenExtractor` interface can participate in extending the supported file formats.

The classes implementing `IDateTakenExtractor` have to be registered in the `RootDateTakenExtractor` class (which is itself an `IDateTakenExtractor`) of the `PhotoArchiver` project.

## Implementations

For the moment there are 4 metadata formats supported: EXIF, RIFF, QuickTime and file name.

Their implementations are all in the `PhotoArchiver.DateTakenExtractors` project.

The class `LastModificationDateTakenExtractor` is not registered but is provided as legacy code and free for you to use at your own discretion.

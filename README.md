# ImportWC
Import WeatherCat .cat log files into Cumulus MX

## About this program
The ImportWC utility is a command line program written in .NET, so it will run on Windows or Linux. Under Linux you will have to use the dotnet runtime environment to execute the program.

The utility will read your WeatherCat log files and create corresponding Cumulus MX monthly log files. It does not create day file entries. After importing the monthly log files you must run the CreateMissing utility to create the day file entires from the ne wmonthly logs.

## Installing
Just copy all the files in the release zip file to your Cumulus MX root folder.

## Before you run ImportWC
You will need to edit the *wc_config.ini* file.

In this configuration file you tell ImportWC where to find your WeatherCat data files, and what units are being used by WeatherCat.

<br>

ImportWC has to be told the first date when you expect data to be available. To do this it reads the "Records Began Date" from your Cumulus.ini file.

By default his is set to the first time you run Cumulus MX.

If you have imported old data from another program, or another installation of Cumulus (and you have used the original Cumulus.ini file), then you will have to change the date in Cumulus MX to set it to the earlist date in your imported data.

You can edit the Records Began Date in Cumulus MX:

&nbsp;&nbsp;&nbsp;&nbsp;**_Settings > Station Settings > General Settings > Advanced_**

Alternatively (not recommended), you can edit the Cumulus.ini file directly. **You must edit the Cumulus.ini file with Cumulus MX STOPPED.**

The entry in Cumulus.ini can be found in the [Station] section of the file...

```` ini
[Station]
StartDateIso=YYYY-MM-DD
````

**_NOTE_**_: You must retain the same date format_.

However, if ImportWLK finds that the first date in your WLK files is earlier than the Records Began Date, it will use that date instead.

ImportWC also uses your Cumulus.ini file to determine things like what units you use for your measurements. So make sure you have all this configured correctly in Cumulus MX before importing data.

*_Note:_* The units used in Cumulus MX may be different from the units in the files you are importing, the units will be converted.

## Running ImportWC
### Windows
Just run the ImportWLK.exe from your root Cumulus MX folder
> ImportWC.exe
### Linux/MacOS
Run via the dotnet executable after first setting the path to the Cumulus MX root folder
> dotnet ImportWC.dll


## Post Conversion Actions
After running the ImportWLK convertor, you will need to perform some additional tasks to complete the migration:

### Run CreateMissing
The ImportWC utility does not add any data to your day file when it creates the monthly log files. You must run the CreateMissing utility to create the day file entries.

### Run the records editors in Cumulus MX
Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.

# ImportWC
Import WeatherCat .cat log files into Cumulus MX

## About this program
The ImportWC utility is a command line program written in .NET, so it will run on Windows or Linux. Under Linux you will have to use the dotnet runtime environment to execute the program.

The utility will read your WeatherCat log files and create corresponding Cumulus MX monthly log files. It does not create day file entries. After importing the monthly log files you must run the CreateMissing utility to create the day file entires from the new monthly logs.

If your WeatherCat data files contain synthetic channels, channels 1-10 will be extracted and written to a Cumulus MX Custom log called "SythLogYYYYMM.txt". Each WC synthetic channel is mapped to the corresponding field number in the custom log file.

You can configure this custom log name in Cumulus MX, and if the same data is available via web tags, continue to log to this custom log file.

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

However, if ImportWC finds that the first date in your .cat files is earlier than the Records Began Date, it will use that date instead.

ImportWC also uses your Cumulus.ini file to determine things like what units you use for your measurements. So make sure you have all this configured correctly in Cumulus MX before importing data.

*_Note:_* The units used in Cumulus MX may be different from the units in the files you are importing, the units will be converted.

## Running ImportWC
### Windows
Just run the ImportWC.exe from your root Cumulus MX folder
> ImportWC.exe
### Linux/MacOS
Run via the dotnet executable after first setting the path to the Cumulus MX root folder
> dotnet ImportWC.dll


## Post Conversion Actions
After running the ImportWC convertor, you will need to perform some additional tasks to complete the migration:

### Run CreateMissing
The ImportWC utility does not add any data to your day file when it creates the monthly log files. You must run the CreateMissing utility to create the day file entries.

You can find the CreateMissing documentation online here: https://github.com/cumulusmx/CreateMissing

### Run the records editors in Cumulus MX
There is no automatic way of updating the records in Cumulus MX. You will have to use the various records editors to manually update them. Load each records screen, and you can click record values and times from either the day file or the monthy log files and they will replace the current values.

Not having an automatic means of doing this is probably a good thing as it allows you to disregard (and possibly correct) silly values that may have crept into the day file and log files. If you have records from some previous software refer to those as well, as some transient values may have been recorded that are missing from the imported log files.

### Old WeatherCat log file formats
If you have have WeatherCar old log files going back before 2013 it is likely that these files have a slightly different format from the current files. Don't worry, ImportWC understands both formats, but the day the format changes causes problems. This day is likely to have huge rainfall figures that you will see when running the records editors in Cumulus MX. If this does occur you will need to manually correct the rainfall values in the day file. Once you have done that you can run the records editors again, and the huge values should no longer be present (though they will still be visible in the monthly log files - correcting those is a big task!)

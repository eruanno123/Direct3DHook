
Overview
--------

**Hellblade: Senua's Sacrifice** is a wonderful game with lacking support for loading specific point in the game.
It has some sort of explanation in the game plot (_no spoilers here!_) but after my playthrough I thought it would be 
nice if I could return to specific moments in the game (e.g. best fight moments :) ), without starting the game from scratch.

This simple hacked together in one hour from existing building blocks application makes backup of all save points during the playthrough. Just run it in the background before starting the game.

This is a work derived from Direct3DHook by Justin Stenning (https://github.com/spazzarama/Direct3DHook)

How it works
------------

The application keeps watching the Hellblade user data directory using `FileSystemWatcher`
class. On write event the _screen capture_ request is made. The screenshot data (_JPEG_ image) and copy of the save file are written to user-specified backup directory.
The application renames the files using user-provided or default formatting.

This application only makes backups. Restoring the save game needs to be done manually!

User guide
----------

Hopefully running executable without parameters is just enough. For further customization use the following parameters:

- `-h`, `--help` - print supported command line parameters
- `-f`, `--format=<fmt>` - backed file name format. If not specified the default format is `hellblade_{0:yyMMdd_HHmmss}_{1:000}_{2}`. Use C#-like format string with following placeholders:
    - `{0}` - date and time
    - `{1}` - sequential number
    - `{2}` - MD5 checksum (first 6 _hex_ digits).
    
- `-p`, `--process-name=<proc-name>` - Hellblade game executable name or path. 
    If not specified the default process name is `HellbladeGame-Win64-Shipping`. This is true 
    at least for GOG platform, not sure about others.
- `-o`, `--output-path=<path>` - Output directory where backup files should be stored.
    If not specified, by default `Backup` directory is created in `%LocalAppData%\HellbladeGame\Saved\SaveGames`
- `-i`, `--input-path=<path>` - Input directory where Hellblade used to store the save file.
    If not specified `%LocalAppData%\HellbladeGame\Saved\SaveGames` is used 
- `-w`, `--wildcard=<pattern>` - Files that are watched against write access. By default `*.sav`.
- `-s`, `--image-size=<WxH>` - Specifies target screen capture size, e.g. 640x480. By default
    the game resolution is used. **Note: image size other than default is not working (bug)**
- `-v`, `--verbosity=<0-4>` - Console trace verbosity: 0 - highest, 4 - lowest

Disclaimer
----------

The application does not decompile, disassemble, or reverse engineer the game or whatever could violate its EULA.
Software is provided "as is" without any warranties. _Hellblade_ is a trademark owned by Ninja Theory.
CriPakTools
===========

Tool to extract/update contents of CRIWARE's CPK archive format.

-----------

This is based off of code uploaded by Falo's code released on the Xentax forums (http://forum.xentax.com/viewtopic.php?f=10&t=10646) which was futher modified by Nanashi3 (http://forums.fuwanovel.org/index.php?/topic/1785-request-for-psp-hackers/page-4), then modified by EsperKnight (https://github.com/esperknight/CriPakTools) who turned it into a command line application, and added the ability to replace existing files inside the CPK archive.

I plan on making it able to replace multiple files with one command (one repack process) and make a GUI with drag'n'drop features later on. 

Endgame content would be identifying the .cpk file (which game it's from) and listing the possible files inside the archive that could be replaced (textures, models, ui etc). Mainly for JoJo All Star Battle and Eyes of Heaven.

To use (Legacy) :

* CriPakTool.exe IN_FILE - Displays all contained chunks.
* CriPakTool.exe IN_FILE EXTRACT_ME - Extracts a file.
* CriPakTool.exe IN_FILE ALL - Extracts all files.
* CriPakTool.exe IN_FILE REPLACE_ME REPLACE_WITH [OUT_FILE] - Replaces REPLACE_ME with REPLACE_WITH.  Optional output it as a new CPK file otherwise it's replaced.

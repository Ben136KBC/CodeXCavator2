# Welcome to CodeXCavator2
## A code indexing and search tool

CodeXCavator allows you to search your source code! It is very fast and can handle massive code sets where the index exceeds 2GB which is more than ctag and global can handle.

To use the application, you have three options:
- Download https://github.com/Ben136KBC/CodeXCavator2/blob/master/Deploy.zip, unzip and run CodeXCavator.exe
- Download the Deploy directory and run the CodeXCavator.exe in there.
- You can download the Portable Apps version [here](https://github.com/Ben136KBC/CodeXCavator2/blob/master/PortableAppsFiles/CodeXCavator2Portable_0.10_English.paf.exe)

No installation or registration or adminitrator rights is required. The application requires .NET 4.0.

CodeXCavator is essentially a derivative of CodeXCavator (see https://sourceforge.net/p/codexcavator/wiki/Home/) but with a number of changes made by Ben van der Merwe. The changes are as follows:

- Search text input widget is selected by default on startup.
- Searching is case insensitive by default.
- Application remembers its previous location and size when opened.
- The main view has a button to update the current index.
- The main view has a button to create a new simple index.
- If no index is specified on the command line, a view is open which allows the user to select an existing index, select a recent index, or create a new simple index.
- The last index used is remembered and can be opened by default. There is an option for this on the main view.
- The search results view (bottom right) has been changed slightly to fit the headings for each result onto one row, allowing more results to be shown.
- Support for Fortran with a syntax highlighter.

The changes were not made to the original CodeXCavator, because it suits the needs of the original author as is, and he is concerned about changes possibly breaking something. As required by the license agreement, modified files in this archive are clearly marked. This version is released under the same license agreement as the original.

Things I intend to work on:
- Add to PortableApps. A version is available for download above, but not through the official list of portable apps, as an app has to be compatible with .NET 2 or .NET 4, which is not really an option. See here: https://portableapps.com/node/59992
- Add an option to find the definition of something in the code.
- Sorting results by directory.

Last updated: 15 February 2019 by Ben van der Merwe.

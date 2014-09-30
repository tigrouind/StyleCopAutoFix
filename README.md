StyleCopAutoFix
===============
This console application automatically fix common StyleCop violations related to missing (or unnecessary) blank/empty lines : SA1505, SA1507, SA1508, SA1513, SA1514, SA1515, SA1516 and SA1518.

It only apply fixes for the violations listed above, these are the only ones I found which are safe and easy to fix (it is unlikely that these fixes will broke the code).

Usage : StyleCopAutoFix sln_filepath|csproj_filepath|cs_filepath
     
I also suggest to use the following script, which auto-format all files in a solution : https://gist.github.com/JayBazuzi/9e0de544cdfe0c7a4358. It fixes a lot of StyleCop violations (also in a safe way).

Application has been tested on several projects without any issues but I suggest to backup your files before proceeding.
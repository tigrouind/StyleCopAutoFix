StyleCopAutoFix
===============
This console application fix most common StyleCop violations related to blank lines : SA1516, SA1507, SA1508, SA1518, SA1505.

It only fix these issues, because there are easy to fix and safe (it is unlikely that it will broke the code).

Usage : StyleCopAutoFix full_path_to_sln_or_csproj_file

I suggest to also use this script, which auto format all files in a soluation : https://gist.github.com/JayBazuzi/9e0de544cdfe0c7a4358. It fixes a lot of StyleCop violations (in a safe way).

WARNING : Please backup your files before proceeding.
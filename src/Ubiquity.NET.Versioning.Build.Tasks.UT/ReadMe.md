# About
This library handles testing of the build tasks. It does so, by manually constructing test
projects that include a package reference to the Build tasks package (as a real world consumer
would do). It controls global properties, resolves the NuGet references, and then runs the task
'PrepareVersioningForBuild'. This is all made plausible by the MSBuild evaluation libraries in
combination with the [MSBuild ProjectCreator](https://github.com/jeffkl/MSBuildProjectCreator)
library.

Directly controlling the build using these libraries allows multiple test runs with different
parameters/options to validate all the flexibility offered by the tests. By validating the
actual package it validates in a manner that a real world consumer will use and not something
"faked".

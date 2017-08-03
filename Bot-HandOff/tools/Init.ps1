param($installPath, $toolsPath, $package, $project)

$readme = $installPath + "\readme.md";

$DTE.ExecuteCommand("File.OpenFile", $readme);
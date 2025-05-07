
nuget pack ../FUSE-fx.DynDB-JSONfs.nuspec -Symbols -OutputDirectory "..\dist" -InstallPackageToOutputPath


IF NOT EXIST "..\..\..\..\(NuGetRepo)" GOTO NOCOPYTOGLOBALREPO
xcopy "..\dist\*.nuspec" "..\..\..\..\(NuGetRepo)\" /d /r /y /s
xcopy "..\dist\*.nupkg*" "..\..\..\..\(NuGetRepo)\" /d /r /y /s
:NOCOPYTOGLOBALREPO

PAUSE
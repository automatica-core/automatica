$paths = Get-ChildItem $args[0] -Filter *Tests.csproj -Recurse
foreach($pathobject in $paths) {
    $cd = Split-Path $pathobject.fullname
   
	cd $cd
	
	dotnet add package coverlet.msbuild
	
	echo $cd
}

$paths = Get-ChildItem $args[0] -Filter *Test.csproj -Recurse
foreach($pathobject in $paths) {
    $cd = Split-Path $pathobject.fullname
   
	cd $cd
	
	dotnet add package coverlet.msbuild
	
	echo $cd
}
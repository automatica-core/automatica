$paths = Get-ChildItem $args[0] -Filter *.nupkg -Recurse
foreach($pathobject in $paths) {
    $path = $pathobject.fullname
    
	$filePath = $path	
	$apiKey=$args[1]
	$url="https://www.myget.org/F/automaticacore/auth/$apiKey/api/v3/index.json"
	
	dotnet nuget push $filePath -s $url
	
}

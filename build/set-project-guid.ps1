$paths = Get-ChildItem $args[0] -Filter *.csproj -Recurse
foreach($pathobject in $paths) {
    $path = $pathobject.fullname
    
	$filePath = $path	
	
	$xml=New-Object XML
	$xml.Load($filePath)
	$node = $xml.SelectSingleNode("//Project/PropertyGroup")
  
	$projectGuidNode = $node.ProjectGuid
	
	if($projectGuidNode -eq $null) {
		
		$projectGuid = $xml.CreateElement("ProjectGuid")
		$node.AppendChild($projectGuid)
	
		$projectGuid.InnerText =  [guid]::NewGuid().ToString().ToUpper()
		$xml.Save($filePath)
	}
	
	#Write-Host $projectGuidNode ($pathobject.name)
	
	
}

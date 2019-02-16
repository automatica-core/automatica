
$path = $args[0];
$vers = $args[1];

Get-ChildItem $path -Filter *.csproj -Recurse |
Foreach-Object {

    $content = $_.FullName
	echo $content
	$csprojPath = $content # path to the file i.e. 'C:\Users\ben\Code\csproj powershell\MySmallLibrary.csproj'
	$newVersion = $vers # the build version, from VSTS build i.e. "1.1.20170323.1"

	echo $newVersion
						
	Write-Host "Starting process of generating new version number for the csproj"
	
	$myBuildNumber = $newVersion;

	$filePath = $csprojPath
	$xml=New-Object XML
	$xml.Load($filePath)
	$versionNode = $xml.Project.PropertyGroup.Version
	if ($versionNode -eq $null) {
		# If you have a new project and have not changed the version number the Version tag may not exist
		$versionNode = $xml.CreateElement("Version")
		$xml.Project.PropertyGroup.AppendChild($versionNode)
		$versionNode = $myBuildNumber;
		Write-Host "Version XML tag added to the csproj"
	} else {
		$xml.Project.PropertyGroup.Version = $myBuildNumber
	}
	$xml.Save($filePath)
	
		
	Write-Host "Updated csproj "$csprojPath" and set to version "$myBuildNumber																															
}
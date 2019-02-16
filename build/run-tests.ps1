$sourceDir=$args[0]
$openCoverDir=$args[1]

download --extract --out $openCoverDir https://github.com/OpenCover/opencover/releases/download/4.6.519/opencover.4.6.519.zip

$testProjects =  Get-ChildItem -Path $sourceDir\src -Include @("*.Tests","*.Test") -Recurse -ErrorAction SilentlyContinue -Force | ?{ $_.PSIsContainer } | %{$_.FullName}

$latestOpenCover="$openCoverDir\OpenCover.Console.exe"


foreach ($testProject in $testProjects) {
$dotnetArguments = "xunit", "--configuration DebugTests"

    "Running tests with OpenCover " + $testProject
    & $latestOpenCover `
        -register:Path64 `
        -target:dotnet.exe `
        -targetdir:$testProject `
        "-targetargs:$dotnetArguments" `
        -returntargetcode `
        -output:"$openCoverDir\..\OpenCover.coverageresults" `
        -mergeoutput `
        -oldStyle `
        -excludebyattribute:System.CodeDom.Compiler.GeneratedCodeAttribute `
        "-filter:*"
		

}

echo "done.."
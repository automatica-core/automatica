$dir = $args[0];
$rootDir = $args[1]

cd $dir
dotnet test --configuration DebugTests  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura  /p:CoverletOutput=$rootDir
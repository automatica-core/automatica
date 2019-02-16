$paths = Get-ChildItem $args[0] -Filter *.opencover.xml -Recurse
$i = 0;

$tarDir = $args[1]

foreach($obj in $paths) {

	$m =  $obj.fullname;
	$name = $obj.name;
	
	$tarName = "$i-$name";
	$tarPath = "$tarDir\$tarName"
	echo "move $m to $tarName";
	
	Move-Item $m $tarPath;

	$i++;
}
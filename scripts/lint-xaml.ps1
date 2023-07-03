$files = Get-ChildItem *.xaml -Recurse | Select-Object -ExpandProperty FullName | Where-Object { $_ -notmatch "(\\obj\\)|(\\bin\\)" }

if ($files.count -gt 0)
{
  dotnet tool run xstyler -f $files
}
else
{
  exit 0
}

git diff --exit-code

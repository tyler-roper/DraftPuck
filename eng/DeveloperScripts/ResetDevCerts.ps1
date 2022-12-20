$certificateName = "BrewPuck.pfx"
$certificatePassword = "b30b3f16-18c1-4a00-8f8f-0ce0fba61cb2"
$appDataPath = "$env:USERPROFILE\AppData\Roaming\ASP.NET\Https"
$userAspNetPath = "$env:USERPROFILE\.aspnet\https"

Remove-Item "$appDataPath\$certificateName" -ErrorAction Ignore
Remove-Item "$userAspNetPath\$certificateName" -ErrorAction Ignore
dotnet dev-certs https --trust
dotnet dev-certs https -ep "$appdatapath\$certificatename" -p $certificatepassword
dotnet dev-certs https -ep "$useraspnetpath\$certificatename" -p $certificatepassword
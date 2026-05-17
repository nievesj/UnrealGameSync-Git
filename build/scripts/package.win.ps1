Remove-Item -Path build\UGSGit\*.pdb -Force
Compress-Archive -Path build\UGSGit -DestinationPath "build\ugsgit_${env:VERSION}.${env:RUNTIME}.zip" -Force

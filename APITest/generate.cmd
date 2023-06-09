

if "%ProgramFiles(x86)%"=="" (
    set svcutil="%ProgramFiles%\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.8 Tools\svcutil.exe"
    set options=/language:C# /serializer:XmlSerializer /useSerializerForFaults /messageContract
) else if exist "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools" (
    set svcutil="%ProgramFiles(x86)%\Microsoft SDKs\Windows\v10.0A\Bin\NETFX 4.8 Tools\svcutil.exe"
    set options=/language:C# /serializer:XmlSerializer /useSerializerForFaults /messageContract /syncOnly
) else if exist "%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.8 Tools\svcutil.exe" (
    set svcutil="%ProgramFiles(x86)%\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.8 Tools\svcutil.exe"
    set options=/language:C# /serializer:XmlSerializer /useSerializerForFaults /messageContract /syncOnly
) else (
    set svcutil="%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.8 Tools\svcutil.exe"
    set options=/language:C# /serializer:XmlSerializer /useSerializerForFaults /messageContract
)

@echo * Create proxy code for client on .NET 4.8 ...
@echo * svcutil = %svcutil%
@echo * options = '%options%'
%svcutil% %options% /namespace:*,MT.Laboratory.Balance.XprXsr.V03 /out:MT.Laboratory.Balance.XprXsr.V03.cs .\MT.Laboratory.Balance.XprXsr.V03.wsdl

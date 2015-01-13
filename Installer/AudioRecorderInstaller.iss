#define MyAppName "Audio Knight"
#define MyAppVersion "1.0"
#define MyAppPublisher ""
#define MyAppURL ""
#define MyAppExeName "AudioKnight.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{9B9EF0F2-795B-4FC9-898E-57F4450141DB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}/{#MyAppName}
UsePreviousAppDir=No
CreateAppDir=yes
OutputDir=output2
OutputBaseFilename=AudioKnight-v{#MyAppVersion}-Setup
SetupIconFile=application-icon.ico
Compression=lzma
SolidCompression=yes
DisableDirPage=yes
MinVersion=6.01.7600
Uninstallable=yes
PrivilegesRequired=admin
DefaultGroupName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{pf32}\{#MyAppName}"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Files]
Source: "..\AudioRecorder\bin\Release\*.exe"; Flags: ignoreversion; DestDir: "{app}"
Source: "..\AudioRecorder\bin\Release\*.dll"; Flags: ignoreversion; DestDir: "{app}"
Source: "..\AudioRecorder\bin\Release\*.config"; Flags: ignoreversion; DestDir: "{app}"
Source: "dotNetFx45_Full_setup.exe"; Flags: ignoreversion createallsubdirs recursesubdirs ; DestDir: "{app}/NET45Setup";
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Run]
Filename: "{app}\NET45Setup\dotNetFx45_Full_setup.exe"; StatusMsg: "Installing .NET Framework v4.5 ..."; Check: IsDotNetMissing

[Code]


function IsDotNetMissing(): boolean;
var
    key: string;
    install, release, serviceCount: cardinal;
    check45, success: boolean;
    version: string;
    service: cardinal;
begin

    version := 'v4.5';
    service := 0

    // .NET 4.5 installs as update to .NET 4.0 Full
    if version = 'v4.5' then begin
        version := 'v4\Full';
        check45 := true;
    end else
        check45 := false;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0/4.5 uses value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 uses additional value Release
    if check45 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= 378389);
    end;

    result := not (success and (install = 1) and (serviceCount >= service));
end;


;
; MB Studio Universal Installer Script
; by Johandros
;

!include x64.nsh
!include "header\mb_studio_universal.nsh"
!include "header\mb_studio_x86.nsh"
!include "header\mb_studio_x64.nsh"

;-------------------------------- 
;General

${UniversalProperties}

OutFile "universal\MB Studio - Installer (universal).exe"

;-------------------------------- 
;Installer Sections     
Section "Install" installInfo

  ${If} ${RunningX64}
    ${InstallDefault64BitFiles}
  ${Else}
    ${InstallDefault32BitFiles}
  ${EndIf}
  
  ${CreateApplicationLinks}
  
  SetRegView 32
  ${If} ${RunningX64}
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "EstimatedSize" "${SIZE_OF_FILE_64}"
  ${Else}
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "EstimatedSize" "${SIZE_OF_FILE_32}"
  ${EndIf}
  
;
; DOWNLOAD EXAMPLE WHICH COULD DOWNLOAD PYTHON AND MAYBE OTHER FILES LATER (UPDATER?)
;  
;  inetc::get "https://www.dropbox.com/s/x6fznmxh99b1mgn/test.txt?dl=1" "$INSTDIR\test.txt"
;  Pop $0 ;Return value from download - OK is good!
;  MessageBox MB_OK $0
;
    
  ${If} ${RunningX64}
    ${InstallPython64Bit}
  ${Else}
    ${InstallPython32Bit}
  ${EndIf}
  
  ${WriteMBUninstaller}
 
SectionEnd

${UninstallAllUniversal}

;--------------------------------
;Functions

Function .onInit
  ${If} ${RunningX64}
    SectionSetSize ${installInfo} ${SIZE_OF_FILE_64_INT}
  ${Else}
    SectionSetSize ${installInfo} ${SIZE_OF_FILE_32_INT}
  ${EndIf}
FunctionEnd

;--------------------------------
;eof
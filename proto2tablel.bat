@rem turn off echo - atsign is line-level way how to do it
@echo off
@rem provided your app takes three params, this is how to pass them to exe file
%~dp0\tool_proto2excel\build\consoleProto2Excel.exe %~dp0\table %~dp0\protobuf

pause
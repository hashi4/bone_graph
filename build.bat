@setlocal
@rem .net 4.0以上のコンパイラを適宜指定
@rem ↓はWindow10に同梱されているもの(64bit)
@set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

@rem PMXエディタのライブラリ位置を指定(pmxエディタをインストールしたパス\Lib)
@set PMXE_LIBPATH=..\Lib

@set LIBPATH=%PMXE_LIBPATH%\PEPlugin,%PMXE_LIBPATH%\SlimDX\x64
@set LIBS=PEPlugin.dll,SlimDX.dll

@set PLUGIN_NAME=bone_graph
@set SRC=%PLUGIN_NAME%.cs
@set TARGET=%PLUGIN_NAME%

@set TARGET=%TARGET%.dll

%CSC% /target:library /out:%TARGET% %SRC% /lib:%LIBPATH% /r:%LIBS%
@%endlocal

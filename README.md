<p align="center" width="50%">
    <img width="8%" src="https://cdn.discordapp.com/emojis/871436538087624805.png?v=1">
</p><h1 align="center">LuaSTG Editor Ava</h1>
<h4 align="center">

**LuaSTG Editor Ava** is a *code generator* for the LuaSTG engine, based on the *THlib* library.<br>
It is based on and forked from [**Sharp-X-Team**](https://github.com/Sharp-X-Team)'s [**Editor Sharp X**](https://github.com/Sharp-X-Team/LuaSTG-Editor-Sharp-X) fork, ported to [**Avalonia**](https://github.com/avaloniaui/avalonia) a cross-platform UI framework.

<br>

<h3>Usage</h3>
Games can be created for LuaSTG without the use of direct lua coding by using the editor. There are multiple nodes that equate to functions and code groups that can be placed within a hierarchy to generate compatible lua output code. This is the main way of producing content within the editor, but direct coding is permitted as well.
<br><br>
<p align="center" width="50%">
    <img width="100%" src="https://github.com/Sharp-X-Team/LuaSTG-Editor-Sharp-X/blob/main/CodeGenerationExample.png">
</p>

<br>

<h3>Compatibility</h3>
The Sharp X editor is designed to fit many variants of the LuaSTG engine.

<br>

Ava should ideally support what Sharp X supports, but only **LuaSTG Evo** has been tested for the moment.
<br>
<br>
Wine/Proton support is available through the compiler settings.
<br>
Ava will automatically detect Wine and Proton runners installed on the system and allow you to change what the compiler binary is ran through.
<br>
<br>
You can pass environment variables and flags through your runner of choice using the "Game Runner command" input:
<br>

```text
MYENV_VAR=1 %command% -myFlag
```
<br>
<br>
If you are wanting to use a bottles prefix for your runner you can select "Custom" from the dropdown menu:
<br>

```text
flatpak run --command=bottles-cli com.usebottles.bottles run -b "BottleName" -e %command%
```
<br>
<br>

<h3>Interface</h3>
<p align="center" width="50%">
    <img width="100%" src="https://github.com/burpdev/LuaSTG-Editor-Ava/blob/main/EditorPreview.png">
</p>

<br>
<br>

<h3>Developer Note</h3>
I mostly made this for myself since I didn't find a Linux editor similar to Sharp X. Work still needs to be done on certain areas, but everything that I've wanted is complete for the moment.

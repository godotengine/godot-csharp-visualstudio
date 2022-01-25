# Godot C# extension for Visual Studio

Visual Studio extension for the Godot game engine C# projects.

## Requirements

- **Godot 3.2.3** or greater. Older versions of Godot are not supported and do not work.
- **Visual Studio 2022**. VS 2019 or earlier are not supported.
- **Visit 1.x** branch to get the **Visual Studio 2019** supported version.

## Features

- Debugging.
- Launch a game directly in the Godot editor from Visual Studio.
- Additional code completion for Node paths, Input actions, Resource paths, Scene paths and Signal names.

**NOTES:**

- A running Godot instance must be editing the project in order for code completion and the `Play in Editor` debug target to work.
- Node path suggestions are provided from the currently edited scene in the Godot editor.

## Debug targets

- **Play in Editor**\
  Launches the game in the Godot editor for debugging in Visual Studio.\
  For this option to work, a running Godot instance must be editing the project.
- **Launch**\
  Launches the game with a Godot executable for debugging in Visual Studio.\
  Before using this option, the value of the _"executable"_ property must be changed
  to a path that points to the Godot executable that will be launched.
- **Attach**\
  Attaches to a running Godot instance that was configured to listen for a debugger connection.

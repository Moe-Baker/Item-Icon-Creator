# Intro
This project is an editor tool that helps you quickly author icons for items from prefabs.

# General Usage
The tool is provided as an editor window, located on the toolbar at "Window/Item Icon Creator"

![image](https://github.com/user-attachments/assets/b155e911-2125-4319-89d5-b011baf30c16)

The tool will automatically find compatible assets (ScriptableObjects inheriting `IPreviewItem`) and allow you to create icons for them.

![image](https://github.com/user-attachments/assets/f9c6feae-28fd-4db4-aca3-7385d964e71a)

# Script Implementation

The tool will automatiically scan your project for ScriptableObject instances implementing the `IPreviewItem` interface.
To register a custom asset, just simply have your scriptable objects implement the `IPreviewItem` interface and the tool will take care of the rest.

# Installation Steps

The tool is provided as a UPM (Unity Package Manager) package, to install it, navigate to the package manager window under "Window/Package Manager"

![image](https://github.com/user-attachments/assets/fa92bfeb-38bc-420c-b781-43982ccf6544)

Then enter the following URL https://github.com/Moe-Baker/Item-Icon-Creator.git and click install

![image](https://github.com/user-attachments/assets/fe44d895-1abc-4c3a-b251-85decd827c67)

# Samples

The tool comes with two samples.

1. Example Items:
- This sample showcases an implementation of sample items (scriptable objects + prefab) to quickly allow inspecting the tool.

<br/>

2. Override Preview Scene:
- This sample showcases the ability of overriding the scene used for generating the icons, the ability is implemented by searching for scenes with the "Item-Icon-Preview-Scene" label.
- The tool by default uses the Universal Render-Pipeline, but it can support any render pipeline easily by overriding the preview scene.

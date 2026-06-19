# Outlast Trials Mod Tool

An all-in-one, fast, and automated modding utility designed to streamline the modification process for The Outlast Trials.

---

## About the Project
The Outlast Trials Mod Tool serves as a centralized environment for creating custom modifications without the need for manual command-line tools or complex Unreal Engine packaging workflows. While the application is currently in its early development stages, it functions similarly to asset viewing utilities like FModel, but with a major architectural advantage: it enables creators to seamlessly modify, inject, and repack files directly into the game. 

---

## Core Functionality
The current build focuses on stabilizing core visual and textual adjustments, offering a reliable suite of tools for immediate asset manipulation.

### Texture2D Manipulation
The utility provides robust handling of game textures. Creators can browse the internal file structure, select any Texture2D asset, and export it into standard PNG and JSON formats. Once edited in an external graphics program, the custom PNG can be injected back into the utility with a single click, replacing the original asset seamlessly.

### Localization & Text Editing
A fully integrated, user-friendly editor handles the game's `.locres` string packages. This submodule allows developers to search through thousands of text strings, locate specific namespaces or keys, and directly translate or replace dialogue, menu items, and subtitles.

### Automated Mod Packaging
The tool completely automates the asset packaging pipeline. After modifications are made, the application reads the changes from a designated staging area, manages the internal folder structures, and compiles a ready-to-play `.pak` archive that the game can read natively.

### Universal Localization
To accommodate a global modding community, the entire interface features native multilingual support. The utility can toggle fluidly between English, Russian, and Chinese languages out of the box.

---

## Future Roadmap (July–August Update)
A massive engineering update is currently underway to expand the tool's capabilities into deeper systems of the Unreal Engine framework.

### UI & UMG Widget Viewer
The upcoming update will introduce an advanced JSON parser capable of reading internal game widget layouts. It will reconstruct the original WidgetTree and render a simplified, fixed-resolution visual preview of the user interface, utilizing a custom asset fallback system to safely substitute missing textures.

### Engine Logic & Blueprint Editing
Development is moving toward exposing core structural blueprints. This feature will allow advanced modders to review and alter underlying game logic, asset behaviors, and basic gameplay parameters.

### Audio Track Injection
A dedicated audio subsystem will be integrated to allow the extraction and replacement of native sound banks, enabling custom voice-overs, music tracks, and sound effect overrides.

### Interface & UX Overhaul
The entire toolset will receive a comprehensive design update, focusing on maximizing workspace efficiency, improving file search speeds, and smoothing out user interaction workflows.

---

## SOURCE CODE
To deploy a custom modification, initiate the tool and point it to the main installation directory of The Outlast Trials. Browse through the integrated file tree, select the desired texture or localization file, and open the modification panel. Once you have loaded your custom image or rewritten the text values, move over to the Mod Tab to inspect your active staging environment. Clicking the Create Mod button will compile your work into a packaged file; transfer this final `.pak` archive into the game's `~mods` folder to launch your custom content.

Compilation & Build Instructions

This section is provided for transparency, development purposes.
The instructions below will guide you on how to compile the exact same standalone executable that is distributed on the release page.

Prerequisites
To build this project from source, you will need:
* [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (or newer)
* Visual Studio 2022, JetBrains Rider, or VS Code.

Step-by-step Build Guide:
1. Clone the repository
Open your terminal or command prompt and clone the project to your local machine:
`git clone [https://github.com/DetectiveFl/TOT-Mod-Tool-core.git](https://github.com/DetectiveFl/TOT-Mod-Tool-core.git)
cd TOT-Mod-Tool-Core`

3. Restore dependencies
Ensure all necessary NuGet packages and dependencies (like CUE4Parse and MVVM toolkits) are downloaded:
`dotnet restore`

5. Publish the Standalone Release
To generate the single-file executable exactly as it is packaged for regular users, run the following command in the root folder of the project:
`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`

4. Locate the final build
Once the build process reports a "Build succeeded" message, you can find the final compiled .exe and any required external libraries in the following directory:
`\bin\Release\net8.0-windows\win-x64\publish\`

If you intend to move or share the release version, you must also include the "Tools" folder located in `OutlastTrialsMod/bin/Debug/net8.0-windows/`.
I hope this guide and tool prove useful.

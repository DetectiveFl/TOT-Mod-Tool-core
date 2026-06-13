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
git clone [https://github.com/DetectiveFl/TOT-Mod-Tool-core.git](https://github.com/DetectiveFl/TOT-Mod-Tool-core.git)
cd TOT-Mod-Tool-Core

3. Restore dependencies
Ensure all necessary NuGet packages and dependencies (like CUE4Parse and MVVM toolkits) are downloaded:
bash: dotnet restore

4. Publish the Standalone Release
To generate the single-file executable exactly as it is packaged for regular users, run the following command in the root folder of the project:
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

4. Locate the final build
Once the build process reports a "Build succeeded" message, you can find the final compiled .exe and any required external libraries in the following directory:
\bin\Release\net8.0-windows\win-x64\publish\

If you intend to move or share the release version, you must also include the "Tools" folder located in `bin -> Debug`.
I hope this guide and tool prove useful.

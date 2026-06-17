<div align="center">
    
# ![Extensions](../build/icon.svg)<br/>extensions
**Assorted Rhino 8 and Grasshopper components, including toolpath helpers for Robots**

[![License](https://img.shields.io/github/license/visose/extensions?style=flat-square)](../LICENSE)
[![Version](https://img.shields.io/github/v/release/visose/extensions?include_prereleases&style=flat-square)](../../../releases)
[![Repo stars](https://img.shields.io/github/stars/visose/extensions?style=flat-square)](../../../)
[![Sponsor](https://img.shields.io/badge/sponsor-gray?style=flat-square&logo=GitHub-Sponsors)](https://github.com/sponsors/visose)

**[About](#about) •
[Install](#install) •
[Credits](#credits)**

</div>

## About

**Extensions** is a **[Rhino 8](https://www.rhino3d.com/)** and **Grasshopper** plug-in with miscellaneous geometry, document, rendering, discrete-assembly, and robot toolpath utilities. It also includes a .NET 8 library for use from custom Rhino plug-ins or Grasshopper scripting components.

## Install
- Install in **Rhino 8.21 or newer** using the `_PackageManager` command, search for `Extensions`.
   > If you have an older manually installed version, delete `Extensions.gha` and `Extensions.dll` from the `Grasshopper Components` folder.
- Install **[Robots](https://github.com/visose/Robots)** from the package manager when using the robot toolpath components.

## Credits
This application makes use of the following libraries:
* Robots (https://github.com/visose/Robots)
* Clipper2 (https://github.com/AngusJohnson/Clipper2)
* Kendzi straight skeleton (https://github.com/kendzi/kendzi-math)
* Geometry3Sharp (https://github.com/gradientspace/geometry3Sharp)
* gsGCode (https://github.com/gradientspace/gsGCode)

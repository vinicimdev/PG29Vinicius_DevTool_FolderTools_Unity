# Folder Tools for Unity

Editor utility for customizing the Unity Project Window — color-code folders, add icon badges, and pin folders to a Quick Access panel.

## Features

- **Folder Colors** — tint any folder with a color via right-click or Alt+Click
- **Icon Badges** — overlay a built-in Unity icon on folders (C# Script, Material, Prefab, etc.)
- **Quick Access** — dockable panel for pinning frequently used folders

## Installation

**Package Manager**

1. Open `Window > Package Manager`
2. Click `+` → `Add package from git URL`
3. Enter:
```
https://github.com/vinicimdev/folder-tools.git
```

**Specific version**
```
https://github.com/vinicimdev/folder-tools.git#v1.0.0
```

## Usage

| Action | How |
|---|---|
| Set folder color | Right-click folder → **Folder > Customize**, or **Alt+Click** |
| Set icon badge | Right-click folder → **Folder > Customize** → Icon tab |
| Clear color/icon | Right-click folder → **Folder > Clear All** |
| Open Quick Access | **Window > Folder Tools > Quick Access** or `Ctrl+Shift+Q` |
| Add folder to Quick Access | Drag folder into the Quick Access panel |
| Navigate to folder | Click any entry in the Quick Access panel |
| Reorder Quick Access | Drag entries within the panel |
| Remove from Quick Access | Hover entry → click ✕ |

## Data

Settings are saved to `Assets/FolderToolsData/` in your project. Add this folder to version control if you want to share folder colors/icons with your team.

Add to `.gitignore` if you want per-user settings:
```
Assets/FolderToolsData/
Assets/FolderToolsData.meta
```

## Requirements

- Unity 2021.3+

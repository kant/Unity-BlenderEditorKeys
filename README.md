# BlenderEditorKeys
Single-file, drop-in Unity Editor script to give you Blender-like transformation hotkeys and functionality!

## Installation
1. Download `BlenderEditorKeys.cs` and place it in your project's `Assets` folder
2. That's it! The script should automatically run when the editor is open

**Recommended key bindings in `Edit > Shortcuts`**:
 - Rebind your `Tools/Scale` key as this script will override the default
 - Rebind your `Delete Current Selection` key to `X` to match Blender

## Usage
`G`rab objects  
`R`otate objects  
`S`cale objects  

`Left Click` or `Enter` to confirm  
`Right Click` or `Escape` to cancel  

### Locking transformation axis
Hit `X`, `Y`, or `Z` while transforming an object to lock the transformation to a single axis  
Hit `Shift` in combination with `X`, `Y`, or `Z` to lock transformation to the other two axis

### Reset transforms
Hit `Shift` in combination with `G`, `R`, or `S` to reset that transform to its default

### Pivots & Snapping
This script respects the Pivot and Snapping settings at the top left of the Unity editor  
Please refer to the Unity docs on how to change these settings  
You can also hold `Ctrl` for temporary snapping

### Transforming by an exact number
Simply start typing in a number while transforming an object to transform it by an exact amount  
Backspace and decimal keys work too!

## Known Issues
 - Reset transform modifier key must be `Shift` instead of `Alt` as the Unity editor seems to override and consume `Alt` keypresses  
 - Unity doesn't easily support scaling along global axis (skewing) like Blender does (not even the Unity gizmo will do this)  
 - Vertex/surface snapping and look-at rotation are not supported (too complex, use the normal Unity gizmo for these)  

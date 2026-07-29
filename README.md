# 8ish Bit Tower Defense
This is my attempt at making a tower defense game that is stuffed to the brim with procedural content generation techniques, so I can do as little level design as possible. This is for my Game Dev 3rd Year Module, so, logically, Main branch is protected.

Details:
The game is made in Unity's URP, currently some slight modifactions to the render pipeline are in place to add some stylisation. Such as:
  - Dropping the render scale, to make it more pixellated and avoid hard edges with imported assets.
  - Post Processing Values have also been used to make it *pop* a little more.

Currently the game uses some PCG techniques, mainly a seedbased randomisation to change the landscape as well as scatter the forest. The seed is randomly generated on each start. The terrain is created by creating a 2D array of points that get a random height based on their neighbours, these points are then used to draw triangles and form the terrain.

<img width="1042" height="437" alt="image" src="https://github.com/user-attachments/assets/819c0bb3-6b06-44aa-92f3-767a2cf5e228" />
*seed: 1149008041*

<img width="1032" height="437" alt="image" src="https://github.com/user-attachments/assets/9c4e9b94-2ce7-4efd-9b67-2d0172ea82ed" />
*seed: 1732580937*

The nav mesh automatically recalculates to match the terrain after the mesh is generated and the forests are scattered.

The tree's sway in the wind thanks to a shader made by Nicrom, you can access it here for free:
https://assetstore-fallback.unity.com/packages/vfx/shaders/low-poly-wind-182586
Many thanks go your way!


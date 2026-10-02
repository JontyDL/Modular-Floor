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

Enemies have been added, and follow some simple and more complicated rules. Enemies are spawned along the fringes of the map, they then pathfind towards the closest pathway to the tower, following it towards and attacking the central tower, diverting to attack any defenders along the way. All enemies are reused through an asset pool and and scale with increasing difficulty as the game progresses.

<img width="3840" height="1080" alt="PathingStrategy" src="https://github.com/user-attachments/assets/39dbb9b1-659e-4d22-baa0-29f7430b2d21" />

There are currently 3 Enemy types, a standard enemy with average speed, damage and health, there is a tank with high damage and health, but low speed, and a Bomber enemy, that has high speed and low health, that blows up once it reaches it's destination, dealing very high damage.

There are 5 tools in the players' toolkit that they can use to defend against these attackers, assuming that the play has the gold necessary to buy them. A typical archery tower, that attacks anything within it's range, a mill that increases the amount of gold earned for every enemy kill, a mine that generates gold over time, a bomb (with a ring demonstrating it's range) that explodes when an enemy gets too close, and a spell (scaled down to fit in the image) that doubles the damage that any attacker or defender may recieve if they are in the spell.

<img width="1116" height="617" alt="image" src="https://github.com/user-attachments/assets/5c56e3a4-6ba3-47d9-a374-b9f96c4f5684" />



The tree's sway in the wind thanks to a shader made by Nicrom, you can access it here for free:
https://assetstore-fallback.unity.com/packages/vfx/shaders/low-poly-wind-182586
Many thanks go your way!


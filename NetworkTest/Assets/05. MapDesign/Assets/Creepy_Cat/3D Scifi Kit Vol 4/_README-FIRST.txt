								------------------------------------
								Welcome to the 3D SciFi Kit Volume 4
								------------------------------------


--------------------
SUPPORT AND CONTACT:
--------------------

Email:
black.creepy.cat@gmail.com

Youtube: 
https://www.youtube.com/channel/UCvNtMt39uh_nJFZGT6qjgAQ/videos?flow=list&view=57

Facebook: 
https://www.facebook.com/BlackCreepyCat/

Forum:
https://forum.unity.com/threads/wip-3d-scifi-kit-vol-4-official-topic.1285424/



-----------------------
NOTE FOR THE BEGINNERS:
-----------------------

First all, i'm addressing to the Unity beginners, i'm tired of getting emails or reading reviews or you 
venting your anger when you see a red/yellow lines of text in the debug console. 

It's normal! When you import a package, unity display some warning or some minor red lines of text, in the console.

99% of these lines don't mean that the package is buggy, the only important lines, are the one that freezes
the Unity runtime when you click on the play button.... Else? Clear the console, the next time you reload the project
the importation warning/fake error etc... They gone... And funny thing, you will get some others! :)

So if you see red or yellow lines while importing the package, don't panic! Just clear the console after importing.
Pay no attention to this message! It appears only after execution, it do no block the runtime. 

---------
New note:
---------

And i recall that the demonstration scenes are there for example! It does not contain any display optimization because
it is up to YOU to do it! I'm not in your head, and i don't know your project... So if you take, for example, the provided
scene to build your game, you must also control the display of the objects. 

That's why it's pointless to write reviews about too many details in the scenes and/or that you don't understand that
my scenes don't go to 120 FPS...

It's normal, i always work like this. I prefer to torture the GPU and stop when the FPS number is too low. Because if 
it runs without optimization => the result will be better with it!

So please stop posting messages about the famous "package optimization problem!" because the display optimization 
is on your side. My job is to provide you, light polygon datas and number of textures => Not to do the graphic optimization
of your game. Do you think that a game like Witcher, display ALL the entire maps objects in realtime?

I often use the example of the minecraft cubes in my examples:

Imagine i make a package with different minecraft cubes. You will tell me, in this case, that the package is optimized!
But what would you say? if in the demo scene, I put 45 million minecraft cube without any display optimization? 
You will tell me that the package is not optimized....

Which has nothing to do :) Data weight is not only to consider, it's the number!

Informations for beginners about "optimization" :

- Important (topic "Mode angry ON"): 
  https://forum.unity.com/threads/released-3d-scifi-kit-vol-4-official-topic.1285424/page-4

- Lightmapping: 
  https://docs.unity3d.com/Manual/Lightmappers.html
  https://docs.unity3d.com/Manual/progressive-lightmapper.html
  https://blog.unity.com/engine-platform/5-common-lightmapping-problems-and-tips-to-help-you-fix-them

- Occlusion culling: 
  https://docs.unity3d.com/Manual/OcclusionCulling.html
  https://christopherhilton88.medium.com/why-occlusion-culling-improves-performance-in-unity-887f14227fba
  https://learn.unity.com/tutorial/working-with-occlusion-culling
  https://thegamedev.guru/unity-performance/occlusion-culling-tutorial/

- Optimization VR: 
  https://docs.unity3d.com/2020.1/Documentation/Manual/MobileOptimizationPracticalGuide.html
  https://www.intel.com/content/www/us/en/developer/articles/technical/optimization-for-unity-software-and-virtual-reality-run-time-generated-content.html


-------------------
ABOUT HDRP/LWRP/URP
-------------------

For the moment the 3D Scifi Kit 4 is compatible with the shader standard. It is possible to convert it to HDRPP/URP
or others. But it will take you a little work. When i deem that HDRP will be reliable, I will convert my products 
to this engine.

But for the moment it is far from being the case... If you want to try the experience, i recommend the Unity Guru 
youtube channel. You will find plenty of information to help you convert packages to HDRP/URP.

https://www.youtube.com/watch?v=VD5Qr4Rt7-Q


---------
ABOUT PBR
---------

By default, i put a clean (without so much scratchs) detailled map : Dirt_A.png with a tilling of two, but try the 
others detailed maps i provide : Dirt_B.png (for more dirt) try to use them to make tests of "Secondary Maps" for
materials.

----------------
TAKE A LOOK HERE
----------------

Here is a serie of links to help you to improve the kit rendering. Do not hesitate to test them! 

- Great! This guy is a genius: https://github.com/keijiro
- Great! This guy is a genius: https://github.com/SlightlyMad?tab=repositories

- Effect (tested): https://github.com/keijiro/ContactShadows
- Effect (tested): https://github.com/keijiro/KinoObscurance
- Effect (tested): https://github.com/keijiro/KinoStreak
- Effect (tested): https://github.com/keijiro/KinoBloom
- Effect (tested): https://github.com/SlightlyMad/VolumetricLights

- Effect (no tested, but seem nice): https://github.com/keijiro/DeferredAO
- Effect (no tested, but seem nice): https://github.com/keijiro/KinoFog
- Effect (no tested, but seem nice): https://github.com/keijiro/SonarFx

- Effect (no tested, but seem nice): https://github.com/ArthurBrussee/Vapor


--------------------------------------
HOW TO IMPORT THE KIT (Step BY Step) :
--------------------------------------

- Create a new "Standard" project (No HDRP or URP or LWRP)

- Into BUILD SETTING / PLAYER SETTINGS put the rendering mode into "deferred / linear" instead "forward / gamma"
  https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html

- Switch the project rendering mode to LINEAR instead GAMMA (better rendering with post Fx) 
  https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html

- Switch the camera to "Legacy Deferred" or "Deferred" 	(The project as been made for deferred)	
  https://docs.unity3d.com/Manual/RenderingPaths.html

- Via the package manager, install the "Post Processing Effects"
  https://www.youtube.com/watch?v=-rO3XdeVsPI

- Import the package 3D Scifi Kit Vol 4.

- Once made, you can use the file : "Camera Profile" i provide, on your camera to get the same render result than videos.

NOTE: Do not write a review because you see red or yellow lines of text in the console! Please, be adult... Just clear
it and it's done! Unity talk so much when importing a package! 99,99% Of those lines are not important! The only important
text lines, are the red lines that block the runtime (in this case, send me a email)... Forget the others! If you reload 
the project 99% of them do not appear again.

Example of lines:
- "Couldn't create a Convex Mesh from source mesh "XXXXXX" within the maximum polygons limit (256)." 
- "Visual Studio Editor Package version XXXXX is available"
- "Calling Deallocate on pointer, that can not be deallocated by allocator ALLOC_TEMP_THREAD"

Here is some informations about the post processing effect.
https://docs.unity3d.com/Packages/com.unity.postprocessing@2.0/manual/index.html


-------------
ABOUT THE KIT
-------------

The 3D SciFi Kit 4, is a huge modular kit, which allows you to create levels for your games! You can create corridors, 
for your FPS games. Or, you can create your own Martian base, for your exploration or survival games!

A wide choice of creation is possible! Many elements, allow you to create things for your levels. A bit like Lego toys. 
For information, all the buildings you see in the video, have all been created with the kit parts.

With this new kit, i wanted to create a real production tool. Which allows you a high level of customization in the 
choice of colors. Indeed, you can customize your own colors! I used new production methods, which allow you greater 
freedom of action and customization.

Among these new working methods, i also included a lot of objects, composed of several children. This allows you to 
optimize your scenes, by adding or removing 3d details. Object modeling is low poly, but this kit was not designed 
for mobile or vr devices. 

But you can, i think, use it anyway, in this case you need to create a "light" scene, to optimize the display.

I also wanted a high level of interactivity with this product.Indeed, many scripts allow you to interact with the 
elements of the kit. For example, crates, windows, doors, cupboards, lights. To help you in your project, and free 
you from this kind of constraint.

Lights and particles are included with all elements! This will allow you to easily create luminous atmospheres 
in your levels with ease.

Enjoy with this kit :)

--------------------------------
Kit Roadmap for the next updates
--------------------------------

- First release with the necessary kit pieces to build some Bases/Hangar with props

- The Vehicles and the Spaceships or props will come later during the next updates


-------------------------------
FAQ, frequently asked questions
-------------------------------

- Know bug: Impossible to get the normal map appear on the terrain maps Demo_A/B but it appear on the map Demo_C...



- Creepy Cat! Your package suxx! it's not optimized... I get different FPS on two machines!

It's normal, all my scenes are made WITHOUT display optimization... if you want get higher FPS take a look 
about the occlusion culling (menu windows/rendering/occlusion culling): https://docs.unity3d.com/Manual/OcclusionCulling.html
Why working like this? When you are a professional, you need permanently to know the polygonal charge. Working without 
artifice, allows you to control the load: "If the FPS is rather good without occlusion, it will be even better with..."

	"There is no display optimization into the provided scene! Stop to talk/mail me about the "kit optimization"!
	YOU NEED TO DO IT BY YOURSELF! I provide the top quality i can do, in term of low poly modeling meshes/textures
	set number, but you need to optimize the kit scenes display (and the prefabs!) for your platform/needs."

	You want a optimized prefab, from one of mine? 
	----------------------------------------------
	- Duplicate it => Rename it (XXX_Optimized) => Unpack it => Remove the no needed stuff (Pointlight/Details) => Resave it!
	- Explore my kit! Make it your own! I find it incredible to have to write this kind of thing... Do you think it is 
	possible to have a kit with perfect scene rendering, compatible with all platforms? And the same FPS? My datas are optimized
	but not the scenes. I do it on purpose to test the kit to its limits in the case of NON-optimization.



- The look of your kit is to "plastic"!

Don't forget that a kit must be able to fit into all types of productions. 
Personally, i like clean scifi renderings like "Oblivion" or "Star Trek" :) 
But for those who want to dirt the textures a bit more, you can use 
the Dirt_A (Clean) or Dirt_B (Dirty) maps in the "Secondary Maps" / "Detail Albedo x2" slots


- Is the kit compatible with the SCfi Kit V3 or V2

No... The production methods is different, the objects modeling is different, the units are differents...
You can add the both kit in your project i think, but you CAN'T mix the pieces between each others...


- Can i vent my anger or seek support in product reviews?

No no no and no! You must use the different means of communication, mail or facebook to talk to me. 
I do not respond to any support request in the reviews and Unity will delete it...


- Creepy Cat! How doors are moving?

Look the kit! :) All animated things are relative to a very small open source lib called Uween! I contacted the autor
to get the permission to use it in the kit (Dir: Common Scripts/Uween), take a look on my tests to understand the lib.
Why this one? To animate with ease... The system is a bit hard to understand the first time, but this lib is perfect!
with a bit of reflexion you can animate everything... See (code Simple_D, you can see how interact a light intensity
via a fake gameobject position in space). To understand what type of curve to use, look the bitmap called 
"Curve" (Dir: Common Scripts/Uween)


- Are the updates free?

Yes of course, the kit will be offered at a reduced price for the launch, and the updates will be free and 
without time limit.





									  Creepy Cat

             _,'|             _.-''``-...___..--';)
           /_ \'.      __..-' ,      ,--...--''''
          <\    .`--'''       `     /'
           `-';'               ;   ; ;
     __...--''     ___...--_..'  .;.'
    (,__....----'''       (,..--''   


---------------
About Updates :
---------------

- 1.0 : - First release



- 1.1 : - New cool spaceship included, inspired by Oblivion movie Bubble Ship.
        - New metalmaps added (*.Metal_B), to get a more metalic rendering on the materials slots "*_ID_Grey_A" 
		- Take a look to "Textures/_For GFX Artists/Cavity" it's a good start to make your own metalic maps.
		- Do not hesitate to play with Photoshop and the metal map bitmaps :) To get different kit render!



- 1.2 : - Few fix on objects/prefabs.
		- Fixed directory tree for folder prefabs.
		- New many small details added in the first scene, DevMap scene updated too.
		- Fix on the Bubble Spaceship, new interior stuff...
		- Fix on the scene, particles inclusion etc...
		- Windy calm ambiant sound included (ripped from Vol3...), used into Demo_A
		- Many panels console,added,buttons,screens etc, take a look inside: "Prefabs/Technic"
		- New Computer_Hori & Vert textures included to fill monitors with LCD display style.

		- Included a new class: LedButtonRandText to allow you to make buttons with a number change randomly
		  Check: P_Panel_Buton_G_01/H_01 / P_Panel_Mini_Screen_Anim_01 etc...

		- Included a new class: LedButtonSeqText it allow you to make sequencial text display
		  Check: P_Panel_Screen_01_Anim_D



- 1.3 : - New class included: ObjectTowardOnlyYaw it allow you to toward a object to another but only by yaw rotation
          usefull for solar panel, lock the script on your sun light, and they will follow him! :)

		- Modification of mouselook: I included a boolean allowing to use it only in rotation mode
		  with this you can use it in pilot head mode or static mode (Demo_B).

		- New map included, with a new seamless terrain/rock/props. It's a flight ride. I hope you like it!?
		  This is a big battlefield/exploration/testing terrain with various slopes i gona use now to present the news props!
		  Note: Two versions of the map, one for physic playing, and the other for the roadtrip automatic.

		- A new library included: BezierSolution (Look inside:Common Scripts) https://github.com/yasirkula/UnityBezierSolution
		  a very cool animation system based on spline, very nice to use! Creating an animation like the flight trip (Scene B)
		  is "a pain in the ass" to do with the standard unity keyframe system... Creating a good and nice spline take time, but
		  what a pleasure to see some spline animation into you level!

        - Map Demo_A FPS increased! A bad terrain setup drop down the FPS... Fixed now!
		- New prefabs with big machines/structures/bridge etc...
        - New prefabs with physic.

		- New shoot system! I replaced/debug/organized many of the old code, to give you a better feeling with shoot.
		  and now the explosions (for shoot death, and for hit object death) generate a physic shockwave! It move all
		  the rigodbody's around (class: ShootExplosionSetup) you can use it for any dying object (ship crash etc) take 
		  a look on this class :) The explosion lightning system is better now!

		- Class UniFPSCounter modified, now you have a real Crosshair and no more ugly GuiText :)



- 1.4 : - New class addon for the Super mouse look! It allow you to transform the frelook camera into a FPS camera (see demos)
		  it's not the best FPS camera! But a simple system that don't need any other package to run. It's a physic camera controler
		  sensible to the explosions! If someone can code the "crouch" i don't say no! :)

		- New map added! Demo_C, when i made the elevator script, i was inspirated by a Star Citizen video showing
		  a big plateform map inside a asteroid crater. 

		- New closed terrain added (Terrain_C), a big creater with a mountain in the center. Usefull to make closed maps.

		- SupermouseLook class modification, added a public bool to allow you to clamp camera pitch to -90/90. Used by the plugin FPS.

		- Jump/Land/Step sound added.

		- ShootLaunchFrom class bug removed (stuff about sound of shoot <> sound of the projectile).

		- Many fix on some objects colliders! The FPS camera show me where it missed collider... Because 90% of the time
		  i'm controling my work with the freelook with no collision camera (quick/easy for me...)

		- ShootLaunchFrom class modified! Now you can add the launcher collider! To avoid to shoot yourself...

		- And many things/fix/prefabs i don't remember...



- 1.5 : - New vehicle included, a light 2 places STV Car!

		- New class included RoverSuspension to help you to setup your car suspension.
		
		- New class included RoverWheelDrive to help you to drive your car.

		- New class included RoverAudio to help you to setup the rover sound.

		- New class included BoxCarWizard to help you to make some vehicles with a menu wizard (vehicles). 



- 1.6 : - Modification of the KitManager class, you can now play or not the gravity switch sound.

		- Freelook prefab sounds included. 

		- New station and scene included (Demo_D_SpacePort_A) this scene will change in the futur...

		- New prefab added to build your own spaceport.

		- If you are a Unity beginner, please read : NOTE FOR THE BEGINNERS



- 1.7 : - New meshes/prefabs included.

		- KitManager modified to include a stop motion effects (time slow motion). 

        - New prefabs with pre builded interiors provided, to help you to understand the kit!

		- New scene included with a partial station with interiors.

		- New explosions / particles / fires effects included.



- 1.8 : - New weapon modular rifle added! :) Check the directory : Prefabs/Props/_Weapons



- 1.9 : - New spaceship modular kit! I hope you like it :) (Bunch of work...)

		- New kit price due to the spaceship kit...

		- New kit lightning manager, it allow you to get a cool number of pointlight without to much FPS drop.

		- New prefabs added without lithning (Ex: P_Panel_Mini_Screen_02_NoLight) Extension
		  use them if you want to optimize your scenes. The normal versions contain many pointlight.

		- A global scenes fps optimization, it was simple :) I "disabled" all the no necessary 
		  pointlights... There is bunch of them alive, to get a good rendering quality with 
		  post FX, but you can/need to disable them to get more FPS! 


		  

		  
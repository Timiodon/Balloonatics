# How to open the project
1. Install Unity version 6000.0.23f1
2. Open this repository as a Unity Project

# How to run a demo scene
1. In the opened Unity project, you can find three pre-arranged demo-scenes under `Project > Assets > Scenes`
2. To run any of those in the editor, you can just open it with a double click and then press the play button in the Unity editor. However, for better performance, you might want to build a standalon application, which you can do under `File > Build Profiles`, where you can select the scene you want to build, as well as other building options. When you are finished, you can press `Build and Run`

# What to do, if the performance is bad?
You can trade off performance for accuracy by changing the `Simulation Loop Substeps` attribute (which controls the number of subloop iterations of the XPBD algorithm) of the `Solver` Script attached to the `Solver` GameObject located in the `Hierarchy` view. Note that the `DemoScene` and the `LiveDemoScene` run with more than 60 FPS in realtime on one of our PCs and the `SampleScene` runs with 20-50 FPS. 
The `DemoScene` in particular is the one we showed live at the presentation, so it should even run in realtime on a laptop.

# What parts of the project were done by us?
- We wrote all of the Scripts contained in `Project > Assets > Scripts` ourselves, except for the `FreeFlyCamera.cs` as well as the `NormalSolver.cs`, but the latter one we optimized to reduce garbage collection.
- We designed all scenes (`Project > Assets > Scenes`) and their UI. 
- We modeled the `SimpleBalloon.fbx` mesh ourselves using Blender.

# Codebase structure
The main loop of the XPBD algorithm is contained in `Solver.cs`. It has references to all the objects being simulated (e.g. `ClothBalloon.cs` or `RigidBody.cs`). A simulation object offers an interface to the solver for simulating all it's particles. Additionally, it holds references to all its attached constraints (e.g. `StretchingConstraint.cs`). In turn, those constraints expose an interface to the simulation objects for adding / solving those constraints.

To handle collisions, the solver also makes use of the interface exposed by `CollisionHandler.cs`, which internally makes use of `SpatialHashGrid.cs`.

# Related Work 
We took inspiration for parts of our code from the following sources:
- One of the XPBD papers (see project proposal PowerPoint)
- Exercise 4 of the PBS course
- [Matthias Mueller Cloth simulation physics tutorials](https://matthias-research.github.io/pages/tenMinutePhysics/index.html)
- [Soft body tutorials from the blackedout01 YouTube channel](https://www.youtube.com/@blackedoutk)

Additionally, we took the code for the `NormalSolver.cs` from [here](https://gist.github.com/runevision/6fd7cc8d841245a53df5d09ccf6b47ff).
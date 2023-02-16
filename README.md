# README

This is a small inverse kinematics package for Unity3D primarily based upon the Forward-and-Backward-Reaching Inverse Kinematics (FABRIK) algorithm. At this time is has support for pole targets and joint-level angular constraints on top of basic functionality.

## Add this to a Unity project

1. Open the Unity Package Manager for your desired project

2. Hit the plus icon in the top left corner of the Unity Package Manager window

3. The package manager provides two ways to add the IK Toolkit: From disk, or from Git URL.

  * From Disk: Download this repo as a .zip file, extract it, and when prompted by the package manager, select the extracted directory as the package source.

  * From Git URL: Type in the https or ssh URL provided by this repo into the package manager's search field.

4. After this, a tab in the Package manager should have appeared, titled "Packages - ahanlindev", and underneath it should be "IK Toolkit x.x.x"

For instructions on usage once added, see the [documentation](/Documentation~/DOCUMENTATION.md).

## Sources

### FABRIK algorithm: 
Aristidou, Andreas, and Joan Lasenby. “Fabrik: A Fast, Iterative Solver for the Inverse Kinematics Problem.” Graphical Models, vol. 73, no. 5, May 2011, pp. 243–260., https://doi.org/10.1016/j.gmod.2011.05.003. 

### Pole and misc. logic:
DitzelGames, "C# Inverse Kinematics in Unity", YouTube, May 11, 2019, https://youtu.be/qqOAznS05fvk

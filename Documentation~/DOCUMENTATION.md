# ahanlindev Inverse Kinematics Package

## Overview

This package offers modest inverse kinematics functionality based upon the Forward-and-Backward-Reaching Inverse Kinematics (FABRIK) algorithm. Further support has been added for joint-level rotational constraints.

## Package Contents
 - `IKChain` component class
    - Main source of functionality
    - A `GameObject` that contains this component will act as the root joint of a kinematic chain
    - If end-effector and target `Transform`s are supplied, the chain will apply the FABRIK algorithm at runtime to attempt to move the end-effector to the target, affecting all intermediate positions in the heirarchy.
        - The end-effector must be a descendant of the `GameObject` that carries the `IKChain` component. 
    - A chain has multiple fields that can be filled in the Unity Inspector:
        - `End Effector`: A `Transform` that denotes the end of the chain. Must be a descendant of the `GameObject` that carries the `IKChain` component.
        - `Target`: A `Transform` that denotes the position that the algorithm will attempt to place the end-effector. Any transform within the same scene as the object bearing this component may be used as a target.
        - `Pole Target`: A `Transform` that acts as a "secondary target" that intermediate joints in the chain will favor towards 
        - `Max Bend Angle`: The number of degrees that a given joint in the chain is allowed to deviate from the orientation of the last joints preceding it. If a joint has an `IKJoint` component, this value is ignored for that joint.
        - 
 - `IKJoint` component class
    - Container for joint restraints. At this time, the only type of joint supported is a conal region defined by a direction and an angle.
    - If any `GameObject`s that are between an `IKChain` and its end-effector carry an `IKJoint` component, whatever restraints are set in that joint are considered in the algorithm.  
    - A joint has multiple fields that can be filled out in the Unity Inspector:
        - `Direction`: A vector that denotes the center of the region that the joint is constrained to.
        - `Max Angle`: The number of degrees that the joint's orientation is allowed to deviate from `Direction`
 
## Requirements

This package is known to work for Unity Editor version 2021.3.0f1
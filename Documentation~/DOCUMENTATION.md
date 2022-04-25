# ahanlindev Inverse Kinematics Package

## Overview

This package offers modest inverse kinematics functionality based upon the Forward-and-Backward-Reaching Inverse Kinematics (FABRIK) algorithm.

## Package Contents
 - `IKChain` component class
    - Main source of functionality
    - A `GameObject` that contains this component will act as the starting joint of a kinematic chain
    - If end-effector and target Transforms are supplied, the chain will apply the FABRIK algorithm to attempt to move the end-effector to the target, affecting all intermediate positions in the heirarchy.
        - The end-effector must be a descendant of the `GameObject` that carries the `IKChain` component. 
 - `IKJoint` component class
    - Container for joint restraints
    - If any `GameObject`s that are between an `IKChain` and its end-effector carry an `IKJoint` component, whatever restraints are set in that joint are considered in the algorithm.  
 - IK Chain Generator window
    - Accessible via __Window/IK Chain Generator__ in the Unity editor
    - Allows generation or removal of `IKChain` and `IKJoint` objects between a given start and end `GameObject`
        - On generation, an `IKChain` component is added to the supplied root `GameObject`, and `IKJoint` components are added to the end effector and all of its ancestors until the supplied root.
        - On removal, the inverse occurs. The `IKChain` component is stripped from the root and `IKJoint` components are removed from both supplied `GameObject`s as well as all intermediate `GameObject`s
## Requirements

This package is known to work for Unity Editor version 2021.3.0f1
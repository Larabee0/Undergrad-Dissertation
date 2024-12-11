# Comp-302-2202796


## Methods/key words
- Triangular Simplification
- Retopology
- mesh re-coding

## Simplication methods
- vertex decimation
- mesh decimation
- iterative edge contraction
- Schroeder's Decimation
- edge collapses

## Detail Measurement Methods
- approximation error (Geometric distance using Eavg metric derived from Hausdorff distance)
- metrological deviation
- 
# R&D topic statement
The purpose here to take a high ploy generated convex mesh and to create a lower poly count,
but as geometrically "accurate" as possible to the original as quickly possible.

- What alogirthim is the fastest?
- What algoirthim produces the best detail with the least geometry?
- What alogirthim has the best balance of these two?

## Week 2 summary
### Need for work
- Not much/any in the field of apply simplication in real-time to a real-time generated mesh
- a lot of work is old (pre 2010)
- nothing focused on real-time terrain generation in games
### Refined research question
Out of a given set of triangular simplification algoirthims, which has the best balance of execution time to geometric accuracy for real-time terrain generation?

# Computing Artefact
The computing artefact using my comp305 project. This is a C# Vulkan ECS graphics engine which generates convex planetry terrain meshes based off a number of settings and a seed.
In this proposal I am adding to it the features needed to measure the Simplication algorthims I have selected.
For comp302's submission I am adding the calculation of Geometric Devation. All content written for comp302 will be inside the comp302 folder (Comp-302-2202796\SDL-Vulkan-CS\Comp302)

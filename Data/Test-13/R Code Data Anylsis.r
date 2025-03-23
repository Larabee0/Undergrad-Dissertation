# The computing arefact provides several CSV files that can be used for data analysis.
# Two csv files are intented to be compared together, one csv file contains the data
# for the terrain generation algorithm and the other for the quadric simplification
# algorithm All data is output twice, the difference is the order of the rows within
# the set, one is ordered by subDivisions then input reduction rate, the other by
# input reduction rate then by subDivisions
# Files that start with SUMMARY contain an the mean data for a whole planet.	
# Files that start with EXE contain only the execution time for
# the generation/simplification operation for a whole planet. These files have not
# been analysed as execution performance is not within the scope of this paper
#
# For this data anylsis the full data sets are used.
# These data sets record results for each tile mesh that composes a planet.
# The seed shows what planet any given tile belongs to.
# the Tile index identifies a tile within a planet.

### Independent variables ###
# Algorithm (0 or 1) identifies what algorithm was used to produce a simplified mesh.
# Algorithm 0: Terrain Generator 
# Algorithm 1: Quadric Simplification (QSlim)
# Src_SubDiv: The number of additional vertices added to each edge of each triangle
# of the original mesh. - How much geometry is in the mesh to start with.
# High values = more geometry

# Input_Reduction:
#   indicates the requested whole number percentage of original geometry to keep.
# 	95 > keep 95% of original geometry - reduce mesh by 5%
#	05 > keep 5% of original geometry - reduce mesh by 95%
# 	It is important to note this is a REQUESTED input.
# The actual number of vertices and triangles in the final mesh may not
# match these values because a vertex/triangle doesn't divide smaller than 1

### Dependant varaibles ###
# Vert_Count: Number of vertices in the simplified mesh
# Tri_Count: Number of triangles in the simplified mesh
# Vert_Reduction: % of vertices left compared to original mesh
# Tri_Reduction: % of triangles left compared to original mesh

## Elevation - defines how far in the local y axis of the mesh
## each vertex gets displaced to produce mountains/hills/flat ground ##
## Effectively these values indicate how much the mesh has been displaced,
## values towards 0 = low displacement ##
# Min_Elev: Lowest point of vertex displacement in the final mesh
# Max_Elev: Highest point of vertex displacement in the final mesh
# Mean_Elev: Aveage point of vertex displacement in the final mesh

## Shape quality ##
# Geometric devation from original mesh,
#if min, max and mean = 0 there is no difference between the start and end mesh

## Overall quality is measured by mean geometric devation (Mean_Dev) ##
# Min_Dev: Smallest geometric devation from the original mesh
# Max_Dev: Largest geometric devation from the original mesh
# Mean_Dev: Mean geometric devation from the original mesh


### Data source files ###
# SUB_Terrain_Generator.csv
#    Full Terrain generation quality set ordered by subDivisions
# SUB_Quadric_Simplification.csv
#    Full Quadric simplification quality set ordered by subDivisions
# These are compbined into one data set, combined_Set

library(readr)

SUB_Terrain_Generator <- read_csv(
"H:/GitFork/Comp-302-2202796/Data/Test-13/SUB_Terrain_Generator.csv",
    col_types = cols(Seed = col_integer(), 
        Tile_ID = col_integer(),
        Algorithm = col_factor(levels = c("0", "1")), Src_SubDiv = col_integer(), 
        Input_Reduction = col_integer(), 
        Vert_Count = col_integer(), Tri_Count = col_integer()))

SUB_Quadric_Simplification <- read_csv(
"H:/GitFork/Comp-302-2202796/Data/Test-13/SUB_Quadric_Simplification.csv",
    col_types = cols(Seed = col_integer(), 
        Tile_ID = col_integer(),
        Algorithm = col_factor(levels = c("0", "1")), Src_SubDiv = col_integer(), 
        Input_Reduction = col_integer(), 
        Vert_Count = col_integer(), Tri_Count = col_integer()))

combined_Set <- rbind(SUB_Terrain_Generator, SUB_Quadric_Simplification)

### Data anylsis ###
# We are looking at how The algorithm used, Src_SubDiv and Input_Reduction
# effect overall quality (Mean_Dev).
# Multiple Statistical tests where run to analyse the data set.
# First, a T-test comparing Algorithm to Mean_Dev on the whole data set is run to
# validate there is a difference between sets.
# Second, cohen's d is calculated for Algorithm and Mean_Dev across the whole data set
# to calculate the overall effect size.
# Thirdly a linear model regression test is run to see how the three factors
# Algorithm, Src_SubDiv and Input_Reduction effect Mean_Dev.
# Fourth, a pratt analysis of the model is run to calculate the importance of
# each regressor.
#
# Finally, a t test is conducted for each expirmental test run.
# The focus of this test is to see how Mean_Dev varies isolating to
# only the Algorithm.
# The Difference between the two data sets being compared here is the
# algorithm being used, Src_SubDiv & Input_Reduction are constant for each t.test.
# This is done to test if the Algorithm difference is universally better and
# to highlight anomalies in the data set.
# Cohen's D is also calculated for each test run.
#
# Additional notes.
# - The sink command is used to record the data anylsis to a text file.
# - Some t tests from each expirmental test section show NAN t values but still
#   state the alternative hypthsis is true.
#	This is incorrect. In such cases the alternative hypthsis is false,
#   there is not difference in Mean_Dev between the sets.
#   The mean in group 0 & mean in group 1 right below this statement
#   show that in all cases.

library(rstatix) 
library(relaimpo)
sink("Data_Analysis.txt")
print("Overall_T-Test")
statistic <- t.test(Mean_Dev~Algorithm, data = combined_Set, Paired=TRUE)
print(statistic)
print("Overall_Cohen's_D")
cohens_d <-combined_Set %>% cohens_d(Mean_Dev~Algorithm, paired=TRUE)
print(cohens_d)
print("")
print("")


print("Linear_Model")
lm_results <- lm(Mean_Dev~Algorithm+Src_SubDiv+Input_Reduction,data = combined_Set)
summary(lm_results)
print("Pratt_analysis_of_Linear_Model
-product_of_the_standardized_coefficient_and_the_correlation")
lm_pratt <- calc.relimp(lm_results,type=c("pratt"),rela = TRUE)
print(lm_pratt)

print("")
print("")
print("Per_expirment_T-tests")

tTestResults <- vector("list",190)
cohens_dResults <- vector("list",190)
counter <- 1
tTestResults <- list()
for(s in 1:10)
{
    for(i in 1:19){
		statistic <- t.test(
                            Mean_Dev~Algorithm,
                            data = combined_Set[
                                combined_Set$Src_SubDiv == subDivisions[s]
                                & combined_Set$Input_Reduction == inputReductions[i],
                            ], Paired=TRUE)
		
		cohens_d <-combined_Set[
                            combined_Set$Src_SubDiv == subDivisions[1]
                            & combined_Set$Input_Reduction == inputReductions[10],
                            ] %>% cohens_d(Mean_Dev~Algorithm, paired=TRUE)
		
		statistics[[counter]] <- statistic
		cohens_dResults[[counter]] <- cohens_d
		
		identifier <- toString(
                                c("Expirment-SubDivisions:_",
                                subDivisions[s],
                                "InputReductions:_",
                                inputReductions[i]))
        
		
		print(identifier)
		print("t-test")
		print(statistic)
		print("Cohen's_D")
		print(cohens_d)
		print("")
		print("")
		
        counter <- counter + 1
    }
}

sink(NULL)
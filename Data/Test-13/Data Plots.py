import pandas as pd
import seaborn as sns
import matplotlib.pyplot as plt

SUB_Terrain_Generator = pd.read_csv("H:/GitFork/Comp-302-2202796/Data/Test-13/SUB_Terrain_Generator.csv")
SUB_Quadric_Simplification = pd.read_csv("H:/GitFork/Comp-302-2202796/Data/Test-13/SUB_Quadric_Simplification.csv")

frames = [SUB_Terrain_Generator, SUB_Quadric_Simplification]
combined_Set = pd.concat(frames)
#print(combined_Set)
print(combined_Set.columns)

sns.set_theme(style="darkgrid")

# ok overall difference
#sns.barplot(data = combined_Set, x=' Algorithm', y =' Mean_Dev')

# really messy but mildly cool
# sns.lineplot(data = combined_Set, x=" Input_Reduction", y=" Mean_Dev", hue=" Src_SubDiv", style=" Algorithm")

#sns.relplot(data = combined_Set, kind="line",
#            x=" Input_Reduction", y=" Mean_Dev",col=" Algorithm",
#            size=" Src_SubDiv", style=" Algorithm",
#            hue=" Algorithm",
#            facet_kws=dict(sharex=False),)


## this is a good one
dataToPlot = combined_Set[combined_Set[" Src_SubDiv"].isin([5,10,15,20,25,30,35, 40, 45, 50])]
#dataToPlot = combined_Set[combined_Set[" Src_SubDiv"].isin([35, 40, 45, 50])]
dataToPlot = dataToPlot[dataToPlot[" Input_Reduction"].isin([5, 10, 15, 20, 25, 30, 35])]#, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95])]
print(dataToPlot)

#sns.catplot(data=dataToPlot, kind="bar", x=" Input_Reduction", y=" Tri_Reduction", hue=" Algorithm")

#sns.stripplot(
#    data=dataToPlot, x=" Input_Reduction", y=" Tri_Reduction", hue=" Algorithm",
#    jitter=False, s=20, marker="D", linewidth=1, alpha=.1,
#)
#sns.catplot(data=dataToPlot, kind="bar", x=" Input_Reduction", y=" Tri_Reduction", hue=" Algorithm", col=" Src_SubDiv", col_wrap=5)
#sns.catplot(data=dataToPlot, x=" Input_Reduction", y=" Tri_Reduction", hue=" Algorithm", col=" Src_SubDiv", aspect=.5)


grid = sns.FacetGrid(dataToPlot, col=" Src_SubDiv",hue=" Algorithm",
                     col_wrap=5, height=1.5,hue_order=[0, 1])
grid.map_dataframe(sns.lineplot, " Input_Reduction", " Mean_Dev")
#grid.map_dataframe(sns.lineplot, " Input_Reduction", " Mean_Dev")


#sns.displot(dataToPlot,x=" Input_Reduction", y = " Mean_Dev", hue=" Algorithm",kind="kde")

# horrifically slow and in appearence
#sns.pairplot(dataToPlot, hue=" Algorithm")

plt.show()


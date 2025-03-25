using VECS;
using Planets;
using System.Reflection;
using System;

namespace COMP302
{
    public class Program
    {
        private static ArtifactAuthoring artifactAuthoring;
        
        static int Main()
        {
            try
            {
                Assembly.Load("Planets");
                Application app = new();
                app.PreOnCreate += CreateArtifact;
                app.OnDestroy += DestroyArtifact;
                app.Run();
                app.PreOnCreate -= CreateArtifact;
                app.OnDestroy -= DestroyArtifact;
                app.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0},\n{1}", ex.Message, ex.StackTrace));
                return 1;
            }
            return 0;
        }

        static void CreateArtifact()
        {
            artifactAuthoring = new();
            Authoring.MainMenu();
        }

        static void DestroyArtifact()
        {
            ArtifactAuthoring.Destroy();
        }
    }
}

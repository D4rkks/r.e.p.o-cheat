using System;
using r.e.p.o_cheat;

class DisableMethods
{
    static void Main()
    {
        string className = "PlayerTumble";
        string[] methods = { "ImpactHurtSet", "ImpactHurtSetRPC", "Update", "TumbleSet", "Setup" };

        MonoPatcher.DisableMonoMethods(className, methods, out _);
        Console.WriteLine("Methods disabled.");
    }
}

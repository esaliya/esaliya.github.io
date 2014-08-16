using System;
using MPI;

namespace MPINETinMono
{
    class Program
    {
	    static void Main(string[] args)
	    {
			using (new MPI.Environment(ref args))
			{
			    Console.Write("Rank {0} of {1} running on {2}\n",
								Communicator.world.Rank,
								Communicator.world.Size,
								MPI.Environment.ProcessorName);
			}
        }
    }
}


##Running C# MPI.NET Applications with Mono and OpenMPI

I wrote an [earlier post](http://esaliya.blogspot.com/2013/04/running-mpinet-applications-with-mono.html) on the same subject, but just realized it's not detailed enough even for me to retry, hence the reason for this post.

I've tested this in [FutreGrid](https://portal.futuregrid.org/) with Infiniband to run our C# based [pairwise clustering program](https://github.com/DSC-SPIDAL/csharp/tree/master/SalsaTPL/Salsa.PairwiseClusteringTPL) on real data up to 32 nodes (I didn't find any restriction to go above this many nodes - it was just the maximum I could reserve at that time)

**What you'll need**
- [Mono 3.4.0](http://download.mono-project.com/sources/mono/mono-3.4.0.tar.bz2)
- [MPI.NET](http://www.osl.iu.edu/research/mpi.net) source code revision 338.

        svn co https://svn.osl.iu.edu/svn/mpi_net/trunk -r 338 mpi.net
    + Also, download the [Unsafe.pl.patch](https://github.com/esaliya/blogs/blob/master/resources/Unsafe.pl.patch), which was originally available [here](http://sources.gentoo.org/cgi-bin/viewvc.cgi/gentoo-x86/sys-cluster/mpi-dotnet/files/)

- [OpenMPI 1.4.3](http://www.open-mpi.org/software/ompi/v1.4/downloads/openmpi-1.4.3.tar.gz). Note this is a retired version of OpenMPI and we are using it only because that's the best that I could get MPI.NET to compile against. If in future MPI.NET team provides support for a newer version of OpenMPI, you may be able to use it as well.
- [Automake 1.9](http://ftp.gnu.org/gnu/automake/automake-1.9.tar.gz). Newer versions may work, but I encountered some errors in the past, which made me stick with version 1.9.


**How to install**
1. I suggest installing everything to a user directory, which will avoid you requiring super user privileges. Let's create a directory called `build_mono` inside home directory.

        mkdir ~/build_mono

    The following lines added to your ``~/.bashrc`` will help you follow the rest of the document.

        BUILD_MONO=~/build_mono
        PATH=$BUILD_MONO/bin:$PATH
        LD_LIBRARY_PATH=$BUILD_MONO/lib
        ac_cv_path_ILASM=$BUILD_MONO/bin/ilasm
        
        export BUILD_MONO PATH LD_LIBRARY_PATH ac_cv_path_ILASM

    Once these lines are added do,
    
        source ~/.bashrc

2. Build automake by first going to the directory that containst `automake-1.9.tar.gz` and doing,

        tar -xzf automake-1.9.tar.gz
        cd automake-1.9
        ./configure --prefix=$BUILD_MONO
        make
        make install

    You can verify the installation by typing `which automake`, which should point to `automake` inside `$BUILD_MONO/bin`

3. Build OpenMPI. Again, change directory to where you downloaded `openmpi-1.4.3.tar.gz` and do,

        tar -xzf openmpi-1.4.3.tar.gz
        cd openmpi-1.4.3
        ./configure --prefix=$BUILD_MONO
        make
        make install
    
    Optionally if Infiniband is available you can point to the `verbs.h` (usually this is in `/usr/include/infiniband/`) by specifying the folder `/usr` in the above `configure` command as,
    
        ./configure --prefix=$BUILD_MONO --with-openib=/usr
    
    If building OpenMPI is successfull, you'll see the following output for `mpirun --version` command,
    
        mpirun (Open MPI) 1.4.3
        
        Report bugs to http://www.open-mpi.org/community/help/
    
    Also, to make sure the Infiniband module is built correctly (if specified) you can do,
    
        ompi_info|grep openib
    
    which, should output the following.
    
        MCA btl: openib (MCA v2.0, API v2.0, Component v1.4.3)
    
4. Build Mono. Go to directory containing `mono-3.4.0.tar.bz2` and do,

        tar -xjf mono-3.4.0.tar.bz2
        cd mono-3.4.0
    
    Mono 3.4.0 release is missing a file, which you'll need to add by pasting the following content to a file called `./mcs/tools/xbuild/targets/Microsoft.Portable.Common.targets`
    
        <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
            <Import Project="..\Microsoft.Portable.Core.props" />
            <Import Project="..\Microsoft.Portable.Core.targets" />
        </Project>

    You can continue with the build by following,
    
        ./configure --prefix=$BUILD_MONO
        make
        make install
    
    There are several configuration parameters that you can play with and I suggest going through them either in `README.md` or in `./configure --help`. One parameter, in particular, that I'd like to test with is `--with-tls=pthread`

5. Build MPI.NET. If you were wonder why we had that `ac_cv_path_ILASM` variable in `~/.bashrc` then this is where it'll be used. MPI.NET by default tries to find the Intermediate Language Assembler (ILASM) at `/usr/bin/ilasm2`, which for 1. does not exist because we built Mono into `$BUILD_MONO` and not `/usr` 2. does not exist because newer versions of Mono calls this `ilasm` not `ilasm2`. Therefore, after digging through the `configure` file I found that we can specify the path to the ILASM by exporting the above environment variable.

    Alright, back to building MPI.NET. First copy the downloaded `Unsafe.pl.patch` to the subversion checkout of MPI.NET. Then change directory there and do,

        patch MPI/Unsafe.pl < Unsafe.pl.patch
    
    This will say some hunks failed to apply, but that should be fine. It only means that those are already fixed in the checkout. Once patching is completed continue with the following.
    
        ./autogen.sh
        ./configure --prefix=$BUILD_MONO
        make
        make install
    
    At this point you should be able to find `MPI.dll` and `MPI.dll.config` inside `MPI` directory, which you can use to bind against your C# MPI application.


**How to run**
- Here's a [sample MPI program](https://github.com/esaliya/blogs/blob/master/resources/Program.cs) written in C# using MPI.NET.
    
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
    
- There are two ways that you can compile this program.
    
    1.  Use Visual Studio referring to MPI.dll built on Windows
    2.  Use `mcs` from Linux referring to MPI.dll built on Linux

            mcs Program.cs -reference:$MPI.NET_DIR/tools/mpi_net/MPI/MPI.dll
    
        where `$MPI.NET_DIR` refers to the subversion checkout directory of MPI.NET

    Either way you should be able to get `Program.exe` in the end.

- Once you have the executable you can use `mono` with `mpirun` to run this in Linux. For example you can do the following within the directory of the executable,

        mpirun -np 4 mono ./Program.exe
    
    which will produce,
    
        Rank 0 of 4 running on i81
        Rank 2 of 4 running on i81
        Rank 1 of 4 running on i81
        Rank 3 of 4 running on i81

    where `i81` is one of the compute nodes in FutureGrid cluster.
    
    You may also use other advance options with mpirun to determine process mapping and binding. Note. the syntax for such controlling is different from latest versions of OpenMPI. Therefore, it's a good idea to look at different options from `mpirun --help`. For example you may be interested in specifying the following options,
    
        hostfile=<path-to-hostfile-listing-available-computing-nodes>
        ppn=<number-of-processes-per-node>
        cpp=<number-of-cpus-to-allocate-for-a-process>
        
        mpirun --display-map --mca btl ^tcp --hostfile $hostfile --bind-to-core --bysocket --npernode $ppn --cpus-per-proc $cpp -np $(($nodes*$ppn)) ...

    where, `--display-map` will print how processes are bind to processing units and `--mca btl ^tcp` forces to turn off `tcp`

That's all you'll need to run C# based MPI.NET applications in Linux with Mono and OpenMPI. Hope this helps!
        
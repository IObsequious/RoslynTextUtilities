using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace ConsoleTester.Scratch
{
    public class ScratchWorkspace : Workspace
    {
        /// <summary>Constructs a new workspace instance.</summary>
        /// <param name="host">The <see cref="T:Microsoft.CodeAnalysis.Host.HostServices" /> this workspace uses</param>
        /// <param name="workspaceKind">
        /// A string that can be used to identify the kind of workspace. Usually this matches the name
        /// of the class.
        /// </param>
        public ScratchWorkspace(HostServices host, string workspaceKind) : base(host, workspaceKind)
        {
        }
    }
}
